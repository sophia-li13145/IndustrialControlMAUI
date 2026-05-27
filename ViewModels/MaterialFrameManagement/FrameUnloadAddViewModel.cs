using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameUnloadAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<FrameStatusItem> SourceFrameList { get; } = new();
    public ObservableCollection<FrameUnloadMaterialChipVm> SelectedSourceMaterials { get; } = new();
    public ObservableCollection<FrameStatusItem> TargetFrameList { get; } = new();
    public ObservableCollection<SelectedUnloadTargetFrameVm> SelectedTargetFrames { get; } = new();

    [ObservableProperty] private bool isSourcePickerVisible;
    [ObservableProperty] private bool isTargetPickerVisible;
    [ObservableProperty] private bool hasSelectedSourceFrame;
    [ObservableProperty] private FrameStatusItem? pickedSourceFrame;
    [ObservableProperty] private string selectedSourceFrameNo = "请选择";
    [ObservableProperty] private string selectedSourceFrameId = string.Empty;
    [ObservableProperty] private string selectedSourceFrameTypeCode = string.Empty;
    [ObservableProperty] private string selectedSourceFrameTypeName = string.Empty;
    [ObservableProperty] private bool canConfirmUnload;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#D1D5DB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");

    public bool ShowSourcePickerActions => !HasSelectedSourceFrame;

    public FrameUnloadAddViewModel(IMaterialFrameApi api)
    {
        _api = api;
        SelectedTargetFrames.CollectionChanged += OnSelectedTargetFramesChanged;
    }

    [RelayCommand]
    private async Task OpenSourcePickerAsync()
    {
        await EnsureFrameStatusDictLoadedAsync();
        var resp = await _api.GetMaterialFrameListAsync();
        SourceFrameList.Clear();
        foreach (var frame in resp?.result ?? new List<FrameStatusItem>())
        {
            frame.IsSelected = string.Equals(frame.frameNo, SelectedSourceFrameNo, StringComparison.OrdinalIgnoreCase);
            frame.frameStatusDisplay = ResolveFrameStatusDisplay(frame.frameStatus);
            SourceFrameList.Add(frame);
        }
        IsSourcePickerVisible = true;
    }

    [RelayCommand] private void CloseSourcePicker() => IsSourcePickerVisible = false;



    [RelayCommand]
    private void SelectSourceFrame(FrameStatusItem? item)
    {
        if (item is null) return;
        PickedSourceFrame = item;
        foreach (var x in SourceFrameList) x.IsSelected = false;
        item.IsSelected = true;

        ApplySourceFrame(item);
        IsSourcePickerVisible = false;
    }

    [RelayCommand]
    private void ConfirmPickSourceFrame()
    {
        var picked = PickedSourceFrame ?? SourceFrameList.FirstOrDefault(x => x.IsSelected);
        if (picked is null) return;
        ApplySourceFrame(picked);
        IsSourcePickerVisible = false;
    }

    public async Task ScanAndPickSourceFrameAsync(INavigation nav)
    {
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;
        await EnsureFrameStatusDictLoadedAsync();
        var resp = await _api.GetMaterialFrameListAsync(frameNo);
        var picked = resp?.result?.FirstOrDefault();
        if (picked is null) return;
        ApplySourceFrame(picked);
    }

    private void ApplySourceFrame(FrameStatusItem picked)
    {
        SelectedSourceFrameNo = string.IsNullOrWhiteSpace(picked.frameNo) ? "-" : picked.frameNo!;
        SelectedSourceFrameId = picked.id ?? string.Empty;
        SelectedSourceFrameTypeCode = picked.frameTypeCode ?? string.Empty;
        SelectedSourceFrameTypeName = picked.frameTypeName ?? string.Empty;
        HasSelectedSourceFrame = true;
        SelectedSourceMaterials.Clear();
        var showBatchNo = string.Equals(picked.frameStatus, "warehouse", StringComparison.OrdinalIgnoreCase);
        foreach (var x in picked.loadDetailList ?? new List<MaterialFrameLoadDetail>())
        {
            var batchText = showBatchNo && !string.IsNullOrWhiteSpace(x.batchNo) ? $"批号: {x.batchNo}" : string.Empty;
            SelectedSourceMaterials.Add(new FrameUnloadMaterialChipVm
            {
                MaterialCode = x.materialCode ?? string.Empty,
                MaterialName = x.materialName ?? "-",
                MaterialDisplay = x.materialName ?? "-",
                BatchNo = x.batchNo,
                BatchDisplay = batchText,
                QtyDisplay = $"可用数: {(x.currentQuantity ?? x.currentQty ?? x.quantity ?? 0):0.##}",
                SourceQty = (x.currentQuantity ?? x.currentQty ?? x.quantity ?? 0)
            });
        }

        SelectedTargetFrames.Clear();
        TargetFrameList.Clear();
        RefreshConfirmState();
    }

    [RelayCommand]
    private void ClearSelectedSourceFrame()
    {
        HasSelectedSourceFrame = false;
        SelectedSourceFrameNo = "请选择";
        SelectedSourceFrameId = string.Empty;
        SelectedSourceFrameTypeCode = string.Empty;
        SelectedSourceFrameTypeName = string.Empty;
        PickedSourceFrame = null;
        SelectedSourceMaterials.Clear();
        SelectedTargetFrames.Clear();
        TargetFrameList.Clear();
        RefreshConfirmState();
    }

    [RelayCommand]
    private async Task OpenTargetPickerAsync()
    {
        if (!HasSelectedSourceFrame || SelectedSourceMaterials.Count == 0) return;
        var materialCodes = SelectedSourceMaterials.Select(x => x.MaterialCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var materialNames = SelectedSourceMaterials.Select(x => x.MaterialName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        await EnsureFrameStatusDictLoadedAsync();
        var resp = await _api.GetFrameStatusListForUnloadAsync(materialCodes, materialNames, null);
        TargetFrameList.Clear();
        foreach (var item in resp?.result ?? new List<FrameStatusItem>())
        {
            item.IsSelected = SelectedTargetFrames.Any(x => x.TargetFrameNo == item.frameNo);
            item.frameStatusDisplay = ResolveFrameStatusDisplay(item.frameStatus);
            TargetFrameList.Add(item);
        }
        IsTargetPickerVisible = true;
    }

    [RelayCommand] private void CloseTargetPicker() => IsTargetPickerVisible = false;

    public async Task ScanAndAddTargetFrameAsync(INavigation nav)
    {
        if (!HasSelectedSourceFrame || SelectedSourceMaterials.Count == 0) return;
        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;

        var materialCodes = SelectedSourceMaterials.Select(x => x.MaterialCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var materialNames = SelectedSourceMaterials.Select(x => x.MaterialName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var resp = await _api.GetFrameStatusListForUnloadAsync(materialCodes, materialNames, frameNo);
        var item = resp?.result?.FirstOrDefault();
        if (item is null) return;

        var exists = SelectedTargetFrames.FirstOrDefault(x => string.Equals(x.TargetFrameId, item.id, StringComparison.OrdinalIgnoreCase));
        if (exists is not null) return;

        SelectedTargetFrames.Add(new SelectedUnloadTargetFrameVm
        {
            Index = SelectedTargetFrames.Count + 1,
            TargetFrameId = item.id ?? string.Empty,
            TargetFrameNo = item.frameNo ?? "-",
            TargetFrameTypeCode = item.frameTypeCode ?? string.Empty,
            TargetFrameTypeName = item.frameTypeName ?? string.Empty,
            MaterialName = SelectedSourceMaterials.FirstOrDefault()?.MaterialName ?? "-",
            BatchNo = SelectedSourceMaterials.FirstOrDefault()?.BatchNo ?? string.Empty,
            UnloadQty = string.Empty
        });

        var listed = TargetFrameList.FirstOrDefault(x => string.Equals(x.id, item.id, StringComparison.OrdinalIgnoreCase));
        if (listed is not null) listed.IsSelected = true;
        RefreshConfirmState();
    }

    [RelayCommand]
    private void ToggleTargetFrame(FrameStatusItem? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void ConfirmPickTargetFrames()
    {
        var selected = TargetFrameList.Where(x => x.IsSelected).ToList();
        SelectedTargetFrames.Clear();
        var sourceMaterial = SelectedSourceMaterials.FirstOrDefault();
        for (var i = 0; i < selected.Count; i++)
        {
            var t = selected[i];
            SelectedTargetFrames.Add(new SelectedUnloadTargetFrameVm
            {
                Index = i + 1,
                TargetFrameId = t.id ?? string.Empty,
                TargetFrameNo = t.frameNo ?? "-",
                TargetFrameTypeCode = t.frameTypeCode ?? string.Empty,
                TargetFrameTypeName = t.frameTypeName ?? string.Empty,
                MaterialName = sourceMaterial?.MaterialName ?? "-",
                BatchNo = SelectedSourceMaterials.FirstOrDefault()?.BatchNo ?? string.Empty,
                UnloadQty = string.Empty
            });
        }
        IsTargetPickerVisible = false;
        RefreshConfirmState();
    }

    [RelayCommand]
    private void RemoveTargetFrame(SelectedUnloadTargetFrameVm? item)
    {
        if (item is null) return;
        var selected = TargetFrameList.FirstOrDefault(x => string.Equals(x.frameNo, item.TargetFrameNo, StringComparison.OrdinalIgnoreCase));
        if (selected is not null) selected.IsSelected = false;
        SelectedTargetFrames.Remove(item);
        for (var i = 0; i < SelectedTargetFrames.Count; i++) SelectedTargetFrames[i].Index = i + 1;
        RefreshConfirmState();
    }

    [RelayCommand]
    public async Task ConfirmUnloadAsync()
    {
        if (!CanConfirmUnload || SelectedSourceMaterials.Count == 0) return;

        var validUnloadDetails = SelectedTargetFrames
            .Where(x => decimal.TryParse(x.UnloadQty, out var qty) && qty > 0)
            .Select(x => new AddUnloadingDetail
            {
                targetFrameId = x.TargetFrameId,
                targetFrameNo = x.TargetFrameNo,
                targetFrameTypeCode = x.TargetFrameTypeCode,
                targetFrameTypeName = x.TargetFrameTypeName,
                unloadQty = decimal.TryParse(x.UnloadQty, out var qty) ? qty : 0
            }).ToList();

        var req = new AddUnloadingRecordReq
        {
            sourceFrameId = SelectedSourceFrameId,
            sourceFrameNo = SelectedSourceFrameNo,
            sourceFrameTypeCode = SelectedSourceFrameTypeCode,
            sourceFrameTypeName = SelectedSourceFrameTypeName,
            unloadMaterials = SelectedSourceMaterials
                .Select(material => new AddUnloadingMaterial
                {
                    materialCode = material.MaterialCode,
                    materialName = material.MaterialName,
                    sourceQty = material.SourceQty,
                    unloadDetailList = validUnloadDetails
                        .Select(d => new AddUnloadingDetail
                        {
                            targetFrameId = d.targetFrameId,
                            targetFrameNo = d.targetFrameNo,
                            targetFrameTypeCode = d.targetFrameTypeCode,
                            targetFrameTypeName = d.targetFrameTypeName,
                            unloadQty = d.unloadQty
                        }).ToList()
                }).ToList()
        };

        var resp = await _api.AddUnloadingRecordAsync(req);
        if (resp?.success == true && resp.result == true)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(resp?.message) ? "拆框失败，请稍后重试" : resp!.message!;
        if (Shell.Current?.CurrentPage is Page page)
            await page.DisplayAlert("提示", msg, "确定");
    }


    private void OnSelectedTargetFramesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (var item in e.NewItems.OfType<SelectedUnloadTargetFrameVm>())
                item.PropertyChanged += OnSelectedTargetFrameItemChanged;

        if (e.OldItems != null)
            foreach (var item in e.OldItems.OfType<SelectedUnloadTargetFrameVm>())
                item.PropertyChanged -= OnSelectedTargetFrameItemChanged;

        RefreshConfirmState();
    }

    private void OnSelectedTargetFrameItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedUnloadTargetFrameVm.UnloadQty))
            RefreshConfirmState();
    }

    partial void OnHasSelectedSourceFrameChanged(bool value) => OnPropertyChanged(nameof(ShowSourcePickerActions));

    partial void OnPickedSourceFrameChanged(FrameStatusItem? value)
    {
        if (value is null) return;
        foreach (var x in SourceFrameList) x.IsSelected = false;
        value.IsSelected = true;
    }

    partial void OnCanConfirmUnloadChanged(bool value)
    {
        ConfirmButtonColor = value ? Color.FromArgb("#2F66E8") : Color.FromArgb("#D1D5DB");
        ConfirmButtonTextColor = value ? Colors.White : Color.FromArgb("#9CA3AF");
    }

    private void RefreshConfirmState()
    {
        var allQtyValid = SelectedTargetFrames.Count > 0 && SelectedTargetFrames.All(x => decimal.TryParse(x.UnloadQty, out var qty) && qty > 0);
        CanConfirmUnload = HasSelectedSourceFrame && allQtyValid;
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

public class FrameUnloadMaterialChipVm
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = "-";
    public string MaterialDisplay { get; set; } = "-";
    public string? BatchNo { get; set; }
    public string BatchDisplay { get; set; } = string.Empty;
    public string QtyDisplay { get; set; } = "-";
    public decimal SourceQty { get; set; }
}

public partial class SelectedUnloadTargetFrameVm : ObservableObject
{
    [ObservableProperty] private int index;
    [ObservableProperty] private string targetFrameId = string.Empty;
    [ObservableProperty] private string targetFrameNo = "-";
    [ObservableProperty] private string targetFrameTypeCode = string.Empty;
    [ObservableProperty] private string targetFrameTypeName = string.Empty;
    [ObservableProperty] private string materialName = "-";
    [ObservableProperty] private string batchNo = "-";
    [ObservableProperty] private string unloadQty = string.Empty;
}
