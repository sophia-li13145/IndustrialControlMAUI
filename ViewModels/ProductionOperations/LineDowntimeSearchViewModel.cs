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
    private bool _dictLoaded;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;
    [ObservableProperty] private DateTime occurStartDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private TimeSpan occurStartTime = TimeSpan.Zero;
    [ObservableProperty] private DateTime occurEndDate = DateTime.Today;
    [ObservableProperty] private TimeSpan occurEndTime = new(23, 59, 59);
    [ObservableProperty] private StatusOption? selectedStatusOption;
    [ObservableProperty] private int pageIndex = 1;
    [ObservableProperty] private int pageSize = 10;

    public ObservableCollection<StatusOption> StatusOptions { get; } = new();
    public ObservableCollection<LineDowntimeCardItem> Records { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand AddCommand { get; }
    public IAsyncRelayCommand<LineDowntimeCardItem?> ShowDetailCommand { get; }

    public LineDowntimeSearchViewModel(IWorkOrderApi api)
    {
        _api = api;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        AddCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync($"{nameof(Pages.LineDowntimeFormPage)}?mode=add"));
        ShowDetailCommand = new AsyncRelayCommand<LineDowntimeCardItem?>(ShowDetailAsync);
        _ = EnsureDictAsync();
    }

    private async Task EnsureDictAsync()
    {
        if (_dictLoaded) return;
        try
        {
            StatusOptions.Clear();
            StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
            var resp = await _api.GetLineDowntimeDictAsync();
            var statusDict = resp.result?.FirstOrDefault(x => string.Equals(x.field, "recordStatus", StringComparison.OrdinalIgnoreCase))
                             ?? resp.result?.FirstOrDefault();
            if (statusDict?.dictItems is not null)
            {
                foreach (var item in statusDict.dictItems)
                {
                    if (string.IsNullOrWhiteSpace(item.dictItemValue)) continue;
                    var text = item.dictItemName ?? item.dictItemValue!;
                    StatusOptions.Add(new StatusOption { Text = text, Value = item.dictItemValue });
                    _statusMap[item.dictItemValue!] = text;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LineDowntime] Load dict error: {ex}");
            if (StatusOptions.Count == 0) StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
        }
        finally
        {
            SelectedStatusOption ??= StatusOptions.FirstOrDefault();
            _dictLoaded = true;
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
        var start = OccurStartDate.Date.Add(OccurStartTime);
        var end = OccurEndDate.Date.Add(OccurEndTime);
        var page = await _api.PageLineDowntimeAsync(start, end, SelectedStatusOption?.Value, pageNo, PageSize, true);
        var records = page?.result?.records ?? new List<LineDowntimeRecord>();
        return records.Select(x => new LineDowntimeCardItem(x, MapStatus(x.recordStatus))).ToList();
    }

    private string MapStatus(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : (_statusMap.TryGetValue(value!, out var name) ? name : value!);
}

public sealed class LineDowntimeCardItem
{
    public LineDowntimeRecord Source { get; }
    public string StatusText { get; }
    public string StatusColor { get; }
    public string BorderColor { get; }
    public bool IsClosed { get; }

    public LineDowntimeCardItem(LineDowntimeRecord source, string statusText)
    {
        Source = source;
        StatusText = statusText;
        IsClosed = statusText.Contains("复工") || statusText.Contains("完成") || string.Equals(source.recordStatus, "1", StringComparison.OrdinalIgnoreCase);
        StatusColor = IsClosed ? "#35C87A" : "#FF8A22";
        BorderColor = IsClosed ? "#4BD889" : "#FF7A1A";
    }
}
