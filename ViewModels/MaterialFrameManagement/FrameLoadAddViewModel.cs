using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameLoadAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public ObservableCollection<BasMaterialRecord> MaterialList { get; } = new();
    public ObservableCollection<TargetFrameSelectableItem> TargetFrameList { get; } = new();
    public ObservableCollection<SelectedTargetFrameItem> SelectedTargetFrames { get; } = new();

    [ObservableProperty] private string? materialNameKeyword;
    [ObservableProperty] private string selectedMaterialName = "请选择";
    [ObservableProperty] private string? selectedMaterialCode;
    [ObservableProperty] private bool isPickerVisible;
    [ObservableProperty] private bool isTargetFramePopupVisible;
    [ObservableProperty] private int selectedTargetFrameCount;

    public FrameLoadAddViewModel(IMaterialFrameApi api) => _api = api;

    public async Task LoadMaterialsAsync()
    {
        var resp = await _api.PageBasMaterialsAsync(1, 50, MaterialNameKeyword, null);
        MaterialList.Clear();
        foreach (var m in resp?.result?.records ?? new List<BasMaterialRecord>())
            MaterialList.Add(m);
    }

    [RelayCommand]
    public async Task SearchMaterialsAsync() => await LoadMaterialsAsync();

    [RelayCommand]
    private void OpenPicker() => IsPickerVisible = true;

    [RelayCommand]
    private void ClosePicker() => IsPickerVisible = false;

    public async Task ScanAndBindAsync(INavigation nav)
    {
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var code = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        var resp = await _api.PageBasMaterialsAsync(1, 1, null, code);
        var hit = resp?.result?.records?.FirstOrDefault();
        if (hit == null) return;

        SelectMaterial(hit);
    }

    public void SelectMaterial(BasMaterialRecord record)
    {
        SelectedMaterialName = string.IsNullOrWhiteSpace(record.materialName) ? "-" : record.materialName!;
        SelectedMaterialCode = record.materialCode;
        IsPickerVisible = false;
    }

    [RelayCommand]
    private void PickMaterial(BasMaterialRecord? record)
    {
        if (record is null) return;
        SelectMaterial(record);
    }

    [RelayCommand]
    private async Task OpenTargetFramePopupAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMaterialCode) || string.IsNullOrWhiteSpace(SelectedMaterialName))
            return;

        var resp = await _api.GetFrameStatusListAsync(SelectedMaterialCode!, SelectedMaterialName!);

        TargetFrameList.Clear();

        foreach (var x in resp?.result ?? new List<FrameStatusItem>())
        {
            TargetFrameList.Add(new TargetFrameSelectableItem
            {
                frameNo = x.frameNo,
                frameStatus = x.frameStatus,
                IsSelected = SelectedTargetFrames.Any(t => t.FrameNo == x.frameNo)
            });
        }

        IsTargetFramePopupVisible = true;
    }

    [RelayCommand]
    private void CloseTargetFramePopup() => IsTargetFramePopupVisible = false;

    [RelayCommand]
    private void ToggleTargetFrame(TargetFrameSelectableItem? item)
    {
        if (item is null) return;

        var exists = SelectedTargetFrames.FirstOrDefault(x => x.FrameNo == item.frameNo);

        if (exists is not null)
        {
            SelectedTargetFrames.Remove(exists);
            item.IsSelected = false;
        }
        else
        {
            SelectedTargetFrames.Add(new SelectedTargetFrameItem
            {
                FrameNo = string.IsNullOrWhiteSpace(item.frameNo) ? "-" : item.frameNo!,
                Qty = string.Empty
            });

            item.IsSelected = true;
        }

        ReindexSelectedFrames();
    }

    [RelayCommand]
    private void RemoveTargetFrame(SelectedTargetFrameItem? item)
    {
        if (item is null) return;
        SelectedTargetFrames.Remove(item);
        ReindexSelectedFrames();
    }

    private void ReindexSelectedFrames()
    {
        for (var i = 0; i < SelectedTargetFrames.Count; i++)
            SelectedTargetFrames[i].Index = i + 1;
        SelectedTargetFrameCount = SelectedTargetFrames.Count;
    }
}

public partial class SelectedTargetFrameItem : ObservableObject
{
    [ObservableProperty] private int index;
    [ObservableProperty] private string frameNo = "-";
    [ObservableProperty] private string qty = string.Empty;
}
public partial class TargetFrameSelectableItem : ObservableObject
{
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }

    [ObservableProperty]
    private bool isSelected;
}