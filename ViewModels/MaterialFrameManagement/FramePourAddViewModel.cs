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
    public ObservableCollection<FrameStatusItem> SourceFrameList { get; } = new();
    public ObservableCollection<FrameStatusItem> TargetFrameList { get; } = new();
    [ObservableProperty] private FrameStatusItem? selectedSource;
    [ObservableProperty] private FrameStatusItem? selectedTarget;
    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool canConfirm;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#E5E7EB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");
    public bool HasSelectedSource => SelectedSource is not null;
    public bool HasSelectedTarget => SelectedTarget is not null;
    public FramePourAddViewModel(IMaterialFrameApi api) => _api = api;
    [RelayCommand] private async Task OpenSourcePickerAsync() { await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetMaterialFrameListAsync(); SourceFrameList.Clear(); foreach (var x in r?.result ?? new()) { x.IsSelected = string.Equals(x.id, SelectedSource?.id, StringComparison.OrdinalIgnoreCase); x.frameStatusDisplay = ResolveFrameStatusDisplay(x.frameStatus); SourceFrameList.Add(x); } IsSourcePickerVisible = true; }
    [RelayCommand] private void PickSource(FrameStatusItem? i) { if (i is null) return; SelectedSource = i; foreach (var x in SourceFrameList) x.IsSelected = ReferenceEquals(x, i); IsSourcePickerVisible = false; OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    [RelayCommand] private void ConfirmSource() { IsSourcePickerVisible = false; Refresh(); }
    [RelayCommand] private async Task OpenTargetPickerAsync() { await EnsureFrameStatusDictLoadedAsync(); var materialCodes = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var materialNames = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var r = await _api.GetFrameStatusListForUnloadAsync(materialCodes, materialNames, null); TargetFrameList.Clear(); foreach (var x in r?.result ?? new()) { x.IsSelected = string.Equals(x.id, SelectedTarget?.id, StringComparison.OrdinalIgnoreCase); x.frameStatusDisplay = ResolveFrameStatusDisplay(x.frameStatus); TargetFrameList.Add(x); } IsTargetPickerVisible = true; }
    [RelayCommand] private void PickTarget(FrameStatusItem? i) { if (i is null) return; SelectedTarget = i; foreach (var x in TargetFrameList) x.IsSelected = ReferenceEquals(x, i); IsTargetPickerVisible = false; OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    [RelayCommand] private void ConfirmTarget() { IsTargetPickerVisible = false; Refresh(); }
    [RelayCommand] private void ClearSource() { SelectedSource = null; foreach (var x in SourceFrameList) x.IsSelected = false; OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    [RelayCommand] private void ClearTarget() { SelectedTarget = null; foreach (var x in TargetFrameList) x.IsSelected = false; OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    public async Task ScanAndPickSourceFrameAsync(INavigation nav) { var tcs = new TaskCompletionSource<string>(); await nav.PushAsync(new QrScanPage(tcs)); var frameNo = (await tcs.Task)?.Trim(); if (string.IsNullOrWhiteSpace(frameNo)) return; await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetMaterialFrameListAsync(frameNo); var source = r?.result?.FirstOrDefault(); if (source is null) return; source.frameStatusDisplay = ResolveFrameStatusDisplay(source.frameStatus); SelectedSource = source; OnPropertyChanged(nameof(HasSelectedSource)); Refresh(); }
    public async Task ScanAndPickTargetFrameAsync(INavigation nav) { var tcs = new TaskCompletionSource<string>(); await nav.PushAsync(new QrScanPage(tcs)); var frameNo = (await tcs.Task)?.Trim(); if (string.IsNullOrWhiteSpace(frameNo)) return; await EnsureFrameStatusDictLoadedAsync(); var materialCodes = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var materialNames = (SelectedSource?.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); var r = await _api.GetFrameStatusListForUnloadAsync(materialCodes, materialNames, frameNo); var target = r?.result?.FirstOrDefault(); if (target is null) return; target.frameStatusDisplay = ResolveFrameStatusDisplay(target.frameStatus); SelectedTarget = target; OnPropertyChanged(nameof(HasSelectedTarget)); Refresh(); }
    [RelayCommand] private async Task ConfirmAsync() { if (!CanConfirm || SelectedSource is null || SelectedTarget is null) return; var req = new AddPouringRecordReq { sourceFrameId = SelectedSource.id, sourceFrameNo = SelectedSource.frameNo, sourceFrameTypeCode = SelectedSource.frameTypeCode, sourceFrameTypeName = SelectedSource.frameTypeName, targetFrameId = SelectedTarget.id, targetFrameNo = SelectedTarget.frameNo, targetFrameTypeCode = SelectedTarget.frameTypeCode, targetFrameTypeName = SelectedTarget.frameTypeName }; var resp = await _api.AddPouringRecordAsync(req); if (resp?.success == true && resp.result == true) await Shell.Current.GoToAsync(".."); }
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
        _frameStatusDict = statusField?.dictDataList?
            .Where(x => !string.IsNullOrWhiteSpace(x.dictValue) && !string.IsNullOrWhiteSpace(x.dictLabel))
            .GroupBy(x => x.dictValue!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().dictLabel!.Trim(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }
}
