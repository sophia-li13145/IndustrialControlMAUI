using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FramePourAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<FramePourAddSourceFrameItem> SourceFrameList { get; } = new();
    public ObservableCollection<FramePourAddTargetFrameItem> TargetFrameList { get; } = new();
    [ObservableProperty] private FramePourAddSourceFrameItem? selectedSource;
    [ObservableProperty] private FramePourAddTargetFrameItem? selectedTarget;
    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool canConfirm;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#E5E7EB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");
    public bool HasSelectedSource => SelectedSource is not null;
    public bool HasSelectedTarget => SelectedTarget is not null;
    public FramePourAddViewModel(IMaterialFrameApi api) => _api = api;
    [RelayCommand] private async Task OpenSourcePickerAsync() { await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetMaterialFrameListForTransferAddAsync(); SourceFrameList.Clear(); foreach (var x in r?.result ?? new()) { var m = MapSource(x); m.IsSelected = string.Equals(m.id, SelectedSource?.id, StringComparison.OrdinalIgnoreCase); m.frameStatusDisplay = ResolveFrameStatusDisplay(m.frameStatus); SourceFrameList.Add(m); } IsSourcePickerVisible = true; }
    [RelayCommand] private void PickSource(FramePourAddSourceFrameItem? i) { if (i is null) return; SelectedSource = i; foreach (var x in SourceFrameList) x.IsSelected = ReferenceEquals(x, i); OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    [RelayCommand] private void ConfirmSource() { IsSourcePickerVisible = false; Refresh(); }
    [RelayCommand] private async Task OpenTargetPickerAsync() { if (SelectedSource is null) { await ShowSelectSourceTipAsync(); return; } await EnsureFrameStatusDictLoadedAsync(); var materialCodes = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var materialNames = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var r = await _api.GetFrameStatusListForTransferAddAsync(materialCodes, materialNames, null); TargetFrameList.Clear(); foreach (var x in r?.result ?? new()) { var m = MapTarget(x); m.IsSelected = string.Equals(m.id, SelectedTarget?.id, StringComparison.OrdinalIgnoreCase); m.frameStatusDisplay = ResolveFrameStatusDisplay(m.frameStatus); TargetFrameList.Add(m); } IsTargetPickerVisible = true; }
    [RelayCommand] private void PickTarget(FramePourAddTargetFrameItem? i) { if (i is null) return; SelectedTarget = i; foreach (var x in TargetFrameList) x.IsSelected = ReferenceEquals(x, i); OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    [RelayCommand] private void ConfirmTarget() { IsTargetPickerVisible = false; Refresh(); }
    [RelayCommand] private void ClearSource() { SelectedSource = null; foreach (var x in SourceFrameList) x.IsSelected = false; OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    [RelayCommand] private void ClearTarget() { SelectedTarget = null; foreach (var x in TargetFrameList) x.IsSelected = false; OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    public async Task ScanAndPickSourceFrameAsync(INavigation nav) { var tcs = new TaskCompletionSource<string>(); await nav.PushAsync(new QrScanPage(tcs)); var frameNo = (await tcs.Task)?.Trim(); if (string.IsNullOrWhiteSpace(frameNo)) return; await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetMaterialFrameListForTransferAddAsync(frameNo); var source = r?.result?.Select(MapSource).FirstOrDefault(); if (source is null) return; source.frameStatusDisplay = ResolveFrameStatusDisplay(source.frameStatus); SelectedSource = source; OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    public async Task ScanAndPickTargetFrameAsync(INavigation nav) { if (SelectedSource is null) { await ShowSelectSourceTipAsync(); return; } var tcs = new TaskCompletionSource<string>(); await nav.PushAsync(new QrScanPage(tcs)); var frameNo = (await tcs.Task)?.Trim(); if (string.IsNullOrWhiteSpace(frameNo)) return; await EnsureFrameStatusDictLoadedAsync(); var materialCodes = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var materialNames = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var r = await _api.GetFrameStatusListForTransferAddAsync(materialCodes, materialNames, frameNo); var target = r?.result?.Select(MapTarget).FirstOrDefault(); if (target is null) return; target.frameStatusDisplay = ResolveFrameStatusDisplay(target.frameStatus); SelectedTarget = target; OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm || SelectedSource is null || SelectedTarget is null) return;

        var req = new AddPouringRecordReq
        {
            sourceFrameId = SelectedSource.id,
            sourceFrameNo = SelectedSource.frameNo,
            sourceFrameTypeCode = SelectedSource.frameTypeCode,
            sourceFrameTypeName = SelectedSource.frameTypeName,
            targetFrameId = SelectedTarget.id,
            targetFrameNo = SelectedTarget.frameNo,
            targetFrameTypeCode = SelectedTarget.frameTypeCode,
            targetFrameTypeName = SelectedTarget.frameTypeName
        };

        var resp = await _api.AddPouringRecordAsync(req);
        if (resp?.success == true && resp.result == true)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(resp?.message) ? "倒框失败，请稍后重试" : resp!.message!;
        if (Shell.Current?.CurrentPage is Page p)
            await p.DisplayAlert("提示", msg, "确定");
    }
    private void Refresh()
    {
        CanConfirm = SelectedSource is not null && SelectedTarget is not null;
        ConfirmButtonColor = CanConfirm ? Color.FromArgb("#F97316") : Color.FromArgb("#E5E7EB");
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

    private async Task ShowSelectSourceTipAsync()
    {
        if (Shell.Current?.CurrentPage is Page p)
            await p.DisplayAlert("提示", "请选择原料框", "确定");
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }

    private static FramePourAddSourceFrameItem MapSource(FrameUnloadAddSourceFrameItem x) => new() { id = x.id, frameNo = x.frameNo, frameStatus = x.frameStatus, frameStatusDisplay = x.frameStatusDisplay, frameTypeCode = x.frameTypeCode, frameTypeName = x.frameTypeName, IsSelected = x.IsSelected, loadDetailList = (x.loadDetailList ?? new()).Select(m => new FramePourAddLoadDetailItem { materialCode = m.materialCode, materialName = m.materialName }).ToList() };
    private static FramePourAddTargetFrameItem MapTarget(FrameUnloadAddTargetFrameItem x) => new() { id = x.id, frameNo = x.frameNo, frameStatus = x.frameStatus, frameStatusDisplay = x.frameStatusDisplay, frameTypeCode = x.frameTypeCode, frameTypeName = x.frameTypeName, IsSelected = x.IsSelected };
}
