using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class LineDowntimeSearchViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;
    private readonly Dictionary<string, string> _statusMap = new();
    private readonly Dictionary<string, string> _categoryMap = new();
    private bool _dictLoaded;
    private readonly SemaphoreSlim _dictLock = new(1, 1);

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;
    [ObservableProperty] private DateTime occurStartDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime occurEndDate = DateTime.Today;
    [ObservableProperty] private StatusOption? selectedStatusOption;
    [ObservableProperty] private int pageIndex = 1;
    [ObservableProperty] private int pageSize = 10;

    public ObservableCollection<StatusOption> StatusOptions { get; } = new();
    public ObservableCollection<LineDowntimeCardItem> Records { get; } = new();

    public int RemainingItemsThreshold => HasMore ? 2 : -1;

    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand AddCommand { get; }
    public IAsyncRelayCommand<LineDowntimeCardItem?> ShowDetailCommand { get; }

    public LineDowntimeSearchViewModel(IWorkOrderApi api)
    {
        _api = api;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        AddCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync($"{nameof(Pages.LineDowntimeFormPage)}?mode=add"));
        ShowDetailCommand = new AsyncRelayCommand<LineDowntimeCardItem?>(ShowDetailAsync);
    }

    private async Task EnsureDictAsync()
    {
        if (_dictLoaded) return;

        await _dictLock.WaitAsync();
        try
        {
            if (_dictLoaded) return;

            StatusOptions.Clear();
            _statusMap.Clear();
            _categoryMap.Clear();
            StatusOptions.Add(new StatusOption { Text = "全部", Value = null });

            var resp = await _api.GetLineDowntimeDictAsync();
            var statusDict = resp.result?.FirstOrDefault(x => string.Equals(x.field, "recordStatus", StringComparison.OrdinalIgnoreCase))
                             ?? resp.result?.FirstOrDefault();
            var categoryDict = resp.result?.FirstOrDefault(x => string.Equals(x.field, "categoryName", StringComparison.OrdinalIgnoreCase));

            AddDictItems(statusDict?.dictItems, _statusMap, StatusOptions);
            AddDictItems(categoryDict?.dictItems, _categoryMap, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LineDowntime] Load dict error: {ex}");
            if (StatusOptions.Count == 0) StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
        }
        finally
        {
            SelectedStatusOption = StatusOptions.FirstOrDefault(x => x.Value == SelectedStatusOption?.Value)
                                   ?? StatusOptions.FirstOrDefault();
            _dictLoaded = true;
            _dictLock.Release();
        }
    }

    public async Task SearchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await EnsureDictAsync();
            PageIndex = 1;
            Records.Clear();
            var rows = await LoadPageAsync(PageIndex);
            foreach (var row in rows) Records.Add(row);
            HasMore = rows.Count >= PageSize;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || IsLoadingMore || !HasMore) return;
        try
        {
            IsLoadingMore = true;
            PageIndex++;
            var rows = await LoadPageAsync(PageIndex);
            foreach (var row in rows) Records.Add(row);
            HasMore = rows.Count >= PageSize;
        }
        finally { IsLoadingMore = false; }
    }



    private async Task ShowDetailAsync(LineDowntimeCardItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Source.id)) return;

        if (!item.IsClosed)
        {
            await Shell.Current.GoToAsync($"{nameof(Pages.LineDowntimeFormPage)}?mode=edit&id={Uri.EscapeDataString(item.Source.id!)}");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(Pages.LineDowntimeFormPage)}?mode=detail&id={Uri.EscapeDataString(item.Source.id!)}");
    }

    private async Task<List<LineDowntimeCardItem>> LoadPageAsync(int pageNo)
    {
        var start = OccurStartDate.Date;
        var end = OccurEndDate.Date.AddDays(1).AddTicks(-1);
        var page = await _api.PageLineDowntimeAsync(start, end, SelectedStatusOption?.Value, pageNo, PageSize, true);
        var records = page?.result?.records ?? new List<LineDowntimeRecord>();
        return records.Select(x => new LineDowntimeCardItem(x, MapStatus(x.recordStatus), MapCategory(x.categoryName))).ToList();
    }

    private static void AddDictItems(IEnumerable<DictItem>? items, Dictionary<string, string> map, ObservableCollection<StatusOption>? options)
    {
        var addedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items ?? Enumerable.Empty<DictItem>())
        {
            if (string.IsNullOrWhiteSpace(item.dictItemValue)) continue;
            if (!addedValues.Add(item.dictItemValue!)) continue;

            var text = item.dictItemName ?? item.dictItemValue!;
            map[item.dictItemValue!] = text;
            options?.Add(new StatusOption { Text = text, Value = item.dictItemValue });
        }
    }

    private string MapStatus(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : (_statusMap.TryGetValue(value!, out var name) ? name : value!);
    private string MapCategory(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : (_categoryMap.TryGetValue(value!, out var name) ? name : value!);

    partial void OnHasMoreChanged(bool value)
    {
        OnPropertyChanged(nameof(RemainingItemsThreshold));
    }
}

public sealed class LineDowntimeCardItem
{
    public LineDowntimeRecord Source { get; }
    public string StatusText { get; }
    public string StatusColor { get; }
    public string BorderColor { get; }
    public string CategoryText { get; }
    public bool IsClosed { get; }

    public LineDowntimeCardItem(LineDowntimeRecord source, string statusText, string? categoryText = null)
    {
        Source = source;
        StatusText = statusText;
        CategoryText = string.IsNullOrWhiteSpace(categoryText) ? (source.categoryName ?? "-") : categoryText!;
        IsClosed = statusText.Contains("复工") || statusText.Contains("完成") || string.Equals(source.recordStatus, "1", StringComparison.OrdinalIgnoreCase);
        StatusColor = IsClosed ? "#35C87A" : "#FF8A22";
        BorderColor = IsClosed ? "#4BD889" : "#FF7A1A";
    }
}
