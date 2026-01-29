using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;


namespace IndustrialControlMAUI.ViewModels
{
    public partial class QualitySearchViewModel : ObservableObject
    {
        private readonly IQualityApi _qualityapi;
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
        [ObservableProperty] private List<DictItem> qualityTypesDict = new();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;

        private bool _dictsLoaded = false;

        public ObservableCollection<QualityOrderItem> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public QualitySearchViewModel(IQualityApi qualityapi)
        {
            _qualityapi = qualityapi;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
           
        }
        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                if (InspectStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _qualityapi.GetQualityDictsAsync();
                InspectStatusDict = dicts.InspectStatus;
                QualityTypesDict = dicts.QualityTypes;
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
            await EnsureDictsLoadedAsync();
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

        private async Task<List<QualityOrderItem>> LoadPageAsync(int pageNo)
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

            var qualityNo = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim();
            var createdTimeBegin = StartDate != default ? StartDate.ToString("yyyy-MM-dd 00:00:00") : null;
            var createdTimeEnd = EndDate != default ? EndDate.ToString("yyyy-MM-dd 23:59:59") : null;
            var inspectStatus = SelectedStatusOption?.Value;
            var searchCount = false;

            var resp = await _qualityapi.PageQueryAsync(
                pageNo: pageNo,
                pageSize: PageSize,
                qualityNo: qualityNo,
                createdTimeBegin: createdTimeBegin,
                createdTimeEnd: createdTimeEnd,
                inspectStatus: inspectStatus,
                qualityType: null,
                searchCount: searchCount);

            var records = resp?.result?.records ?? new List<QualityRecordDto>();
            var mapped = new List<QualityOrderItem>();
            foreach (var t in records)
            {
                t.inspectStatusName = statusMap.TryGetValue(t.inspectStatus ?? "", out var sName)
                    ? sName
                    : t.inspectStatus;
                t.qualityTypeText = QualityTypesDict.Where(x => x.dictItemValue == t.qualityType)?.FirstOrDefault()?.dictItemName;

                mapped.Add(new QualityOrderItem
                {
                    Id = t.id,
                    QualityNo = t.qualityNo,
                    InspectStatus = t.inspectStatus,
                    InspectStatusText = t.inspectStatusName,
                    InspectResult = t.inspectResult,
                    MaterialName = t.materialName,
                    QualityType = t.qualityType,
                    QualityTypeText = t.qualityTypeText,
                    OrderNumber = t.orderNumber,
                    ProcessName = t.processName,
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
        private async Task GoDetailAsync(QualityOrderItem? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(QualityDetailPage) + $"?id={Uri.EscapeDataString(item.Id)}");
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
