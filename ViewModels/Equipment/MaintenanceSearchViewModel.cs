using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class MaintenanceSearchViewModel : ObservableObject
    {
        private readonly IEquipmentApi _equipmentapi;
        /// <summary>执行 new 逻辑。</summary>
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasMore = true;
        [ObservableProperty] private List<DictItem> maintenanceStatusDict = new();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;

        private bool _dictsLoaded = false;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<MaintenanceOrderItem> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        /// <summary>执行 MaintenanceSearchViewModel 初始化逻辑。</summary>
        public MaintenanceSearchViewModel(IEquipmentApi equipmentapi)
        {
            _equipmentapi = equipmentapi;
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
                if (MaintenanceStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _equipmentapi.GetMainDictsAsync();
                MaintenanceStatusDict = dicts.MaintenanceStatus;

                // 如果你需要将字典转为下拉选项绑定到 Picker：
                StatusOptions.Clear();
                foreach (var d in MaintenanceStatusDict)
                    StatusOptions.Add(new StatusOption { Text = d.dictItemName ?? "", Value = d.dictItemValue });
                _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                _dictsLoaded = true;
            }
        }




        /// <summary>执行 SearchAsync 逻辑。</summary>
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
                foreach (var t in records)
                    Orders.Add(t);
                HasMore = records.Count >= PageSize;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>执行 LoadPageAsync 逻辑。</summary>
        private async Task<List<MaintenanceOrderItem>> LoadPageAsync(int pageNo)
        {
            var statusMap = MaintenanceStatusDict?
            .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
            .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(
            k => k.dictItemValue!,
            v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
            StringComparer.OrdinalIgnoreCase
        ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var upkeepNo = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
            var planUpkeepTimeBegin = StartDate != default ? StartDate.ToString("yyyy-MM-dd 00:00:00") : null;
            var planUpkeepTimeEnd = EndDate != default ? EndDate.ToString("yyyy-MM-dd 23:59:59") : null;
            var upkeepStatus = SelectedStatusOption?.Value;
            var searchCount = false;

            var resp = await _equipmentapi.MainPageQueryAsync(
                pageNo: pageNo,
                pageSize: PageSize,
                upkeepNo: upkeepNo,
                planUpkeepTimeBegin: planUpkeepTimeBegin,
                planUpkeepTimeEnd: planUpkeepTimeEnd,
                upkeepStatus: upkeepStatus,
                searchCount: searchCount);

            var records = resp?.result?.records ?? new List<MaintenanceRecordDto>();
            var mapped = new List<MaintenanceOrderItem>();
            foreach (var t in records)
            {
                t.upkeepStatusText = statusMap.TryGetValue(t.upkeepStatus ?? "", out var sName)
                    ? sName
                    : t.upkeepStatus;

                mapped.Add(new MaintenanceOrderItem
                {
                    Id = t.id,
                    UpkeepNo = t.upkeepNo,
                    UpkeepStatus = t.upkeepStatus,
                    UpkeepStatusText = t.upkeepStatusText,
                    DevName = t.devName,
                    PlanUpkeepTime = ParseDate(t.planUpkeepTime),
                    UpkeepTime = ParseDate(t.upkeepTime),
                    PlanName = t.planName,
                    DevCode = t.devCode,
                    CreatedTime = ParseDate(t.createdTime)
                });
            }

            return mapped;
        }


        /// <summary>执行 ShowTip 逻辑。</summary>
        private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

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

        // 点击一条工单进入执行页
        /// <summary>执行 GoDetailAsync 逻辑。</summary>
        [RelayCommand]
        private async Task GoDetailAsync(MaintenanceOrderItem? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(MaintenanceDetailPage) + $"?id={Uri.EscapeDataString(item.Id)}");
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
