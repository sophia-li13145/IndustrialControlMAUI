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

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 50;
        private readonly Dictionary<string, string> _auditMap = new();   // "1" -> "执行中"
        private readonly Dictionary<string, string> _urgentMap = new();  // "level2" -> "中"
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;
        private bool _dictsLoaded = false;

        public ObservableCollection<MoldDto> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public OutboundMoldSearchViewModel(IMoldApi api)
        {
            _api = api;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
            _ = EnsureDictsLoadedAsync();   // fire-and-forget
        }
        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                var bundle = await _api.GetMoldDictsAsync();

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

        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await EnsureDictsLoadedAsync();   // ★ 先确保字典到位

                var byOrderNo = !string.IsNullOrWhiteSpace(Keyword);
                DateTime? start = byOrderNo ? null : StartDate.Date;
                DateTime? end = byOrderNo ? null : EndDate.Date.AddDays(1);

                var q = new MoldQuery
                {
                    PageNo = PageIndex,
                    PageSize = PageSize,
                    AuditStatus = byOrderNo ? null : SelectedStatusOption?.Value, // ★ 直接传字典值（"0"/"1"/...）
                    CreatedTimeStart = start,
                    CreatedTimeEnd = end,
                    MoldNo = byOrderNo ? Keyword!.Trim() : null
                };

                var page = await _api.GetMoldsAsync(q);
                var records = page?.result?.records
                           ?? page?.result?.list?.records
                           ?? new List<WorkOrderRecord>();

                Orders.Clear();
                foreach (var r in records)
                {
                    // r.auditStatus 是 "0"/"1"/...，r.urgent 是 "level1/2/3"
                    var statusName = MapByDict(_auditMap, r.auditStatus);
                    var urgentName = MapByDict(_urgentMap, r.urgent);
                    var createdAt = TryParseDt(r.createdTime);

                    Orders.Add(new MoldDto
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
                        BomCode = r.bomCode,                 // e.g. "BOM00000006"
                        RouteName = r.routeName,             // e.g. "午餐肉罐头测试工序调整"
                        WorkShopName = r.workShopName        // e.g. "制造二组"
                    });
                }
            }
            finally { IsBusy = false; }
        }

        // 通用的码→名映射（字典里找不到就回退原码）
        private static string MapByDict(Dictionary<string, string> map, string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            return map.TryGetValue(code, out var name) ? name : code;
        }


        private void ClearFilters()
        {
            Keyword = string.Empty;
            SelectedStatus = "全部";
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
            PageIndex = 1;
            SelectedStatusOption = StatusOptions.FirstOrDefault();
            Orders.Clear();
        }



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
        [RelayCommand]
        private async Task GoExecuteAsync(MoldDto? item)
        {
            if (item is null) return;

            // 基础信息列表（显示在执行页“工单基础信息表格”）
            var baseInfos = new List<BaseInfoItem>
    {
        new() { Key = "工单编号",   Value = item.OrderNo },
        new() { Key = "状态",       Value = item.Status },
        new() { Key = "优先级",     Value = item.Urgent },
        new() { Key = "生产数量",   Value = item.CurQty?.ToString() ?? "" },
        new() { Key = "创建日期",   Value = item.CreateDate },
        new() { Key = "物料编码",   Value = item.MaterialCode },
        new() { Key = "物料名称",   Value = item.MaterialName },
        new() { Key = "产线",       Value = item.LineName },
        new() { Key = "BOM编号",    Value = item.BomCode ?? "" },
        new() { Key = "工艺路线",   Value = item.RouteName ?? "" },
        new() { Key = "车间",       Value = item.WorkShopName ?? "" },
    };

            var baseInfoJson = JsonSerializer.Serialize(baseInfos);

            // 跳到执行页（把 orderId/orderNo/baseInfo 都带上）
            await Shell.Current.GoToAsync(nameof(Pages.MoldOutboundExecutePage), new Dictionary<string, object?>
            {
                ["orderNo"] = item.OrderNo,
                ["orderId"] = item.Id,
                ["baseInfo"] = baseInfoJson
            });
        }
    }

}
