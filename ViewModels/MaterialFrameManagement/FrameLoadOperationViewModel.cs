using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameLoadOperationViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private const int DefaultPageSize = 10;
    private int _pageNo;
    private bool _initialized;

    public ObservableCollection<FrameUseRecordOperation> Items { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;
    [ObservableProperty] private string pageTitle = "装框操作";

    public FrameLoadOperationViewModel(IMaterialFrameApi api) => _api = api;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await LoadPageAsync(true);
    }

    [RelayCommand]
    public async Task SearchAsync() => await LoadPageAsync(true);

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsBusy || !HasMore) return;
        await LoadPageAsync(false);
    }

    private async Task LoadPageAsync(bool reset)
    {
        if (reset) IsBusy = true; else IsLoadingMore = true;
        try
        {
            var nextPage = reset ? 1 : _pageNo + 1;
            var resp = await _api.PageFrameUseRecordPageAsync(nextPage, DefaultPageSize, "framing", null, null);
            var records = resp?.result?.records ?? new List<FrameUseRecordOperation>();
            if (reset) Items.Clear();
            foreach (var r in records) Items.Add(r);
            _pageNo = nextPage;
            HasMore = records.Count >= DefaultPageSize;
        }
        finally { IsBusy = false; IsLoadingMore = false; }
    }
}
