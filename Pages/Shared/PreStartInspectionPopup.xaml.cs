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
    private readonly Dictionary<string, string> _maintenanceStatusMap = new(StringComparer.OrdinalIgnoreCase);
    private Task? _maintenanceStatusLoadTask;
    private bool _isToolSectionExpanded = true;
    private bool _isMaterialSectionExpanded = true;

    public PreStartInspectionPopup(IWorkOrderApi api, WorkProcessTaskDetail detail)
    {
        InitializeComponent();
        _api = api;
        _detail = detail;
        ToolList.ItemsSource = _toolRows;
        MaterialList.ItemsSource = _materialRows;
        _maintenanceStatusLoadTask = LoadMaintenanceStatusDictAsync();
    }

    private async Task LoadMaintenanceStatusDictAsync()
    {
        try
        {
            var resp = await _api.GetWorkProcessTaskDictListAsync();
            if (!resp.success)
            {
                await Shell.Current.DisplayAlert("提示", resp.message ?? "工具状态字典获取失败", "确定");
                return;
            }

            var upkeepStatus = resp.result?.FirstOrDefault(field =>
                string.Equals(field.field, "upkeepStatus", StringComparison.OrdinalIgnoreCase));

            _maintenanceStatusMap.Clear();
            if (upkeepStatus?.dictItems != null)
            {
                foreach (var item in upkeepStatus.dictItems)
                {
                    var value = item.dictItemValue?.Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    _maintenanceStatusMap[value] = item.dictItemName ?? value;
                }
            }

            foreach (var row in _toolRows)
            {
                ApplyMaintenanceStatusText(row);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("提示", $"工具状态字典获取失败：{ex.Message}", "确定");
        }
    }

    private async Task EnsureMaintenanceStatusDictLoadedAsync()
    {
        if (_maintenanceStatusLoadTask == null)
        {
            _maintenanceStatusLoadTask = LoadMaintenanceStatusDictAsync();
        }

        await _maintenanceStatusLoadTask;
    }

    private void ApplyMaintenanceStatusText(PreStartInspectionScanResourceDto row)
    {
        var value = row.maintenanceStatus?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            row.MaintenanceStatusText = "未保养";
            return;
        }

        row.MaintenanceStatusText = _maintenanceStatusMap.TryGetValue(value, out var name)
            ? name
            : value;
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

        await EnsureMaintenanceStatusDictLoadedAsync();
        ApplyMaintenanceStatusText(resp.result);
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

    private void OnToggleToolSectionTapped(object? sender, TappedEventArgs e)
    {
        _isToolSectionExpanded = !_isToolSectionExpanded;
        SetToolSectionExpanded(_isToolSectionExpanded);
    }

    private void OnToggleMaterialSectionTapped(object? sender, TappedEventArgs e)
    {
        _isMaterialSectionExpanded = !_isMaterialSectionExpanded;
        SetMaterialSectionExpanded(_isMaterialSectionExpanded);
    }

    private void SetToolSectionExpanded(bool isExpanded)
    {
        SetSectionRows(ToolSectionGrid, isExpanded);
        ToolScanRow.IsVisible = isExpanded;
        ToolTableHeader.IsVisible = isExpanded;
        ToolList.IsVisible = isExpanded;
        ToolFooterToggleRow.IsVisible = isExpanded;
        ToolHeaderToggleLabel.Text = isExpanded ? "收起 向上" : "展开 向下";
    }

    private void SetMaterialSectionExpanded(bool isExpanded)
    {
        SetSectionRows(MaterialSectionGrid, isExpanded);
        MaterialScanRow.IsVisible = isExpanded;
        MaterialTableHeader.IsVisible = isExpanded;
        MaterialList.IsVisible = isExpanded;
        MaterialFooterToggleRow.IsVisible = isExpanded;
        MaterialHeaderToggleLabel.Text = isExpanded ? "收起 向上" : "展开 向下";
    }

    private static void SetSectionRows(Grid sectionGrid, bool isExpanded)
    {
        sectionGrid.RowDefinitions.Clear();
        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });

        if (isExpanded)
        {
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            return;
        }

        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        sectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
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

    private void OnDeleteToolClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: PreStartInspectionScanResourceDto row })
        {
            _toolRows.Remove(row);
        }
    }

    private void OnDeleteMaterialClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: PreStartInspectionScanMaterialDto row })
        {
            _materialRows.Remove(row);
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
            isConfirmed = row.isConfirmed,
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
            isConfirmed = row.isConfirmed,
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
