using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameEmptyAddViewModel : ObservableObject
{
    private const int PickerPageSize = 7;
    private int _pickerPageNo = 1;
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<FrameEmptyAddFrameItem> PickerFrameList { get; } = new();
    public ObservableCollection<FrameEmptyAddFrameItem> SelectedFrames { get; } = new();

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
        _pickerPageNo = 1;
        var resp = await _api.GetFrameReturnSelectableListForEmptyAddAsync(_pickerPageNo, PickerPageSize);
        PickerFrameList.Clear();
        foreach (var item in resp?.result?.records ?? new List<FrameEmptyAddFrameItem>())
        {
            item.IsSelected = SelectedFrames.Any(x => IsSameFrame(x, item));
            item.frameStatusDisplay = ResolveFrameStatusDisplay(item.frameStatus);
            PickerFrameList.Add(item);
        }

        IsPickerVisible = true;
    }

    [RelayCommand]
    private async Task LoadMorePickerFramesAsync()
    {
        if (!IsPickerVisible) return;
        await EnsureFrameStatusDictLoadedAsync();
        var nextPage = _pickerPageNo + 1;
        var resp = await _api.GetFrameReturnSelectableListForEmptyAddAsync(nextPage, PickerPageSize);
        var rows = resp?.result?.records ?? new List<FrameEmptyAddFrameItem>();
        if (rows.Count == 0) return;
        _pickerPageNo = nextPage;
        foreach (var item in rows.Where(x => !PickerFrameList.Any(existing => IsSameFrame(existing, x))))
        {
            item.IsSelected = SelectedFrames.Any(x => IsSameFrame(x, item));
            item.frameStatusDisplay = ResolveFrameStatusDisplay(item.frameStatus);
            PickerFrameList.Add(item);
        }
    }

    public async Task ScanAndAddFrameAsync(INavigation nav)
    {
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;

        await EnsureFrameStatusDictLoadedAsync();
        // 与“列表选择”使用同一个分页查询接口，仅额外传入扫码得到的 frameNo。
        var resp = await _api.GetFrameReturnSelectableListForEmptyAddAsync(1, PickerPageSize, frameNo);
        var item = resp?.result?.records?.FirstOrDefault(x => string.Equals(x.frameNo?.Trim(), frameNo, StringComparison.OrdinalIgnoreCase))
            ?? resp?.result?.records?.FirstOrDefault();
        if (item is null)
        {
            await ShowTipAsync("未查询到该料框");
            return;
        }

        item.frameStatusDisplay = ResolveFrameStatusDisplay(item.frameStatus);

        if (SelectedFrames.Any(x => IsSameFrame(x, item)))
        {
            await ShowTipAsync("该料框已添加");
            return;
        }

        SelectedFrames.Add(item);

        var listed = PickerFrameList.FirstOrDefault(x => IsSameFrame(x, item));
        if (listed is not null) listed.IsSelected = true;

        Refresh();
        OnPropertyChanged(nameof(SelectedCountText));
    }

    [RelayCommand] private void ClosePicker() => IsPickerVisible = false;

    [RelayCommand]
    private void TogglePick(FrameEmptyAddFrameItem? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void ConfirmPick()
    {
        foreach (var item in PickerFrameList)
        {
            var selected = SelectedFrames.FirstOrDefault(x => IsSameFrame(x, item));
            if (item.IsSelected)
            {
                if (selected is null)
                    SelectedFrames.Add(item);
            }
            else if (selected is not null)
            {
                SelectedFrames.Remove(selected);
            }
        }

        IsPickerVisible = false;
        Refresh();
        OnPropertyChanged(nameof(SelectedCountText));
    }

    [RelayCommand]
    private void RemoveSelected(FrameEmptyAddFrameItem? item)
    {
        if (item is null) return;
        SelectedFrames.Remove(item);
        var source = PickerFrameList.FirstOrDefault(x => IsSameFrame(x, item));
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
            frameStatusIdList = ids
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

    private static bool IsSameFrame(FrameEmptyAddFrameItem left, FrameEmptyAddFrameItem right)
    {
        if (!string.IsNullOrWhiteSpace(left.id) && !string.IsNullOrWhiteSpace(right.id))
            return string.Equals(left.id, right.id, StringComparison.OrdinalIgnoreCase);

        return !string.IsNullOrWhiteSpace(left.frameNo)
            && !string.IsNullOrWhiteSpace(right.frameNo)
            && string.Equals(left.frameNo.Trim(), right.frameNo.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ShowTipAsync(string message)
    {
        if (Shell.Current?.CurrentPage is Page p)
            await p.DisplayAlert("提示", message, "确定");
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
        var dict = (statusField?.dictItems ?? new List<DictItem>())
            .Where(x => !string.IsNullOrWhiteSpace(x.dictItemValue))
            .GroupBy(x => x.dictItemValue!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                k => k.Key,
                v => string.IsNullOrWhiteSpace(v.First().dictItemName) ? v.First().dictItemValue! : v.First().dictItemName!,
                StringComparer.OrdinalIgnoreCase);
        _frameStatusDict = dict;
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }
}
