using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FramePourAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    public ObservableCollection<FrameStatusItem> SourceFrameList { get; } = new();
    public ObservableCollection<FrameStatusItem> TargetFrameList { get; } = new();
    [ObservableProperty] private FrameStatusItem? selectedSource;
    [ObservableProperty] private FrameStatusItem? selectedTarget;
    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool canConfirm;
    public FramePourAddViewModel(IMaterialFrameApi api)=>_api=api;
    [RelayCommand] private async Task OpenSourcePickerAsync(){ var r=await _api.GetMaterialFrameListAsync(); SourceFrameList.Clear(); foreach(var x in r?.result??new()) SourceFrameList.Add(x); IsSourcePickerVisible=true; }
    [RelayCommand] private void ConfirmSource(){ IsSourcePickerVisible=false; Refresh(); }
    [RelayCommand] private async Task OpenTargetPickerAsync(){ var r=await _api.GetFrameStatusListForUnloadAsync(new(),new()); TargetFrameList.Clear(); foreach(var x in r?.result??new()) TargetFrameList.Add(x); IsTargetPickerVisible=true; }
    [RelayCommand] private void ConfirmTarget(){ IsTargetPickerVisible=false; Refresh(); }
    [RelayCommand] private async Task ConfirmAsync(){ if(!CanConfirm||SelectedSource is null||SelectedTarget is null)return; var req=new AddPouringRecordReq{ sourceFrameId=SelectedSource.id,sourceFrameNo=SelectedSource.frameNo,sourceFrameTypeCode=SelectedSource.frameTypeCode,sourceFrameTypeName=SelectedSource.frameTypeName,targetFrameId=SelectedTarget.id,targetFrameNo=SelectedTarget.frameNo,targetFrameTypeCode=SelectedTarget.frameTypeCode,targetFrameTypeName=SelectedTarget.frameTypeName}; var resp=await _api.AddPouringRecordAsync(req); if(resp?.success==true&&resp.result==true) await Shell.Current.GoToAsync(".."); }
    private void Refresh()=>CanConfirm=SelectedSource is not null && SelectedTarget is not null;
}
