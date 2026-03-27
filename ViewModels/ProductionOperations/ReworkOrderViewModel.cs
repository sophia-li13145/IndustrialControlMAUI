using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
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

    [ObservableProperty] private string? reworkReason;
    [ObservableProperty] private string? reworkNote;

    public ObservableCollection<StatusOption> ReworkTypeOptions { get; } = new();
    public ObservableCollection<StatusOption> ReworkProcessOptions { get; } = new();
    public ObservableCollection<StatusOption> YesNoOptions { get; } = new();
    public ObservableCollection<ReworkMaterialRow> SupplementRows { get; } = new();

    [ObservableProperty] private StatusOption? selectedReworkType;
    [ObservableProperty] private StatusOption? selectedReworkProcess;
    [ObservableProperty] private StatusOption? selectedNeedSupplement;

    public ReworkOrderViewModel(IWorkOrderApi api)
    {
        _api = api;

        YesNoOptions.Add(new StatusOption { Text = "是", Value = "1" });
        YesNoOptions.Add(new StatusOption { Text = "否", Value = "0" });
        SelectedNeedSupplement = YesNoOptions.FirstOrDefault();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
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
        WorkOrderNo = domain.workOrderNo;
        WorkOrderName = domain.workOrderName;
        MaterialName = domain.materialName;
        QuantityText = (domain.curQty ?? 0m).ToString("G29");

        var child = domain.planChildProductSchemeDetailList.FirstOrDefault();
        var routeDetails = child?.planProcessRoute?.routeDetailList
            ?.OrderBy(x => x.sortNumber ?? int.MaxValue)
            .ToList() ?? new List<RouteDetailEx>();

        foreach (var p in routeDetails)
        {
            if (string.IsNullOrWhiteSpace(p.processCode) && string.IsNullOrWhiteSpace(p.processName)) continue;
            ReworkProcessOptions.Add(new StatusOption
            {
                Value = p.processCode,
                Text = p.processName ?? p.processCode!
            });
        }

        SelectedReworkProcess = ReworkProcessOptions.FirstOrDefault();

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
                ActualQtyText = m.qty?.ToString("G29")
            });
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await Shell.Current.DisplayAlert("提示", "已保存（接口待联调）。", "确定");
    }

    [RelayCommand]
    private async Task SaveAndSubmitAsync()
    {
        await Shell.Current.DisplayAlert("提示", "已保存并提交（接口待联调）。", "确定");
    }
}
