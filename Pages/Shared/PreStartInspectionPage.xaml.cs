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
