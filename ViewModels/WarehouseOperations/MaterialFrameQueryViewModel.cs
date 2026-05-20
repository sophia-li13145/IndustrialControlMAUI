using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class MaterialFrameQueryViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;
    private int _pageNo;
    private const int DefaultPageSize = 10;
    private bool _initialized;

    public ObservableCollection<MaterialFrameItemVm> Items { get; } = new();

    [ObservableProperty] private string? frameNo;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;

    public MaterialFrameQueryViewModel(IWorkOrderApi api) { _api = api; }

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
            var resp = await _api.PageMaterialFrameInfoAsync(nextPage, DefaultPageSize, string.IsNullOrWhiteSpace(FrameNo) ? null : FrameNo!.Trim());
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
}

public class MaterialFrameItemVm
{
    public MaterialFrameItemVm(MaterialFrameRecord r)
    {
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
