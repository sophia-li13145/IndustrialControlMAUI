using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameLoadAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public ObservableCollection<BasMaterialRecord> MaterialList { get; } = new();

    [ObservableProperty] private string? materialNameKeyword;
    [ObservableProperty] private string selectedMaterialName = "请选择";
    [ObservableProperty] private string? selectedMaterialCode;

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
    }
}
