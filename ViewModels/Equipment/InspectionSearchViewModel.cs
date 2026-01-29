using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class InspectionSearchViewModel : ObservableObject
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
        [ObservableProperty] private List<DictItem> inspectStatusDict = new();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;

        private bool _dictsLoaded = false;

        public ObservableCollection<InspectionOrderItem> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public InspectionSearchViewModel(IEquipmentApi equipmentapi)
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
                if (InspectStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _equipmentapi.GetInspectionDictsAsync();
                InspectStatusDict = dicts.InspectStatus;

                // 如果你需要将字典转为下拉选项绑定到 Picker：
                StatusOptions.Clear();
                foreach (var d in InspectStatusDict)
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

        private async Task<List<InspectionOrderItem>> LoadPageAsync(int pageNo)
        {
            var statusMap = InspectStatusDict?
            .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
            .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(
            k => k.dictItemValue!,
            v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
            StringComparer.OrdinalIgnoreCase
        ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var inspectNo = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
            var createdTimeBegin = StartDate != default ? StartDate.ToString("yyyy-MM-dd 00:00:00") : null;
            var createdTimeEnd = EndDate != default ? EndDate.ToString("yyyy-MM-dd 23:59:59") : null;
            var inspectStatus = SelectedStatusOption?.Value;
            var searchCount = false;

            var resp = await _equipmentapi.PageQueryAsync(
                pageNo: pageNo,
                pageSize: PageSize,
                inspectNo: inspectNo,
                createdTimeBegin: createdTimeBegin,
                createdTimeEnd: createdTimeEnd,
                inspectStatus: inspectStatus,
                searchCount: searchCount);

            var records = resp?.result?.records ?? new List<InspectionRecordDto>();
            var mapped = new List<InspectionOrderItem>();
            foreach (var t in records)
            {
                t.inspectStatusText = statusMap.TryGetValue(t.inspectStatus ?? "", out var sName)
                    ? sName
                    : t.inspectStatus;

                mapped.Add(new InspectionOrderItem
                {
                    Id = t.id,
                    InspectNo = t.inspectNo,
                    InspectStatus = t.inspectStatus,
                    InspectStatusText = t.inspectStatusText,
                    InspectResult = t.inspectResult,
                    DevName = t.devName,
                    InspectTime = ParseDate(t.inspectTime),
                    PlanName = t.planName,
                    DevCode = t.devCode,
                    CreatedTime = ParseDate(t.createdTime)
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
        private async Task GoDetailAsync(InspectionOrderItem? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(InspectionDetailPage) + $"?id={Uri.EscapeDataString(item.Id)}");
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
