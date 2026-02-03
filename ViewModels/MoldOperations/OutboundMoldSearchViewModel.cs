// ViewModels/OutboundMoldSearchViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutboundMoldSearchViewModel : ObservableObject
    {
        private readonly IMoldApi _api;
        private readonly IWorkOrderApi _workapi;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasMore = true;
        private readonly Dictionary<string, string> _auditMap = new();   // "1" -> "执行中"
        private readonly Dictionary<string, string> _urgentMap = new();  // "level2" -> "中"
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;
        private bool _dictsLoaded = false;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<WorkOrderDto> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        /// <summary>执行 OutboundMoldSearchViewModel 初始化逻辑。</summary>
        public OutboundMoldSearchViewModel(IMoldApi api, IWorkOrderApi workapi)
        {
            _api = api;
            _workapi = workapi;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
            _ = EnsureDictsLoadedAsync();   // fire-and-forget
           
        }
        /// <summary>执行 EnsureDictsLoadedAsync 逻辑。</summary>
        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                var bundle = await _workapi.GetWorkOrderDictsAsync();

                // 1) 填充状态下拉
                StatusOptions.Clear();
                StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
                foreach (var d in bundle.AuditStatus)
                {
                    if (string.IsNullOrWhiteSpace(d.dictItemValue)) continue;
                    StatusOptions.Add(new StatusOption
                    {
                        Text = d.dictItemName ?? d.dictItemValue!,
                        Value = d.dictItemValue
                    });
                }
                SelectedStatusOption ??= StatusOptions.FirstOrDefault();

                // 2) 建立两个码→名的映射表
                _auditMap.Clear();
                foreach (var d in bundle.AuditStatus)
                    if (!string.IsNullOrWhiteSpace(d.dictItemValue))
                        _auditMap[d.dictItemValue!] = d.dictItemName ?? d.dictItemValue!;

                _urgentMap.Clear();
                foreach (var d in bundle.Urgent)
                    if (!string.IsNullOrWhiteSpace(d.dictItemValue))
                        _urgentMap[d.dictItemValue!] = d.dictItemName ?? d.dictItemValue!;

                _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VM] Load dicts error: {ex}");
                // 兜底：至少保证一个“全部”
                if (StatusOptions.Count == 0)
                    StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
                SelectedStatusOption ??= StatusOptions.First();
                _dictsLoaded = true; // 防止重复打接口
            }
        }

        /// <summary>执行 SearchAsync 逻辑。</summary>
        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await EnsureDictsLoadedAsync();   // ★ 先确保字典到位
                PageIndex = 1;
                Orders.Clear();
                var records = await LoadPageAsync(PageIndex);
                if (records.Count == 0)
                    await Shell.Current.DisplayAlert("提示", "未查询到任何模具出库单", "确定");
                foreach (var r in records)
                    Orders.Add(r);
                HasMore = records.Count >= PageSize;
            }
            catch (Exception ex)
        {
                await Shell.Current.DisplayAlert("查询失败", ex.Message, "确定");
            }
            finally { IsBusy = false; }
        }

        /// <summary>执行 LoadMoreAsync 逻辑。</summary>
        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsBusy || IsLoadingMore || !HasMore) return;

            try
            {
                IsLoadingMore = true;
                PageIndex++;
                var records = await LoadPageAsync(PageIndex);
                foreach (var r in records)
                    Orders.Add(r);
                HasMore = records.Count >= PageSize;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>执行 LoadPageAsync 逻辑。</summary>
        private async Task<List<WorkOrderDto>> LoadPageAsync(int pageNo)
        {
            var byOrderNo = !string.IsNullOrWhiteSpace(Keyword);
            DateTime? start = byOrderNo ? null : StartDate.Date;
            DateTime? end = byOrderNo ? null : EndDate.Date.AddDays(1);

            var q = new WorkOrderQuery
            {
                PageNo = pageNo,
                PageSize = PageSize,
                AuditStatus = byOrderNo ? null : SelectedStatusOption?.Value,
                CreatedTimeStart = start,
                CreatedTimeEnd = end,
                WorkOrderNo = byOrderNo ? Keyword!.Trim() : null
            };

            var page = await _workapi.GetWorkOrdersAsync(q);
            var records = page?.result?.records
                       ?? page?.result?.list?.records
                       ?? new List<WorkOrderRecord>();

            var mapped = new List<WorkOrderDto>();
            foreach (var r in records)
            {
                var statusName = MapByDict(_auditMap, r.auditStatus);
                var urgentName = MapByDict(_urgentMap, r.urgent);
                var createdAt = TryParseDt(r.createdTime);

                mapped.Add(new WorkOrderDto
                {
                    Id = r.id ?? "",
                    OrderNo = r.workOrderNo ?? "-",
                    OrderName = r.workOrderName ?? "",
                    MaterialCode = r.materialCode ?? "",
                    MaterialName = r.materialName ?? "",
                    LineName = r.lineName ?? "",
                    Status = statusName,
                    Urgent = urgentName,
                    CurQty = (int?)r.curQty,
                    CreateDate = createdAt?.ToString("yyyy-MM-dd") ?? (r.createdTime ?? ""),
                    BomCode = r.bomCode,
                    RouteName = r.routeName,
                    WorkShopName = r.workShopName
                });
            }

            return mapped;
        }

        // 通用的码→名映射（字典里找不到就回退原码）
        /// <summary>执行 MapByDict 逻辑。</summary>
        private static string MapByDict(Dictionary<string, string> map, string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            return map.TryGetValue(code, out var name) ? name : code;
        }


        /// <summary>执行 ClearFilters 逻辑。</summary>
        private void ClearFilters()
        {
            Keyword = string.Empty;
            SelectedStatus = "全部";
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
            PageIndex = 1;
            HasMore = true;
            SelectedStatusOption = StatusOptions.FirstOrDefault();
            Orders.Clear();
        }



        /// <summary>执行 TryParseDt 逻辑。</summary>
        private static DateTime? TryParseDt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(s, out dt)) return dt;
            return null;
        }
        // 点击一条工单进入执行页
        /// <summary>执行 GoExecuteAsync 逻辑。</summary>
        [RelayCommand]
        private async Task GoExecuteAsync(WorkOrderDto? item)
        {
            if (item is null) return;

            // 跳到执行页（把 orderId/orderNo/baseInfo 都带上）
            await Shell.Current.GoToAsync(nameof(Pages.OutboundMoldPage), new Dictionary<string, object?>
            {
                ["workOrderNo"] = item.OrderNo,
                ["materialName"] = item.MaterialName
            });
        }
    }

}
