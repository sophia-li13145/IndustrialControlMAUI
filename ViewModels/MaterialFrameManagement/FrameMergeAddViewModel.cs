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

    public FrameMergeAddViewModel(IMaterialFrameApi api) => _api = api;

    [RelayCommand] private async Task OpenSourcePickerAsync(){ var r=await _api.GetMaterialFrameListAsync(); SourceFrameList.Clear(); foreach(var x in r?.result??new()) SourceFrameList.Add(x); IsSourcePickerVisible=true; }
    [RelayCommand] private void ToggleSource(FrameStatusItem? i){ if(i is null)return; i.IsSelected=!i.IsSelected; }
    [RelayCommand] private void ConfirmSource(){ SelectedSourceFrames.Clear(); foreach(var x in SourceFrameList.Where(x=>x.IsSelected)) SelectedSourceFrames.Add(x); IsSourcePickerVisible=false; Refresh(); }
    [RelayCommand] private async Task OpenTargetPickerAsync(){ var codes=SelectedSourceFrames.SelectMany(x=>x.loadDetailList??new()).Select(x=>x.materialCode??"").Where(x=>x!="").Distinct().ToList(); var names=SelectedSourceFrames.SelectMany(x=>x.loadDetailList??new()).Select(x=>x.materialName??"").Where(x=>x!="").Distinct().ToList(); var r=await _api.GetFrameStatusListForUnloadAsync(codes,names); TargetFrameList.Clear(); foreach(var x in r?.result??new()) TargetFrameList.Add(x); IsTargetPickerVisible=true; }
    [RelayCommand] private void PickTarget(FrameStatusItem? i){ if(i is null)return; SelectedTargetFrame=i; foreach(var x in TargetFrameList)x.IsSelected=ReferenceEquals(x,i); }
    [RelayCommand] private void ConfirmTarget(){ IsTargetPickerVisible=false; Refresh(); }
    [RelayCommand] private async Task ConfirmAsync(){ if(!CanConfirm||SelectedTargetFrame is null) return; var req=new AddFrameMergingRecordReq{ memo="", targetFrameStatusId=SelectedTargetFrame.id, sourceFrameStatusIdList=SelectedSourceFrames.Select(x=>x.id??"").Where(x=>x!="").ToList(), materialDetails=SelectedSourceFrames.SelectMany(f=>f.loadDetailList??new()).Select(m=>new AddFrameMergingMaterialDetail{ batchNo=m.batchNo, materialCode=m.materialCode, materialName=m.materialName, qty=m.currentQuantity??m.currentQty??m.quantity??0, sourceFrameNo=SelectedSourceFrames.FirstOrDefault(x=>x.loadDetailList?.Contains(m)==true)?.frameNo, unit=""}).ToList()}; var resp=await _api.AddFrameMergingRecordAsync(req); if(resp?.success==true&&resp.result==true) await Shell.Current.GoToAsync(".."); }
    private void Refresh()=>CanConfirm=SelectedSourceFrames.Count>0 && SelectedTargetFrame is not null;
}
