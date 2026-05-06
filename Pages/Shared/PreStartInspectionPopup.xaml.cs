using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Popups;

public partial class PreStartInspectionPopup : Popup
{
    private readonly IWorkOrderApi _api;
    private readonly WorkProcessTaskDetail _detail;
    private readonly ObservableCollection<PreStartInspectionScanResourceDto> _toolRows = new();
    private readonly ObservableCollection<PreStartInspectionScanMaterialDto> _materialRows = new();

    public PreStartInspectionPopup(IWorkOrderApi api, WorkProcessTaskDetail detail)
    {
        InitializeComponent();
        _api = api;
        _detail = detail;
        ToolList.ItemsSource = _toolRows;
        MaterialList.ItemsSource = _materialRows;
       
    }

    private async void OnToolScanClicked(object? sender, EventArgs e)
    {
        var code = ToolScanEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        var resp = await _api.QueryPreStartInspectionResourceAsync(new PmsPreStartInspectionQueryResourceParam
        {
            workOrderNo = _detail.workOrderNo,
            processCode = _detail.processCode,
            resourceCode = code
        });
        if (!resp.success || resp.result == null)
        {
            await Shell.Current.DisplayAlert("提示", resp.message ?? "扫描失败", "确定");
            return;
        }

        _toolRows.Add(resp.result);
        ToolScanEntry.Text = string.Empty;
    }

    private async void OnMaterialScanClicked(object? sender, EventArgs e)
    {
        var code = MaterialScanEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        var resp = await _api.QueryPreStartInspectionMaterialAsync(new PmsPreStartInspectionQueryMaterialParam
        {
            workOrderNo = _detail.workOrderNo,
            processCode = _detail.processCode,
            materialCode = code
        });
        if (!resp.success || resp.result == null)
        {
            await Shell.Current.DisplayAlert("提示", resp.message ?? "扫描失败", "确定");
            return;
        }

        _materialRows.Add(resp.result);
        MaterialScanEntry.Text = string.Empty;
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        ConfirmButton.IsEnabled = false;
        try
        {
            var resp = await _api.ConfirmPreStartInspectionScansAsync(BuildConfirmRequest());
            if (resp.success && resp.result)
            {
                Close(true);
                return;
            }

            await Shell.Current.DisplayAlert("提示", resp.message ?? "点检确认提交失败", "确定");
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }

    private PmsPreStartInspectionConfirmScansParam BuildConfirmRequest()
    {
        return new PmsPreStartInspectionConfirmScansParam
        {
            workOrderNo = _detail.workOrderNo,
            platPlanNo = _detail.platPlanNo,
            processCode = _detail.processCode,
            processName = _detail.processName,
            schemeNo = _detail.schemeNo,
            materialList = _materialRows.Select(ToConfirmMaterialItem).ToList(),
            toolingList = _toolRows.Select(ToConfirmToolingItem).ToList()
        };
    }

    private PreStartInspectionConfirmMaterialItem ToConfirmMaterialItem(PreStartInspectionScanMaterialDto row)
    {
        return new PreStartInspectionConfirmMaterialItem
        {
            isConfirmed = true,
            matReqNo = row.matReqNo,
            materialCode = row.materialCode,
            materialName = row.materialName,
            platPlanNo = row.platPlanNo ?? _detail.platPlanNo,
            processCode = _detail.processCode,
            processName = _detail.processName,
            scanQty = row.demandQty ?? 0,
            schemeNo = row.schemeNo ?? _detail.schemeNo,
            unit = row.unit,
            workOrderNo = row.workOrderNo ?? _detail.workOrderNo
        };
    }

    private PreStartInspectionConfirmToolingItem ToConfirmToolingItem(PreStartInspectionScanResourceDto row)
    {
        return new PreStartInspectionConfirmToolingItem
        {
            isConfirmed = true,
            maintenanceStatus = row.maintenanceStatus,
            model = row.model,
            platPlanNo = _detail.platPlanNo,
            processCode = row.processCode ?? _detail.processCode,
            processName = row.processName ?? _detail.processName,
            resourceDemandId = row.resourceDemandId,
            resourceType = row.resourceType,
            schemeNo = _detail.schemeNo,
            toolingCode = row.resourceCode,
            toolingName = row.resourceName,
            workOrderNo = _detail.workOrderNo
        };
    }

    private string GetScanResourceType()
    {
        return _toolRows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.resourceType))?.resourceType ?? "tooling";
    }
}
