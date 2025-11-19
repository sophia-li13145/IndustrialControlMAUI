using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class MaintenanceRunSearchViewModel : ObservableObject
    {
        private readonly IEquipmentApi _equipmentapi;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 50;
        [ObservableProperty] private List<DictItem> maintenanceStatusDict = new();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;

        private bool _dictsLoaded = false;

        public ObservableCollection<MaintenanceOrderItem> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public MaintenanceRunSearchViewModel(IEquipmentApi equipmentapi)
        {
            _equipmentapi = equipmentapi;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
            _ = EnsureDictsLoadedAsync();   // fire-and-forget
           
        }
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




        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Orders.Clear();
                var statusMap = MaintenanceStatusDict?
                .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
                .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(
                k => k.dictItemValue!,
                v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
                StringComparer.OrdinalIgnoreCase
            ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var pageNo = PageIndex;
                var pageSize = PageSize;
                var upkeepNo = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
                var planUpkeepTimeBegin = StartDate != default ? StartDate.ToString("yyyy-MM-dd 00:00:00") : null;
                var planUpkeepTimeEnd = EndDate != default ? EndDate.ToString("yyyy-MM-dd 23:59:59") : null;
                var upkeepStatus = SelectedStatusOption?.Value;   // “1”“2”“3”
                var searchCount = false;                           // 是否统计总记录

                // 调用 API
                var resp = await _equipmentapi.MainPageQueryAsync(
                    pageNo: pageNo,
                    pageSize: pageSize,
                    upkeepNo: upkeepNo,
                    planUpkeepTimeBegin: planUpkeepTimeBegin,
                    planUpkeepTimeEnd: planUpkeepTimeEnd,
                    upkeepStatus: upkeepStatus,
                    searchCount: searchCount);

                var records = resp?.result?.records;
                if (records is null || records.Count == 0)
                {
                    await ShowTip("未查询到数据");
                    return;
                }

                foreach (var t in records)
                {
                    t.upkeepStatusText = statusMap.TryGetValue(t.upkeepStatus ?? "", out var sName)
                        ? sName
                        : t.upkeepStatus;

                    Orders.Add(new MaintenanceOrderItem
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


        private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

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

        // 点击一条工单进入执行页
        [RelayCommand]
        private async Task GoDetailAsync(MaintenanceOrderItem? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(MaintenanceRunDetailPage) + $"?id={Uri.EscapeDataString(item.Id)}");
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
