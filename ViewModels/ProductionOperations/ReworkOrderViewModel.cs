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

    [ObservableProperty] private StatusOption? selectedReworkType;
    [ObservableProperty] private StatusOption? selectedNeedSupplement;
    [ObservableProperty] private string reworkProcessSummary = "全部";
    [ObservableProperty] private bool isSupplementSectionVisible = true;

    private ReworkOrderDomain? _domain;

    public ReworkOrderViewModel(IWorkOrderApi api)
    {
        _api = api;

        YesNoOptions.Add(new StatusOption { Text = "是", Value = "1" });
        YesNoOptions.Add(new StatusOption { Text = "否", Value = "0" });
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
        var fields = resp.result ?? new List<FieldDict>();

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

        SelectedReworkType = ReworkTypeOptions.FirstOrDefault();
    }

    private async Task LoadDomainAsync(string id)
    {
        ReworkProcessOptions.Clear();
        SupplementRows.Clear();

        var resp = await _api.GetReworkWorkOrderDomainAsync(id);
        if (resp?.success != true || resp.result == null)
        {
            await Shell.Current.DisplayAlert("错误", resp?.message ?? "返修工单详情加载失败", "确定");
            return;
        }

        var domain = resp.result;
        _domain = domain;
        WorkOrderNo = domain.workOrderNo;
        WorkOrderName = domain.workOrderName;
        MaterialName = domain.materialName;
        QuantityText = (domain.curQty ?? 0m).ToString("G29");
        ReworkQtyText = QuantityText;

        foreach (var p in domain.planProcessRouteResourceDemandList
                     .Where(x => !string.IsNullOrWhiteSpace(x.processCode) || !string.IsNullOrWhiteSpace(x.processName))
                     .GroupBy(x => x.processCode ?? x.processName)
                     .Select(g => g.First()))
        {
            ReworkProcessOptions.Add(new StatusFilterOption
            {
                Value = p.processCode,
                Text = p.processName ?? p.processCode!,
                IsSelected = true
            });
        }

        RefreshReworkProcessSummary();

        var child = domain.planChildProductSchemeDetailList.FirstOrDefault();
        var index = 1;
        foreach (var m in child?.planBom?.bomDetailList ?? new List<PlanBomDetailEx>())
        {
            SupplementRows.Add(new ReworkMaterialRow
            {
                Sequence = index++,
                id = m.id,
                materialCode = m.materialCode,
                materialName = m.materialName,
                NeedSupplement = m.needCollect ?? false,
                standardQty = m.qty,
                unit = m.unit,
                ActualQtyText = m.qty?.ToString("G29")
            });
        }

        IsSupplementSectionVisible = SelectedNeedSupplement?.Value == "1";
    }

    [RelayCommand]
    private async Task ChooseReworkProcessAsync()
    {
        if (Shell.Current.CurrentPage is null) return;

        await Shell.Current.CurrentPage.ShowPopupAsync(new StatusMultiSelectPopup(ReworkProcessOptions));
        RefreshReworkProcessSummary();
    }

    private void RefreshReworkProcessSummary()
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
    private async Task SaveAsync()
    {
        await SubmitInternalAsync(false);
    }

    [RelayCommand]
    private async Task SaveAndSubmitAsync()
    {
        await SubmitInternalAsync(true);
    }

    partial void OnSelectedNeedSupplementChanged(StatusOption? value)
    {
        IsSupplementSectionVisible = value?.Value == "1";
    }

    private async Task SubmitInternalAsync(bool submit)
    {
        if (IsBusy) return;
        if (_domain == null)
        {
            await Shell.Current.DisplayAlert("提示", "数据未初始化完成。", "确定");
            return;
        }

        if (!decimal.TryParse(ReworkQtyText, out var reworkQty))
        {
            await Shell.Current.DisplayAlert("提示", "请输入正确的返修数量。", "确定");
            return;
        }

        IsBusy = true;
        try
        {
            var req = BuildSaveRequest(_domain, reworkQty, submit);
            var resp = submit
                ? await _api.SaveAndSubmitReworkOrderAsync(req)
                : await _api.SaveReworkOrderAsync(req);

            if (resp.success && resp.result == true)
            {
                await Shell.Current.DisplayAlert("提示", submit ? "保存并提交成功！" : "保存成功！", "确定");
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

    private SaveReworkOrderReq BuildSaveRequest(ReworkOrderDomain domain, decimal reworkQty, bool submit)
    {
        var needSupplement = SelectedNeedSupplement?.Value == "1";
        var selectedProcesses = ReworkProcessOptions
            .Where(x => x.IsSelected)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

        var processList = domain.planProcessRouteResourceDemandList
            .Where(x => !string.IsNullOrWhiteSpace(x.processCode) && selectedProcesses.Contains(x.processCode))
            .GroupBy(x => x.processCode)
            .Select(g => g.First())
            .Select(x => new ReworkProcessSaveItem
            {
                id = x.id,
                schemeNo = x.schemeNo ?? domain.schemeNo,
                routeCode = x.routeCode,
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
            id = domain.id,
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
            submit = submit,
            supplementList = supplementList,
            workOrderName = domain.workOrderName,
            workOrderNo = domain.workOrderNo
        };
    }
}
