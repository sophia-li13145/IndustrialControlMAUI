using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class ReworkOrderViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkOrderApi _api;
    private readonly IQualityApi _qualityApi;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? orderId;
    [ObservableProperty] private string? workOrderNo;
    [ObservableProperty] private string? workOrderName;
    [ObservableProperty] private string? materialName;
    [ObservableProperty] private string quantityText = "0";
    [ObservableProperty] private string reworkQtyText = "";

    [ObservableProperty] private string? reworkReason;
    [ObservableProperty] private string? reworkNote;

    public ObservableCollection<StatusOption> ReworkTypeOptions { get; } = new();
    public ObservableCollection<StatusFilterOption> ReworkProcessOptions { get; } = new();
    public ObservableCollection<StatusOption> YesNoOptions { get; } = new();
    public ObservableCollection<ReworkMaterialRow> SupplementRows { get; } = new();
    public ObservableCollection<DefectChip> DefectTags { get; } = new();

    [ObservableProperty] private StatusOption? selectedReworkType;
    [ObservableProperty] private StatusOption? selectedNeedSupplement;
    [ObservableProperty] private string reworkProcessSummary = "全部";
    [ObservableProperty] private bool isSupplementSectionVisible = true;
    private bool _isReworkQtyInvalid;
    private bool _isReworkTypeInvalid;

    public bool IsReworkQtyInvalid
    {
        get => _isReworkQtyInvalid;
        set => SetProperty(ref _isReworkQtyInvalid, value);
    }

    public bool IsReworkTypeInvalid
    {
        get => _isReworkTypeInvalid;
        set => SetProperty(ref _isReworkTypeInvalid, value);
    }

    private ReworkOrderDomain? _domain;

    public ReworkOrderViewModel(IWorkOrderApi api, IQualityApi qualityApi)
    {
        _api = api;
        _qualityApi = qualityApi;
        YesNoOptions.Add(new StatusOption { Text = "否", Value = "0" });
        YesNoOptions.Add(new StatusOption { Text = "是", Value = "1" });
        SelectedNeedSupplement = YesNoOptions.FirstOrDefault();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workOrderNo", out var byNo) && byNo is string workOrderNo && !string.IsNullOrWhiteSpace(workOrderNo))
        {
            await InitAsync(workOrderNo);
            return;
        }

        // 向后兼容旧参数
        if (query.TryGetValue("id", out var v) && v is string id && !string.IsNullOrWhiteSpace(id))
        {
            await InitAsync(id);
        }
    }

    [RelayCommand]
    private async Task InitAsync(string id)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            OrderId = id;
            await LoadDictAsync();
            await LoadDomainAsync(id);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDictAsync()
    {
        ReworkTypeOptions.Clear();

        var resp = await _api.GetReworkDictListAsync();
        if (resp.success != true)
        {
            await Shell.Current.DisplayAlert("错误", resp.message ?? "返修类型字典加载失败", "确定");
            SelectedReworkType = null;
            return;
        }

        var fields = resp.result ?? new List<FieldDict>();

        // 返修类型仅取 field = reworkType 的字典项
        var reworkType = fields.FirstOrDefault(x => string.Equals(x.field, "reworkType", StringComparison.OrdinalIgnoreCase));
        foreach (var item in reworkType?.dictItems ?? new List<DictItem>())
        {
            if (string.IsNullOrWhiteSpace(item.dictItemValue)) continue;
            ReworkTypeOptions.Add(new StatusOption
            {
                Value = item.dictItemValue,
                Text = item.dictItemName ?? item.dictItemValue!
            });
        }

        SelectedReworkType = null;
    }

    private async Task LoadDomainAsync(string id)
    {
        ReworkProcessOptions.Clear();
        SupplementRows.Clear();
        DefectTags.Clear();

        var resp = await _api.GetReworkWorkOrderDomainAsync(id);
        if (resp?.success != true || resp.result == null)
        {
            await Shell.Current.DisplayAlert("错误", resp?.message ?? "返修工单详情加载失败", "确定");
            return;
        }

        var domain = resp.result;
        _domain = domain;
        _domain.materialCode = domain.planChildProductSchemeDetailList[0]?.materialCode;
        WorkOrderNo = domain.workOrderNo;
        WorkOrderName = domain.workOrderName;
        MaterialName = domain.materialName;
        QuantityText = (domain.curQty ?? 0m).ToString("G29");
        ReworkQtyText = string.Empty;
        IsReworkQtyInvalid = false;

        var child = domain.planChildProductSchemeDetailList.FirstOrDefault();
        var routeDetails = child?.planProcessRoute?.routeDetailList
            ?.Where(x => !string.IsNullOrWhiteSpace(x.processCode) || !string.IsNullOrWhiteSpace(x.processName))
            .OrderBy(x => x.sortNumber ?? int.MaxValue)
            .GroupBy(x => x.processCode ?? x.processName)
            .Select(g => g.First())
            .ToList() ?? new List<RouteDetailEx>();

        foreach (var p in routeDetails)
        {
            ReworkProcessOptions.Add(new StatusFilterOption
            {
                Value = p.processCode,
                Text = p.processName ?? p.processCode!,
                IsSelected = true
            });
        }

        UpdateReworkProcessSummaryInternal();

        var supplementResp = await _api.GetReworkBomFlattenDetailsAsync(domain.workOrderNo ?? "");
        var bomRows = supplementResp.success
            ? (supplementResp.result ?? new List<ReworkBomDetailFlattenItem>())
            : new List<ReworkBomDetailFlattenItem>();

        var index = 1;
        foreach (var m in bomRows)
        {
            SupplementRows.Add(new ReworkMaterialRow
            {
                Sequence = index++,
                id = m.id,
                materialCode = m.materialCode,
                materialName = m.materialName,
                NeedSupplement = true,
                standardQty = m.qty,
                unit = m.unit,
                ActualQtyText = m.qty?.ToString("G29")
            });
        }

        IsSupplementSectionVisible = SelectedNeedSupplement?.Value == "1";
        IsReworkTypeInvalid = false;
    }

    [RelayCommand]
    private async Task OpenReworkProcessSelectorAsync()
    {
        if (Shell.Current.CurrentPage is null) return;

        await Shell.Current.CurrentPage.ShowPopupAsync(new StatusMultiSelectPopup(ReworkProcessOptions));
        UpdateReworkProcessSummaryInternal();
    }

    private void UpdateReworkProcessSummaryInternal()
    {
        if (ReworkProcessOptions.Count == 0)
        {
            ReworkProcessSummary = "请选择";
            return;
        }

        var selectedCount = ReworkProcessOptions.Count(x => x.IsSelected);
        if (selectedCount == 0)
        {
            ReworkProcessSummary = "请选择";
        }
        else if (selectedCount == ReworkProcessOptions.Count)
        {
            ReworkProcessSummary = "全部";
        }
        else
        {
            ReworkProcessSummary = string.Join("、", ReworkProcessOptions.Where(x => x.IsSelected).Select(x => x.Text));
        }
    }

    [RelayCommand]
    private async Task SaveAndSubmitAsync()
    {
        await SubmitInternalAsync();
    }

    partial void OnSelectedNeedSupplementChanged(StatusOption? value)
    {
        IsSupplementSectionVisible = value?.Value == "1";
    }

    private async Task SubmitInternalAsync()
    {
        if (IsBusy) return;
        if (_domain == null)
        {
            await Shell.Current.DisplayAlert("提示", "数据未初始化完成。", "确定");
            return;
        }

        if (!ValidateRequiredFields())
        {
            await Shell.Current.DisplayAlert("提示", "请先填写返修数量和返修类型。", "确定");
            return;
        }

        if (!decimal.TryParse(ReworkQtyText, out var reworkQty) || reworkQty <= 0)
        {
            IsReworkQtyInvalid = true;
            await Shell.Current.DisplayAlert("提示", "请输入大于0的返修数量。", "确定");
            return;
        }

        IsBusy = true;
        try
        {
            var req = BuildSaveRequest(_domain, reworkQty);
            var resp = await _api.SaveAndSubmitReworkOrderAsync(req);

            if (resp.success && resp.result == true)
            {
                await Shell.Current.DisplayAlert("提示", "保存并提交成功！", "确定");
                return;
            }

            await Shell.Current.DisplayAlert("错误", resp.message ?? "操作失败", "确定");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private SaveReworkOrderReq BuildSaveRequest(ReworkOrderDomain domain, decimal reworkQty)
    {
        var needSupplement = SelectedNeedSupplement?.Value == "1";
        var selectedProcesses = ReworkProcessOptions
            .Where(x => x.IsSelected)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

        var routeDetails = domain.planChildProductSchemeDetailList
            .FirstOrDefault()?
            .planProcessRoute?
            .routeDetailList ?? new List<RouteDetailEx>();

        var routeCode = domain.planChildProductSchemeDetailList.FirstOrDefault()?.planProcessRoute?.routeCode;

        var processList = routeDetails
            .Where(x => !string.IsNullOrWhiteSpace(x.processCode) && selectedProcesses.Contains(x.processCode))
            .GroupBy(x => x.processCode)
            .Select(g => g.First())
            .Select(x => new ReworkProcessSaveItem
            {
                id = x.id,
                schemeNo = domain.schemeNo,
                routeCode = routeCode,
                processCode = x.processCode,
                processName = x.processName,
                sortNumber = x.sortNumber
            })
            .ToList();

        var supplementList = needSupplement
            ? SupplementRows
                .Where(x => x.NeedSupplement)
                .Select(x => new ReworkSupplementSaveItem
                {
                    actualReplenishmentQty = decimal.TryParse(x.ActualQtyText, out var actual) ? actual : 0m,
                    materialCode = x.materialCode,
                    materialName = x.materialName,
                    standardReplenishmentQty = x.standardQty,
                    unit = x.unit
                })
                .ToList()
            : new List<ReworkSupplementSaveItem>();

        return new SaveReworkOrderReq
        {
            id = null,
            defectTags = string.Join(",", DefectTags.Select(x => x.Name)),
            hasReworkProcess = processList.Count > 0,
            isFeedSupplement = needSupplement,
            materialCode = domain.materialCode,
            materialName = domain.materialName,
            memo = ReworkNote,
            planNo = domain.schemeNo,
            productionOrderNo = domain.platPlanNo,
            reworkProcessList = processList,
            reworkQty = reworkQty,
            reworkType = SelectedReworkType?.Value,
            reworkTypeName = SelectedReworkType?.Text,
            submit = true,
            supplementList = supplementList,
            workOrderName = domain.workOrderName,
            workOrderNo = domain.workOrderNo
        };
    }

    [RelayCommand]
    private async Task OpenDefectPickerAsync()
    {
        var preselectedCodes = DefectTags
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var picked = await DefectPickerPopup.ShowAsync(_qualityApi, preselectedCodes);
        if (picked == null) return;

        DefectTags.Clear();
        foreach (var d in picked)
        {
            var name = d.DefectName ?? d.DefectCode;
            if (string.IsNullOrWhiteSpace(name)) continue;
            DefectTags.Add(new DefectChip
            {
                Name = name,
                ColorHex = Color.FromArgb("#EAF3FF")
            });
        }
    }

    [RelayCommand]
    private void RemoveDefectTag(DefectChip? item)
    {
        if (item == null) return;
        DefectTags.Remove(item);
    }

    private bool ValidateRequiredFields()
    {
        IsReworkQtyInvalid = string.IsNullOrWhiteSpace(ReworkQtyText);
        IsReworkTypeInvalid = SelectedReworkType == null || string.IsNullOrWhiteSpace(SelectedReworkType.Value);
        return !IsReworkQtyInvalid && !IsReworkTypeInvalid;
    }

    partial void OnReworkQtyTextChanged(string value)
    {
        IsReworkQtyInvalid = string.IsNullOrWhiteSpace(value);
    }

    partial void OnSelectedReworkTypeChanged(StatusOption? value)
    {
        IsReworkTypeInvalid = value == null || string.IsNullOrWhiteSpace(value.Value);
    }
}
