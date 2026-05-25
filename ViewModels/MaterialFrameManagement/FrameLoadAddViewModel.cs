using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameLoadAddViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<BasMaterialRecord> MaterialList { get; } = new();
    public ObservableCollection<TargetFrameSelectableItem> TargetFrameList { get; } = new();
    public ObservableCollection<SelectedTargetFrameItem> SelectedTargetFrames { get; } = new();

    [ObservableProperty] private string? materialNameKeyword;
    [ObservableProperty] private string selectedMaterialName = "请选择";
    [ObservableProperty] private string? selectedMaterialCode;
    [ObservableProperty] private bool hasSelectedMaterial;
    [ObservableProperty] private bool isPickerVisible;
    [ObservableProperty] private BasMaterialRecord? pickedMaterial;
    [ObservableProperty] private bool isTargetFramePopupVisible;
    [ObservableProperty] private int selectedTargetFrameCount;
    [ObservableProperty] private bool canConfirmLoad;
    [ObservableProperty] private Color confirmButtonColor = Color.FromArgb("#D1D5DB");
    [ObservableProperty] private Color confirmButtonTextColor = Color.FromArgb("#9CA3AF");

    public bool ShowMaterialPickerActions => !HasSelectedMaterial;

    public FrameLoadAddViewModel(IMaterialFrameApi api)
    {
        _api = api;
        SelectedTargetFrames.CollectionChanged += OnSelectedTargetFramesChanged;
    }

    public async Task LoadMaterialsAsync()
    {
        await EnsureFrameStatusDictLoadedAsync();
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
        HasSelectedMaterial = !string.IsNullOrWhiteSpace(SelectedMaterialCode);
        IsPickerVisible = false;
    }

    partial void OnPickedMaterialChanged(BasMaterialRecord? value)
    {
        if (value is null) return;
        SelectMaterial(value);
        PickedMaterial = null;
    }

    [RelayCommand]
    private void PickMaterial(BasMaterialRecord? record)
    {
        if (record is null) return;
        SelectMaterial(record);
    }

    [RelayCommand]
    private void ClearSelectedMaterial()
    {
        SelectedMaterialName = "请选择";
        SelectedMaterialCode = null;
        HasSelectedMaterial = false;
        PickedMaterial = null;
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
                id = x.id,
                frameNo = x.frameNo,
                frameTypeCode = x.frameTypeCode,
                frameTypeName = x.frameTypeName,
                frameStatus = x.frameStatus,
                fullLoadStatus = x.fullLoadStatus,
                frameStatusDisplay = ResolveFrameStatusDisplay(x.frameStatus),
                IsSelected = SelectedTargetFrames.Any(t => t.FrameNo == x.frameNo)
            });
        }

        IsTargetFramePopupVisible = true;
    }

    [RelayCommand]
    private void CloseTargetFramePopup() => IsTargetFramePopupVisible = false;

    public async Task ScanAndAddTargetFrameAsync(INavigation nav)
    {
        if (string.IsNullOrWhiteSpace(SelectedMaterialCode)) return;

        var tcs = new TaskCompletionSource<string>();
        await nav.PushAsync(new QrScanPage(tcs));
        var frameNo = (await tcs.Task)?.Trim();
        if (string.IsNullOrWhiteSpace(frameNo)) return;

        await EnsureFrameStatusDictLoadedAsync();
        var resp = await _api.GetFrameStatusListByFrameNoAsync(frameNo, SelectedMaterialCode!);
        var frame = resp?.result?.FirstOrDefault();
        if (frame is null) return;

        var exists = TargetFrameList.FirstOrDefault(x =>
            string.Equals(x.frameNo, frame.frameNo, StringComparison.OrdinalIgnoreCase));
        if (exists is null)
        {
            exists = new TargetFrameSelectableItem
            {
                id = frame.id,
                frameNo = frame.frameNo,
                frameTypeCode = frame.frameTypeCode,
                frameTypeName = frame.frameTypeName,
                frameStatus = frame.frameStatus,
                fullLoadStatus = frame.fullLoadStatus,
                frameStatusDisplay = ResolveFrameStatusDisplay(frame.frameStatus)
            };
            TargetFrameList.Add(exists);
        }

        if (!exists.IsSelected)
        {
            exists.IsSelected = true;
            SelectedTargetFrames.Add(new SelectedTargetFrameItem
            {
                FrameNo = string.IsNullOrWhiteSpace(exists.frameNo) ? "-" : exists.frameNo!,
                Qty = string.Empty
            });
            ReindexSelectedFrames();
        }
    }

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
        RefreshConfirmState();
    }

    private void OnSelectedTargetFramesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (var item in e.NewItems.OfType<SelectedTargetFrameItem>())
                item.PropertyChanged += OnSelectedTargetFrameItemChanged;

        if (e.OldItems != null)
            foreach (var item in e.OldItems.OfType<SelectedTargetFrameItem>())
                item.PropertyChanged -= OnSelectedTargetFrameItemChanged;

        RefreshConfirmState();
    }

    private void OnSelectedTargetFrameItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedTargetFrameItem.Qty))
            RefreshConfirmState();
    }

    private void RefreshConfirmState()
    {
        var allQtyValid = SelectedTargetFrames.Count > 0 && SelectedTargetFrames.All(x => decimal.TryParse(x.Qty, out var qty) && qty > 0);
        CanConfirmLoad = !string.IsNullOrWhiteSpace(SelectedMaterialCode) && allQtyValid;
        ConfirmButtonColor = CanConfirmLoad ? Color.FromArgb("#2F66E8") : Color.FromArgb("#D1D5DB");
        ConfirmButtonTextColor = CanConfirmLoad ? Colors.White : Color.FromArgb("#9CA3AF");
    }

    partial void OnSelectedMaterialCodeChanged(string? value) => RefreshConfirmState();

    partial void OnHasSelectedMaterialChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowMaterialPickerActions));
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

    [RelayCommand]
    public async Task ConfirmLoadAsync()
    {
        if (!CanConfirmLoad) return;

        var frameStatusList = TargetFrameList
            .Where(x => x.IsSelected)
            .Select(x => new TargetFrameSelectableItem
            {
                id = x.id,
                frameNo = x.frameNo,
                frameStatus = x.frameStatus,
                frameTypeCode = x.frameTypeCode,
                frameTypeName = x.frameTypeName,
                fullLoadStatus = x.fullLoadStatus
            })
            .ToList();
        var detailList = SelectedTargetFrames
            .Select(x => new AddLoadingDetail
            {
                frameNo = x.FrameNo,
                materialCode = SelectedMaterialCode,
                materialName = SelectedMaterialName,
                qty = decimal.Parse(x.Qty)
            }).ToList();

        var req = new AddLoadingRecordReq
        {
            frameStatusList = frameStatusList,
            loadDetailList = detailList,
            material = new AddLoadingMaterial
            {
                materialCode = SelectedMaterialCode,
                materialName = SelectedMaterialName
            }
        };

        var resp = await _api.AddLoadingRecordAsync(req);
        if (resp?.success == true && resp.result == true)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(resp?.message) ? "装框失败，请稍后重试" : resp!.message!;
        if (Shell.Current?.CurrentPage is Page page)
            await page.DisplayAlert("提示", msg, "确定");
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
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public bool? fullLoadStatus { get; set; }

    [ObservableProperty]
    private bool isSelected;
}
