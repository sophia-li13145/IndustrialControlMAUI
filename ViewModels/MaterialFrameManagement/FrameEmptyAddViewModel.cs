using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameEmptyAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<FrameStatusItem> PickerFrameList { get; } = new();
    public ObservableCollection<FrameStatusItem> SelectedFrames { get; } = new();

    [ObservableProperty] private bool isPickerVisible;
    [ObservableProperty] private bool canConfirm;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#D1D5DB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");

    public string SelectedCountText => SelectedFrames.Count.ToString();

    public FrameEmptyAddViewModel(IMaterialFrameApi api) => _api = api;

    [RelayCommand]
    private async Task OpenPickerAsync()
    {
        await EnsureFrameStatusDictLoadedAsync();
        var resp = await _api.GetFrameReturnSelectableListAsync(1, 10);
        PickerFrameList.Clear();
        foreach (var item in resp?.result?.records ?? new List<FrameStatusItem>())
        {
            item.IsSelected = SelectedFrames.Any(x => string.Equals(x.id, item.id, StringComparison.OrdinalIgnoreCase));
            item.frameStatusDisplay = ResolveFrameStatusDisplay(item.frameStatus);
            PickerFrameList.Add(item);
        }

        IsPickerVisible = true;
    }

    [RelayCommand] private void ClosePicker() => IsPickerVisible = false;

    [RelayCommand]
    private void TogglePick(FrameStatusItem? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void ConfirmPick()
    {
        SelectedFrames.Clear();
        foreach (var item in PickerFrameList.Where(x => x.IsSelected))
            SelectedFrames.Add(item);

        IsPickerVisible = false;
        Refresh();
        OnPropertyChanged(nameof(SelectedCountText));
    }

    [RelayCommand]
    private void RemoveSelected(FrameStatusItem? item)
    {
        if (item is null) return;
        SelectedFrames.Remove(item);
        var source = PickerFrameList.FirstOrDefault(x => x.id == item.id);
        if (source is not null) source.IsSelected = false;
        Refresh();
        OnPropertyChanged(nameof(SelectedCountText));
    }

    [RelayCommand]
    private void ClearAll()
    {
        SelectedFrames.Clear();
        foreach (var item in PickerFrameList) item.IsSelected = false;
        Refresh();
        OnPropertyChanged(nameof(SelectedCountText));
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm) return;
        var ids = SelectedFrames.Select(x => x.id).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList();
        var req = new AddFrameReturnRecordReq
        {
            memo = string.Empty,
            frameStatusIdList = string.Join(',', ids)
        };

        var resp = await _api.AddFrameReturnRecordAsync(req);
        if (resp?.success == true && resp.result == true)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(resp?.message) ? "释放失败，请稍后重试" : resp!.message!;
        if (Shell.Current?.CurrentPage is Page p)
            await p.DisplayAlert("提示", msg, "确定");
    }

    private void Refresh()
    {
        CanConfirm = SelectedFrames.Count > 0;
        ConfirmButtonColor = CanConfirm ? Color.FromArgb("#EF4444") : Color.FromArgb("#D1D5DB");
        ConfirmButtonTextColor = CanConfirm ? Colors.White : Color.FromArgb("#9CA3AF");
    }

    private async Task EnsureFrameStatusDictLoadedAsync()
    {
        if (_frameStatusDict.Count > 0) return;
        var fields = await _api.GetStatusDictListAsync();
        var statusField = fields?.FirstOrDefault(x => string.Equals(x.field, "frameStatus", StringComparison.OrdinalIgnoreCase));
        var dict = statusField?.dictDataList?
            .Where(x => !string.IsNullOrWhiteSpace(x.dictValue) && !string.IsNullOrWhiteSpace(x.dictLabel))
            .GroupBy(x => x.dictValue!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().dictLabel!.Trim(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _frameStatusDict = dict;
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }
}
