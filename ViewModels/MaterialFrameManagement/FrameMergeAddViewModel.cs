using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameMergeAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<FrameMergeAddFrameItem> SourceFrameList { get; } = new();
    public ObservableCollection<FrameMergeAddTargetFrameItem> TargetFrameList { get; } = new();
    public ObservableCollection<FrameMergeAddFrameItem> SelectedSourceFrames { get; } = new();

    [ObservableProperty] private FrameMergeAddTargetFrameItem? selectedTargetFrame;
    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool canConfirm;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#D1D5DB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");

    public bool HasTargetFrame => SelectedTargetFrame is not null;
    public string TotalQtyDisplay => $"{SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new List<FrameMergeAddLoadDetailItem>()).Sum(x => x.qty ?? 0):0.##}";
    public string TotalMaterialNameDisplay => SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new List<FrameMergeAddLoadDetailItem>()).FirstOrDefault()?.materialName ?? "-";

    public FrameMergeAddViewModel(IMaterialFrameApi api) => _api = api;

    [RelayCommand] private async Task OpenSourcePickerAsync() { await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetMaterialFrameListForTransferAddAsync(); SourceFrameList.Clear(); foreach (var x in r?.result ?? new()) { var m = MapSource(x); m.frameStatusDisplay = ResolveFrameStatusDisplay(m.frameStatus); SourceFrameList.Add(m); } IsSourcePickerVisible = true; }
    [RelayCommand] private void CloseSourcePicker() => IsSourcePickerVisible = false;
    [RelayCommand] private void ToggleSource(FrameMergeAddFrameItem? i) { if (i is null) return; i.IsSelected = !i.IsSelected; }
    [RelayCommand] private void ConfirmSource() { SelectedSourceFrames.Clear(); foreach (var x in SourceFrameList.Where(x => x.IsSelected)) SelectedSourceFrames.Add(x); IsSourcePickerVisible = false; OnPropertyChanged(nameof(TotalQtyDisplay)); OnPropertyChanged(nameof(TotalMaterialNameDisplay)); Refresh(); }
    [RelayCommand] private void RemoveSource(FrameMergeAddFrameItem? i){ if(i is null) return; SelectedSourceFrames.Remove(i); var src=SourceFrameList.FirstOrDefault(x=>x.id==i.id); if(src is not null) src.IsSelected=false; OnPropertyChanged(nameof(TotalQtyDisplay)); OnPropertyChanged(nameof(TotalMaterialNameDisplay)); Refresh(); }

    [RelayCommand] private async Task OpenTargetPickerAsync() { if(SelectedSourceFrames.Count==0) { await ShowSelectSourceTipAsync(); return; } var codes = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => x != "").Distinct().ToList(); var names = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => x != "").Distinct().ToList(); await EnsureFrameStatusDictLoadedAsync(); var r = await _api.GetFrameStatusListForTransferAddAsync(codes, names, null); TargetFrameList.Clear(); foreach (var x in r?.result ?? new()) { var m = MapTarget(x); m.frameStatusDisplay = ResolveFrameStatusDisplay(m.frameStatus); TargetFrameList.Add(m); } IsTargetPickerVisible = true; }
    [RelayCommand] private void CloseTargetPicker() => IsTargetPickerVisible = false;
    [RelayCommand] private void PickTarget(FrameMergeAddTargetFrameItem? i) { if (i is null) return; SelectedTargetFrame = i; foreach (var x in TargetFrameList) x.IsSelected = ReferenceEquals(x, i); IsTargetPickerVisible = false; Refresh(); OnPropertyChanged(nameof(HasTargetFrame)); }
    [RelayCommand] private void ConfirmTarget() { IsTargetPickerVisible = false; Refresh(); }
    [RelayCommand] private void ClearTarget(){ SelectedTargetFrame=null; foreach(var x in TargetFrameList)x.IsSelected=false; Refresh(); OnPropertyChanged(nameof(HasTargetFrame)); }


    public async Task ScanAndAddSourceFrameAsync(INavigation nav)
    {
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;

        await EnsureFrameStatusDictLoadedAsync();
        var r = await _api.GetMaterialFrameListForTransferAddAsync(frameNo);
        var source = r?.result?.Select(MapSource).FirstOrDefault();
        if (source is null) return;

        source.frameStatusDisplay = ResolveFrameStatusDisplay(source.frameStatus);
        if (SelectedSourceFrames.Any(x => string.Equals(x.id, source.id, StringComparison.OrdinalIgnoreCase))) return;

        SelectedSourceFrames.Add(source);
        var listed = SourceFrameList.FirstOrDefault(x => string.Equals(x.id, source.id, StringComparison.OrdinalIgnoreCase));
        if (listed is not null) listed.IsSelected = true;

        OnPropertyChanged(nameof(TotalQtyDisplay));
        OnPropertyChanged(nameof(TotalMaterialNameDisplay));
        Refresh();
    }

    public async Task ScanAndPickTargetFrameAsync(INavigation nav)
    {
        if (SelectedSourceFrames.Count == 0) { await ShowSelectSourceTipAsync(); return; }
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;

        var codes = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => x != "").Distinct().ToList();
        var names = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => x != "").Distinct().ToList();
        await EnsureFrameStatusDictLoadedAsync();
        var r = await _api.GetFrameStatusListForTransferAddAsync(codes, names, frameNo);
        var target = r?.result?.Select(MapTarget).FirstOrDefault();
        if (target is null) return;

        target.frameStatusDisplay = ResolveFrameStatusDisplay(target.frameStatus);

        SelectedTargetFrame = target;
        foreach (var x in TargetFrameList) x.IsSelected = string.Equals(x.id, target.id, StringComparison.OrdinalIgnoreCase);
        Refresh();
        OnPropertyChanged(nameof(HasTargetFrame));
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm || SelectedTargetFrame is null || SelectedSourceFrames.Count == 0) return;

        var materialDetails = new List<AddFrameMergingMaterialDetail>();
        foreach (var frame in SelectedSourceFrames)
        foreach (var m in frame.loadDetailList ?? new List<FrameMergeAddLoadDetailItem>())
            materialDetails.Add(new AddFrameMergingMaterialDetail
            {
                batchNo = string.Equals(frame.frameStatus, "warehouse", StringComparison.OrdinalIgnoreCase) ? m.batchNo : null,
                materialCode = m.materialCode,
                materialName = m.materialName,
                qty = m.qty ?? 0,
                sourceFrameNo = frame.frameNo,
                unit = string.IsNullOrWhiteSpace(m.unit) ? null : m.unit
            });

        var req = new AddFrameMergingRecordReq
        {
            memo = "",
            targetFrameStatusId = SelectedTargetFrame.id,
            sourceFrameStatusIdList = SelectedSourceFrames.Select(x => x.id ?? "").Where(x => x != "").ToList(),
            materialDetails = materialDetails
        };

        var resp = await _api.AddFrameMergingRecordAsync(req);
        if (resp?.success == true && resp.result == true)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(resp?.message) ? "合框失败，请稍后重试" : resp!.message!;
        if (Shell.Current?.CurrentPage is Page p)
            await p.DisplayAlert("提示", msg, "确定");
    }

    private void Refresh()
    {
        CanConfirm = SelectedSourceFrames.Count > 0 && SelectedTargetFrame is not null;
        ConfirmButtonColor = CanConfirm ? Color.FromArgb("#4F46E5") : Color.FromArgb("#D1D5DB");
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

    private static FrameMergeAddFrameItem MapSource(FrameUnloadAddSourceFrameItem x) => new()
    {
        id = x.id, frameNo = x.frameNo, frameStatus = x.frameStatus, frameStatusDisplay = x.frameStatusDisplay, frameTypeCode = x.frameTypeCode, frameTypeName = x.frameTypeName, IsSelected = x.IsSelected,
        loadDetailList = (x.loadDetailList ?? new()).Select(m => new FrameMergeAddLoadDetailItem { materialCode = m.materialCode, materialName = m.materialName, batchNo = m.batchNo, unit = m.unit, qty = m.qty }).ToList()
    };
    private static FrameMergeAddTargetFrameItem MapTarget(FrameUnloadAddTargetFrameItem x) => new() { id = x.id, frameNo = x.frameNo, frameStatus = x.frameStatus, frameStatusDisplay = x.frameStatusDisplay, frameTypeCode = x.frameTypeCode, frameTypeName = x.frameTypeName, IsSelected = x.IsSelected };
}
