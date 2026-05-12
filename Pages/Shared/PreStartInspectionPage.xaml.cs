using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class PreStartInspectionPage : ContentPage
{
    private readonly IWorkOrderApi _api;
    private readonly WorkProcessTaskDetail _detail;
    private readonly ObservableCollection<PreStartInspectionScanResourceDto> _toolRows = new();
    private readonly ObservableCollection<PreStartInspectionScanMaterialDto> _materialRows = new();
    private readonly Dictionary<string, string> _maintenanceStatusMap = new(StringComparer.OrdinalIgnoreCase);
    private Task? _maintenanceStatusLoadTask;
    private bool _isToolSectionExpanded = true;
    private bool _isMaterialSectionExpanded = true;
    private readonly HashSet<string> _pendingToolScanCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pendingMaterialScanCodes = new(StringComparer.OrdinalIgnoreCase);

    private readonly TaskCompletionSource<bool> _completionSource;

    public PreStartInspectionPage(IWorkOrderApi api, WorkProcessTaskDetail detail, TaskCompletionSource<bool> completionSource)
    {
        InitializeComponent();
        _api = api;
        _detail = detail;
        _completionSource = completionSource;
        ToolList.ItemsSource = _toolRows;
        MaterialList.ItemsSource = _materialRows;
        _maintenanceStatusLoadTask = LoadMaintenanceStatusDictAsync();
    }

    public static async Task<bool> ShowAsync(IWorkOrderApi api, WorkProcessTaskDetail detail)
    {
        var navigation = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (navigation == null)
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("提示", "无法打开开工前点检页面", "确定");
            }

            return false;
        }

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await navigation.PushAsync(new PreStartInspectionPage(api, detail, completionSource));
        return await completionSource.Task;
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
        await SubmitToolScanAsync(ToolScanEntry.Text);
    }

    private async void OnToolScanButtonClicked(object? sender, EventArgs e)
    {
        var code = await ScanCodeAsync();
        if (string.IsNullOrWhiteSpace(code)) return;

        ToolScanEntry.Text = code;
        await SubmitToolScanAsync(code);
    }

    private async Task SubmitToolScanAsync(string? scanText)
    {
        var code = scanText?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        if (!_pendingToolScanCodes.Add(code)) return;

        try
        {
            if (HasDuplicateTool(code))
            {
                await Shell.Current.DisplayAlert("提示", "该工具工装已在列表中，请勿重复添加", "确定");
                ToolScanEntry.Text = string.Empty;
                return;
            }

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

            if (HasDuplicateTool(resp.result, code))
            {
                await Shell.Current.DisplayAlert("提示", "该工具工装已在列表中，请勿重复添加", "确定");
                ToolScanEntry.Text = string.Empty;
                return;
            }

            await EnsureMaintenanceStatusDictLoadedAsync();
            ApplyMaintenanceStatusText(resp.result);
            _toolRows.Add(resp.result);
            ToolScanEntry.Text = string.Empty;
        }
        finally
        {
            _pendingToolScanCodes.Remove(code);
        }
    }

    private async void OnMaterialScanClicked(object? sender, EventArgs e)
    {
        await SubmitMaterialScanAsync(MaterialScanEntry.Text);
    }

    private async void OnMaterialScanButtonClicked(object? sender, EventArgs e)
    {
        var code = await ScanCodeAsync();
        if (string.IsNullOrWhiteSpace(code)) return;

        MaterialScanEntry.Text = code;
        await SubmitMaterialScanAsync(code);
    }

    private async Task SubmitMaterialScanAsync(string? scanText)
    {
        var code = scanText?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        if (!_pendingMaterialScanCodes.Add(code)) return;

        try
        {
            if (HasDuplicateMaterial(code))
            {
                await Shell.Current.DisplayAlert("提示", "该物料已在列表中，请勿重复添加", "确定");
                MaterialScanEntry.Text = string.Empty;
                return;
            }

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

            if (HasDuplicateMaterial(resp.result, code))
            {
                await Shell.Current.DisplayAlert("提示", "该物料已在列表中，请勿重复添加", "确定");
                MaterialScanEntry.Text = string.Empty;
                return;
            }

            _materialRows.Add(resp.result);
            MaterialScanEntry.Text = string.Empty;
        }
        finally
        {
            _pendingMaterialScanCodes.Remove(code);
        }
    }


    private bool HasDuplicateTool(string scanCode)
    {
        return _toolRows.Any(row => IsSameTool(row, scanCode));
    }

    private bool HasDuplicateTool(PreStartInspectionScanResourceDto candidate, string scanCode)
    {
        return _toolRows.Any(row => IsSameTool(row, candidate) || IsSameTool(row, scanCode));
    }

    private static bool IsSameTool(PreStartInspectionScanResourceDto row, string scanCode)
    {
        return HasSameValue(scanCode, row.resourceCode)
            || HasSameValue(scanCode, row.model)
            || HasSameValue(scanCode, row.resourceDemandId);
    }

    private static bool IsSameTool(PreStartInspectionScanResourceDto row, PreStartInspectionScanResourceDto candidate)
    {
        return HasSameValue(row.resourceCode, candidate.resourceCode)
            || HasSameValue(row.resourceDemandId, candidate.resourceDemandId)
            || (HasSameValue(row.model, candidate.model)
                && HasSameValue(row.resourceName, candidate.resourceName)
                && HasSameValue(row.resourceType, candidate.resourceType));
    }

    private bool HasDuplicateMaterial(string scanCode)
    {
        return _materialRows.Any(row => IsSameMaterial(row, scanCode));
    }

    private bool HasDuplicateMaterial(PreStartInspectionScanMaterialDto candidate, string scanCode)
    {
        return _materialRows.Any(row => IsSameMaterial(row, candidate) || IsSameMaterial(row, scanCode));
    }

    private static bool IsSameMaterial(PreStartInspectionScanMaterialDto row, string scanCode)
    {
        return HasSameValue(scanCode, row.materialCode)
            || HasSameValue(scanCode, row.matReqNo);
    }

    private static bool IsSameMaterial(PreStartInspectionScanMaterialDto row, PreStartInspectionScanMaterialDto candidate)
    {
        return HasSameValue(row.materialCode, candidate.materialCode)
            || HasSameValue(row.matReqNo, candidate.matReqNo);
    }

    private static bool HasSameValue(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left)
            && !string.IsNullOrWhiteSpace(right)
            && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ScanCodeAsync()
    {
        var navigation = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (navigation == null)
        {
            var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("提示", "无法打开扫码页面", "确定");
            }

            return null;
        }

        var tcs = new TaskCompletionSource<string>();
        await navigation.PushAsync(new QrScanPage(tcs));
        return await tcs.Task;
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
            if (resp.success)
            {
                await CompleteAsync(true);
                return;
            }

            await Shell.Current.DisplayAlert("提示", resp.message ?? "点检确认提交失败", "确定");
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }


    private async Task CompleteAsync(bool result)
    {
        if (!_completionSource.Task.IsCompleted)
        {
            _completionSource.TrySetResult(result);
        }

        var navigation = Shell.Current?.Navigation ?? Navigation;
        if (navigation.NavigationStack.LastOrDefault() == this)
        {
            await navigation.PopAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(false);
        return true;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CompleteAsync(false);
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
