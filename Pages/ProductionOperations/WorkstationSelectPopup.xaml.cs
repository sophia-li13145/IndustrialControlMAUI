using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class WorkstationSelectPopup : Popup
{
    private readonly WorkstationSelectPopupModel _model;

    public WorkstationSelectPopup(IWorkOrderApi api, IEnumerable<WorkstationInfo> selected)
    {
        InitializeComponent();
        _model = new WorkstationSelectPopupModel(api, selected);
        BindingContext = _model;
        Opened += async (_, _) => await _model.LoadAsync(1);
    }

    private async void OnSearchClicked(object? sender, EventArgs e) => await _model.SearchAsync(SearchEntry.Text);
    private async void OnPreviousClicked(object? sender, EventArgs e) => await _model.LoadAsync(_model.PageNo - 1);
    private async void OnNextClicked(object? sender, EventArgs e) => await _model.LoadAsync(_model.PageNo + 1);
    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid { BindingContext: WorkstationSelectionItem item })
            _model.Toggle(item);
    }
    private void OnSelectAllClicked(object? sender, EventArgs e) => _model.ClearSelection();
    private void OnCancelClicked(object? sender, EventArgs e) => Close(null);
    private void OnConfirmClicked(object? sender, EventArgs e) => Close(_model.GetSelection());
}

public partial class WorkstationSelectPopupModel : ObservableObject
{
    private const int PageSize = 10;
    private readonly IWorkOrderApi _api;
    private readonly Dictionary<string, WorkstationInfo> _selected = new(StringComparer.OrdinalIgnoreCase);
    private string? _keyword;
    [ObservableProperty] private int pageNo = 1;
    [ObservableProperty] private int total;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string selectionSummary = "当前：全部工位";
    public ObservableCollection<WorkstationSelectionItem> Items { get; } = new();
    public bool CanGoPrevious => !IsBusy && PageNo > 1;
    public bool CanGoNext => !IsBusy && PageNo * PageSize < Total;
    public string PageText => $"第 {PageNo} 页";

    public WorkstationSelectPopupModel(IWorkOrderApi api, IEnumerable<WorkstationInfo> selected)
    {
        _api = api;
        foreach (var item in selected)
            if (!string.IsNullOrWhiteSpace(item.workstationCode)) _selected[item.workstationCode.Trim()] = item;
        UpdateSelectionSummary();
    }

    public async Task SearchAsync(string? keyword) { _keyword = keyword?.Trim(); await LoadAsync(1); }

    public async Task LoadAsync(int page)
    {
        if (IsBusy || page < 1) return;
        IsBusy = true;
        NotifyPaging();
        try
        {
            var response = await _api.PageWorkstationListAsync(page, PageSize, _keyword);
            var result = response?.result;
            PageNo = result is { pageNo: > 0 } ? result.pageNo : page;
            Total = result?.total ?? 0;
            Items.Clear();
            foreach (var station in result?.records ?? Enumerable.Empty<WorkstationInfo>())
            {
                var code = station.workstationCode?.Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;
                Items.Add(new WorkstationSelectionItem(station, _selected.ContainsKey(code)));
            }
        }
        finally { IsBusy = false; NotifyPaging(); }
    }

    public void Toggle(WorkstationSelectionItem item)
    {
        item.IsSelected = !item.IsSelected;
        if (item.IsSelected) _selected[item.Code] = item.Source; else _selected.Remove(item.Code);
        UpdateSelectionSummary();
    }

    public void ClearSelection()
    {
        _selected.Clear();
        foreach (var item in Items) item.IsSelected = false;
        UpdateSelectionSummary();
    }

    public IReadOnlyCollection<WorkstationInfo> GetSelection() => _selected.Values.ToArray();
    private void UpdateSelectionSummary() => SelectionSummary = _selected.Count == 0 ? "当前：全部工位" : $"已选择 {_selected.Count} 个工位";
    private void NotifyPaging() { OnPropertyChanged(nameof(CanGoPrevious)); OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(PageText)); }
    partial void OnPageNoChanged(int value) => NotifyPaging();
    partial void OnTotalChanged(int value) => NotifyPaging();
}

public partial class WorkstationSelectionItem : ObservableObject
{
    public WorkstationInfo Source { get; }
    public string Code => Source.workstationCode ?? "";
    public string Name => Source.workstationName ?? Code;
    [ObservableProperty] private bool isSelected;
    public WorkstationSelectionItem(WorkstationInfo source, bool selected) { Source = source; isSelected = selected; }
}
