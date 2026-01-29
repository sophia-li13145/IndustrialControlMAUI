using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class ExceptionSubmissionSearchViewModel : ObservableObject
    {
        private readonly IEquipmentApi _equipmentapi;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasMore = true;
        [ObservableProperty] private List<DictItem> exceptionStatusDict = new();
        [ObservableProperty] private List<DictItem> urgentDict = new();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;

        private bool _dictsLoaded = false;

        public ObservableCollection<MaintenanceReportDto> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public ExceptionSubmissionSearchViewModel(IEquipmentApi equipmentapi)
        {
            _equipmentapi = equipmentapi;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
            _ = EnsureDictsLoadedAsync();   // fire-and-forget
           
        }

        // ======= 新增：报修命令 =======
        [RelayCommand]
        private async Task RepairAsync(MaintenanceReportDto? item)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.id))
                return;

            // 二次确认
            var ok = await Shell.Current.DisplayAlert(
                "确认报修",
                "确定要对该异常提报执行报修操作吗？",
                "确定",
                "取消");
            if (!ok) return;

            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // 调用报修接口：只需要 id
                var resp = await _equipmentapi.SubmitExceptAsync(item.id);

                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("报修成功。");
                    // ⭐ 本地更新这一行的状态，并触发 UI 刷新
                    var idx = Orders.IndexOf(item);
                    if (idx >= 0)
                    {
                        // 直接改原对象
                        item.auditStatus = "1";          // 报修状态码，按你后端实际调整
                        item.auditStatusText = "已报修"; // 或者用字典里对应的中文

                        // 关键：替换这一行，强制 CollectionView 重绘
                        Orders[idx] = item;
                    }

                }
                else
                {
                    await ShowTip($"报修失败：{resp?.message ?? "接口返回失败"}");
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"报修异常：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                if (ExceptionStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _equipmentapi.GetExceptDictsAsync();
                ExceptionStatusDict = dicts.AuditStatus;
                UrgentDict = dicts.Urgent;

                // 如果你需要将字典转为下拉选项绑定到 Picker：
                StatusOptions.Clear();
                foreach (var d in ExceptionStatusDict)
                    StatusOptions.Add(new StatusOption { Text = d.dictItemName ?? "", Value = d.dictItemValue });
                _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                _dictsLoaded = true;
            }
        }




        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                PageIndex = 1;
                Orders.Clear();
                var records = await LoadPageAsync(PageIndex);
                if (records.Count == 0)
                {
                    await ShowTip("未查询到数据");
                    return;
                }
                foreach (var t in records)
                    Orders.Add(t);
                HasMore = records.Count >= PageSize;
            }
            catch (Exception ex)
            {
                await ShowTip($"查询异常：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsBusy || IsLoadingMore || !HasMore) return;

            try
            {
                IsLoadingMore = true;
                PageIndex++;
                var records = await LoadPageAsync(PageIndex);
                foreach (var t in records)
                    Orders.Add(t);
                HasMore = records.Count >= PageSize;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private async Task<List<MaintenanceReportDto>> LoadPageAsync(int pageNo)
        {
            var statusMap = ExceptionStatusDict?
            .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
            .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(
            k => k.dictItemValue!,
            v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
            StringComparer.OrdinalIgnoreCase
        ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var urgentMap = UrgentDict?
           .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
           .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
           .Select(g => g.First())
           .ToDictionary(
           k => k.dictItemValue!,
           v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
           StringComparer.OrdinalIgnoreCase
       ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var maintainNo = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
            var createdTimeBegin = StartDate != default ? StartDate.ToString("yyyy-MM-dd 00:00:00") : null;
            var createdTimeEnd = EndDate != default ? EndDate.ToString("yyyy-MM-dd 23:59:59") : null;
            var auditStatus = SelectedStatusOption?.Value;
            var searchCount = false;

            var resp = await _equipmentapi.ESPageQueryAsync(
                pageNo: pageNo,
                pageSize: PageSize,
                maintainNo: maintainNo,
                createdTimeBegin: createdTimeBegin,
                createdTimeEnd: createdTimeEnd,
                auditStatus: auditStatus,
                searchCount: searchCount);

            var records = resp?.result?.records ?? new List<MaintenanceReportDto>();
            var mapped = new List<MaintenanceReportDto>();
            foreach (var t in records)
            {
                t.auditStatusText = statusMap.TryGetValue(t.auditStatus ?? "", out var sName)
                    ? sName
                    : t.auditStatus;
                t.urgentText = urgentMap.TryGetValue(t.urgent ?? "", out var nName)
                   ? nName
                   : t.urgent;

                mapped.Add(new MaintenanceReportDto
                {
                    id = t.id,
                    maintainNo = t.maintainNo,
                    auditStatus = t.auditStatus,
                    auditStatusText = t.auditStatusText,
                    devName = t.devName,
                    devCode = t.devCode,
                    createdTime = t.createdTime,
                    urgent = t.urgent,
                    urgentText = t.urgentText
                });
            }

            return mapped;
        }


        private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

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

        // 点击一条工单进入执行页
        [RelayCommand]
        private async Task GoDetailAsync(MaintenanceReportDto? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(ExceptionSubmissionPage) + $"?id={Uri.EscapeDataString(item.id)}");
        }
        //进入编辑页面
        [RelayCommand]
        private async Task GoEditAsync(MaintenanceReportDto? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(EditExceptionSubmissionPage) + $"?id={Uri.EscapeDataString(item.id)}");
        }

        // 新建异常：不带 id 跳到编辑页 => 进入新增模式
        [RelayCommand]
        private async Task GoCreateAsync()
        {
            await Shell.Current.GoToAsync(nameof(EditExceptionSubmissionPage));
        }

        /// <summary>
        /// 安全解析日期字符串（空或格式不对返回 null）
        /// </summary>
        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParse(s, out var d))
                return d;

            return null;
        }
    }

}
