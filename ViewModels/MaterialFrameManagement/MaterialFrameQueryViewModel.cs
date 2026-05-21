using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class MaterialFrameQueryViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private int _pageNo;
    private const int DefaultPageSize = 10;
    private bool _initialized;

    public ObservableCollection<MaterialFrameItemVm> Items { get; } = new();

    [ObservableProperty] private string? frameNo;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;
    [ObservableProperty] private string pageTitle = "料框查询";
    [ObservableProperty] private bool showBottomActionButton;
    [ObservableProperty] private string actionButtonText = "新增记录";
    [ObservableProperty] private string? operationType;

    public MaterialFrameQueryViewModel(IMaterialFrameApi api) { _api = api; }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await LoadPageAsync(reset: true);
    }

    [RelayCommand]
    public async Task SearchAsync() => await LoadPageAsync(reset: true);

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsBusy || !HasMore) return;
        await LoadPageAsync(reset: false);
    }

    private async Task LoadPageAsync(bool reset)
    {
        if (reset) IsBusy = true; else IsLoadingMore = true;
        try
        {
            var nextPage = reset ? 1 : _pageNo + 1;
            var resp = await _api.PageMaterialFrameInfoAsync(nextPage, DefaultPageSize, OperationType, string.IsNullOrWhiteSpace(FrameNo) ? null : FrameNo!.Trim());
            var records = resp?.result?.records ?? new List<MaterialFrameRecord>();

            if (reset) Items.Clear();

            foreach (var r in records)
                Items.Add(new MaterialFrameItemVm(r));

            _pageNo = nextPage;
            HasMore = records.Count >= DefaultPageSize;
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
        }
    }

    public void ApplyOperation(string displayName, string operationTypeValue)
    {
        PageTitle = $"{displayName}操作";
        ActionButtonText = "新增记录";
        OperationType = operationTypeValue;
        ShowBottomActionButton = true;
    }
}

public class MaterialFrameItemVm
{
    public MaterialFrameRecord Source { get; }

    public MaterialFrameItemVm(MaterialFrameRecord r)
    {
        Source = r;
        FrameNoDisplay = string.IsNullOrWhiteSpace(r.frameNo) ? "-" : r.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(r.currentLocation) ? "未分配位置" : r.currentLocation!;
        var use = r.frameInfo?.useStatus ?? 0;
        UseStatusText = use == 1 ? "占用" : "空闲";
        UseStatusColor = use == 1 ? "#EF4444" : "#22C55E";
        var full = r.fullLoadStatus == true;
        FullLoadStatusText = full ? "已满载" : "未满载";
        FullLoadStatusColor = full ? "#F97316" : "#9CA3AF";
    }

    public string FrameNoDisplay { get; }
    public string CurrentLocationDisplay { get; }
    public string UseStatusText { get; }
    public string UseStatusColor { get; }
    public string FullLoadStatusText { get; }
    public string FullLoadStatusColor { get; }
}

public class MaterialFrameDetailLoadItemVm
{
    public MaterialFrameDetailLoadItemVm(MaterialFrameLoadDetail d)
    {
        MaterialName = FirstNotEmpty(d.materialName, d.productName, d.itemName, "-");
        BatchNo = FirstNotEmpty(d.batchNo, d.lotNo, "-");
        QtyDisplay = (d.currentQty ?? d.currentQuantity ?? d.quantity ?? 0m).ToString("0.##");
    }

    public string MaterialName { get; }
    public string BatchNo { get; }
    public string QtyDisplay { get; }

    private static string FirstNotEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v)) return v!;
        }

        return "-";
    }
}

public partial class MaterialFrameDetailViewModel : ObservableObject
{
    public ObservableCollection<MaterialFrameDetailLoadItemVm> LoadDetails { get; } = new();

    [ObservableProperty] private string frameNoDisplay = "-";
    [ObservableProperty] private string currentLocationDisplay = "未分配位置";
    [ObservableProperty] private string useStatusText = "空闲";
    [ObservableProperty] private string useStatusColor = "#22C55E";

    public int DetailCount => LoadDetails.Count;

    public void Apply(MaterialFrameRecord? record)
    {
        LoadDetails.Clear();
        if (record == null)
        {
            OnPropertyChanged(nameof(DetailCount));
            return;
        }

        FrameNoDisplay = string.IsNullOrWhiteSpace(record.frameNo) ? "-" : record.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(record.currentLocation) ? "未分配位置" : record.currentLocation!;
        var use = record.frameInfo?.useStatus ?? 0;
        UseStatusText = use == 1 ? "占用" : "空闲";
        UseStatusColor = use == 1 ? "#EF4444" : "#22C55E";

        foreach (var detail in record.loadDetailList ?? new List<MaterialFrameLoadDetail>())
            LoadDetails.Add(new MaterialFrameDetailLoadItemVm(detail));

        OnPropertyChanged(nameof(DetailCount));
    }
}
