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
            var frameNo = string.IsNullOrWhiteSpace(FrameNo) ? null : FrameNo!.Trim();
            var resp = string.IsNullOrWhiteSpace(OperationType)
                ? await _api.PageMaterialFrameInfoAsync(nextPage, DefaultPageSize, operationType: null, frameNo: frameNo)
                : await _api.PageMaterialFrameOperationAsync(nextPage, DefaultPageSize, OperationType!, frameNo);
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
