using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using static Android.App.DownloadManager;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>图1：仪表选择弹窗 VM</summary>
    public partial class MeterSelectViewModel : ObservableObject
    {
        private readonly IEnergyApi _api;
        private readonly CancellationTokenSource _cts = new();
        private bool _inited;
        private EnergyMeterUiRow? _lastSelected;

        /// <summary>执行 MeterSelectViewModel 初始化逻辑。</summary>
        public MeterSelectViewModel(IEnergyApi api)
        {
            _api = api;
        }

        // ===== 顶部筛选（已按要求去掉“部门/设备”） =====
        [ObservableProperty] private string? filterCode;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OptionItem> EnergyTypeOptions { get; } = new();
        [ObservableProperty] private OptionItem? selectedEnergyType;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<IdNameOption> WorkshopOptions { get; } = new();
        [ObservableProperty] private IdNameOption? selectedWorkshop;

        // 如果暂时不做产线，以下两行可以删除
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<IdNameOption> LineOptions { get; } = new();
        [ObservableProperty] private IdNameOption? selectedLine;

        // ===== 列表数据 =====
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<EnergyMeterUiRow> Rows { get; } = new();
        [ObservableProperty] private EnergyMeterUiRow? selectedRow;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasMore = true;
        private int _pageNo = 1;
        private const int PageSize = 10;

        /// <summary>弹窗加载时调用一次</summary>
        public async Task EnsureInitAsync()
        {
            if (_inited) return;

            // 能源类型
            var types = await _api.GetEnergyTypeDictAsync(_cts.Token);
            EnergyTypeOptions.Clear();
            EnergyTypeOptions.Add(new OptionItem { Value = null, Text = "全部" });
            foreach (var t in types)
                EnergyTypeOptions.Add(new OptionItem { Value = t.dictItemValue, Text = t.dictItemName ?? t.dictItemValue ?? "" });
            SelectedEnergyType = EnergyTypeOptions.FirstOrDefault();

            // 车间
            var shops = await _api.GetWorkshopsAsync("workshop", _cts.Token);
            WorkshopOptions.Clear();
            foreach (var s in shops) WorkshopOptions.Add(s);
            SelectedWorkshop = WorkshopOptions.FirstOrDefault();

            // —— 产线（当前接口不带车间过滤，后续如有 parent 参数再联动）
            var lines = await _api.GetProductLinesAsync("production_line", _cts.Token);
            LineOptions.Clear();
            foreach (var l in lines) LineOptions.Add(l);
            SelectedLine = LineOptions.FirstOrDefault();
            _ = Query();
            _inited = true;
        }

        /// <summary>执行 Query 逻辑。</summary>
        [RelayCommand]
        private async Task Query()
        {
            await EnsureInitAsync();
            _pageNo = 1;
            Rows.Clear();
            var records = await LoadPageAsync(_pageNo);
            foreach (var r in records)
                Rows.Add(r);
            HasMore = records.Count >= PageSize;
        }

        /// <summary>执行 LoadMoreAsync 逻辑。</summary>
        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsLoadingMore || !HasMore) return;

            try
            {
                IsLoadingMore = true;
                _pageNo++;
                var records = await LoadPageAsync(_pageNo);
                foreach (var r in records)
                    Rows.Add(r);
                HasMore = records.Count >= PageSize;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>执行 LoadPageAsync 逻辑。</summary>
        private async Task<List<EnergyMeterUiRow>> LoadPageAsync(int pageNo)
        {
            var resp = await _api.MeterPageQueryAsync(
                pageNo: pageNo,
                pageSize: PageSize,
                meterCode: FilterCode,
                energyType: SelectedEnergyType?.Value,
                workshopId: SelectedWorkshop?.Id,
                lineId: SelectedLine?.Id,
                searchCount: true,
                ct: _cts.Token);

            var recs = resp?.result?.records ?? new();
            var mapped = new List<EnergyMeterUiRow>();
            foreach (var r in recs)
            {
                mapped.Add(new EnergyMeterUiRow
                {
                    MeterCode = r.meterCode ?? "",
                    EnergyType = r.energyType ?? "",
                    WorkshopName = r.workshopName ?? "",
                    WorkshopId = r.workshopId ?? "",
                    LineName = r.lineName ?? "",
                    LineId = r.lineId ?? ""
                });
            }

            return mapped;
        }

        /// <summary>执行 SelectRow 逻辑。</summary>
        [RelayCommand]
        private void SelectRow(EnergyMeterUiRow row)
        {
            if (_lastSelected != null && _lastSelected != row)
                _lastSelected.IsSelected = false;

            row.IsSelected = true;
            _lastSelected = row;

            SelectedRow = row;   // 你原有的 SelectedRow 继续保留给“确认”用
        }
    }
}
