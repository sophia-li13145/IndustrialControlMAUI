using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameMergeAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    public ObservableCollection<FrameStatusItem> SourceFrameList { get; } = new();
    public ObservableCollection<FrameStatusItem> TargetFrameList { get; } = new();
    public ObservableCollection<FrameStatusItem> SelectedSourceFrames { get; } = new();

    [ObservableProperty] private FrameStatusItem? selectedTargetFrame;
    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool canConfirm;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#D1D5DB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");

    public bool HasTargetFrame => SelectedTargetFrame is not null;
    public string TotalQtyDisplay => $"{SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new List<MaterialFrameLoadDetail>()).Sum(x => x.currentQuantity ?? x.currentQty ?? x.quantity ?? 0):0.##}";
    public string TotalMaterialNameDisplay => SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new List<MaterialFrameLoadDetail>()).FirstOrDefault()?.materialName ?? "-";

    public FrameMergeAddViewModel(IMaterialFrameApi api) => _api = api;

    [RelayCommand] private async Task OpenSourcePickerAsync() { var r = await _api.GetMaterialFrameListAsync(); SourceFrameList.Clear(); foreach (var x in r?.result ?? new()) SourceFrameList.Add(x); IsSourcePickerVisible = true; }
    [RelayCommand] private void CloseSourcePicker() => IsSourcePickerVisible = false;
    [RelayCommand] private void ToggleSource(FrameStatusItem? i) { if (i is null) return; i.IsSelected = !i.IsSelected; }
    [RelayCommand] private void ConfirmSource() { SelectedSourceFrames.Clear(); foreach (var x in SourceFrameList.Where(x => x.IsSelected)) SelectedSourceFrames.Add(x); IsSourcePickerVisible = false; OnPropertyChanged(nameof(TotalQtyDisplay)); OnPropertyChanged(nameof(TotalMaterialNameDisplay)); Refresh(); }
    [RelayCommand] private void RemoveSource(FrameStatusItem? i){ if(i is null) return; SelectedSourceFrames.Remove(i); var src=SourceFrameList.FirstOrDefault(x=>x.id==i.id); if(src is not null) src.IsSelected=false; OnPropertyChanged(nameof(TotalQtyDisplay)); OnPropertyChanged(nameof(TotalMaterialNameDisplay)); Refresh(); }

    [RelayCommand] private async Task OpenTargetPickerAsync() { if(SelectedSourceFrames.Count==0) return; var codes = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialCode ?? "").Where(x => x != "").Distinct().ToList(); var names = SelectedSourceFrames.SelectMany(x => x.loadDetailList ?? new()).Select(x => x.materialName ?? "").Where(x => x != "").Distinct().ToList(); var r = await _api.GetFrameStatusListForUnloadAsync(codes, names); TargetFrameList.Clear(); foreach (var x in r?.result ?? new()) TargetFrameList.Add(x); IsTargetPickerVisible = true; }
    [RelayCommand] private void CloseTargetPicker() => IsTargetPickerVisible = false;
    [RelayCommand] private void PickTarget(FrameStatusItem? i) { if (i is null) return; SelectedTargetFrame = i; foreach (var x in TargetFrameList) x.IsSelected = ReferenceEquals(x, i); IsTargetPickerVisible = false; Refresh(); OnPropertyChanged(nameof(HasTargetFrame)); }
    [RelayCommand] private void ConfirmTarget() { IsTargetPickerVisible = false; Refresh(); }
    [RelayCommand] private void ClearTarget(){ SelectedTargetFrame=null; foreach(var x in TargetFrameList)x.IsSelected=false; Refresh(); OnPropertyChanged(nameof(HasTargetFrame)); }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm || SelectedTargetFrame is null || SelectedSourceFrames.Count == 0) return;

        var materialDetails = new List<AddFrameMergingMaterialDetail>();
        foreach (var frame in SelectedSourceFrames)
        foreach (var m in frame.loadDetailList ?? new List<MaterialFrameLoadDetail>())
            materialDetails.Add(new AddFrameMergingMaterialDetail
            {
                batchNo = string.Equals(frame.frameStatus, "warehouse", StringComparison.OrdinalIgnoreCase) ? m.batchNo : null,
                materialCode = m.materialCode,
                materialName = m.materialName,
                qty = m.currentQuantity ?? m.currentQty ?? m.quantity ?? 0,
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
}
