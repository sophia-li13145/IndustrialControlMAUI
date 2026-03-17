using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class DeviceScanBindViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkOrderApi _api;
    private readonly List<DevicesInfo> _deviceCache = new();

    [ObservableProperty] private string workOrderNo = string.Empty;
    [ObservableProperty] private string workOrderName = string.Empty;
    [ObservableProperty] private string materialName = string.Empty;
    [ObservableProperty] private string processName = string.Empty;
    [ObservableProperty] private string scheQty = string.Empty;

    [ObservableProperty] private string factoryCode = string.Empty;
    [ObservableProperty] private string processCode = string.Empty;
    [ObservableProperty] private string? lineCode;
    [ObservableProperty] private string schemeNo = string.Empty;
    [ObservableProperty] private string? platPlanNo;

    [ObservableProperty] private string? deviceCodeInput;
    [ObservableProperty] private StatusOption? selectedDeviceOption;
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<StatusOption> DeviceOptions { get; } = new();
    public ObservableCollection<WorkOrderDeviceBindItem> BoundDevices { get; } = new();

    public DeviceScanBindViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("task", out var obj) || obj is not ProcessTask task)
            return;

        WorkOrderNo = task.WorkOrderNo ?? string.Empty;
        WorkOrderName = task.WorkOrderName ?? string.Empty;
        MaterialName = task.MaterialName ?? string.Empty;
        ProcessName = task.ProcessName ?? string.Empty;
        ScheQty = task.ScheQty?.ToString("G29") ?? string.Empty;
        FactoryCode = task.FactoryCode ?? string.Empty;
        ProcessCode = task.ProcessCode ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(task.Id))
        {
            var detailResp = await _api.GetWorkProcessTaskDetailAsync(task.Id!);
            if (detailResp?.success == true && detailResp.result is not null)
            {
                var detail = detailResp.result;
                FactoryCode = detail.factoryCode ?? task.FactoryCode ?? string.Empty;
                ProcessCode = detail.processCode ?? task.ProcessCode ?? string.Empty;
                LineCode = detail.line;
                SchemeNo = detail.schemeNo ?? string.Empty;
                PlatPlanNo = detail.platPlanNo;
            }
            else
            {
                FactoryCode = task.FactoryCode ?? string.Empty;
                ProcessCode = task.ProcessCode ?? string.Empty;
            }
        }

        await LoadDevicesAsync();
        await LoadBoundDevicesAsync();
    }

    [RelayCommand]
    private async Task LoadBoundDevicesAsync()
    {
        if (!CanCallApi()) return;

        try
        {
            IsBusy = true;
            var resp = await _api.GetWorkOrderDeviceListAsync(FactoryCode, ProcessCode, SchemeNo, WorkOrderNo);
            BoundDevices.Clear();
            if (resp?.result != null)
            {
                foreach (var item in resp.result)
                    BoundDevices.Add(item);
            }
        }
        catch (Exception ex)
        {
            await ShowTip($"查询绑定设备失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BindScanDeviceAsync()
    {
        await BindByInputCodeAsync(DeviceCodeInput);
    }

    public async Task BindByInputCodeAsync(string? code)
    {
        var inputCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(inputCode))
        {
            await ShowTip("请先输入或扫码设备编号");
            return;
        }

        //var matched = _deviceCache.FirstOrDefault(x =>
        //    string.Equals(x.deviceCode?.Trim(), inputCode, StringComparison.OrdinalIgnoreCase));

        //if (matched is null || string.IsNullOrWhiteSpace(matched.deviceCode))
        //{
        //    await ShowTip("此设备不在系统中");
        //    return;
        //}

        DeviceCodeInput = inputCode;
        SelectedDeviceOption = DeviceOptions.FirstOrDefault(x =>
            string.Equals(x.Value, inputCode, StringComparison.OrdinalIgnoreCase));

        await BindDeviceAsync(inputCode);
    }

    public async Task BindManualDeviceByCodeAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            await ShowTip("请先选择设备");
            return;
        }

        await BindDeviceAsync(code.Trim());
    }

    private async Task BindDeviceAsync(string deviceCode)
    {
        if (IsBusy || !CanCallApi()) return;
        var bindOk = false;

        try
        {
            IsBusy = true;
            var req = new BindWorkOrderDeviceReq
            {
                deviceCode = deviceCode,
                factoryCode = FactoryCode,
                processCode = ProcessCode,
                schemeNo = SchemeNo,
                workOrderNo = WorkOrderNo,
                platPlanNo = PlatPlanNo
            };

            var resp = await _api.BindWorkOrderDeviceAsync(req);
            if (resp?.success == true && resp.result)
            {
                bindOk = true;
                await ShowTip("绑定成功");
                DeviceCodeInput = string.Empty;
            }
            else
            {
                await ShowTip(resp?.message ?? "绑定失败");
            }
        }
        catch (Exception ex)
        {
            await ShowTip($"绑定失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }

        if (bindOk)
            await LoadBoundDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        _deviceCache.Clear();
        DeviceOptions.Clear();
        DeviceOptions.Add(new StatusOption { Text = "请选择设备", Value = null });

        if (string.IsNullOrWhiteSpace(FactoryCode) || string.IsNullOrWhiteSpace(ProcessCode))
        {
            SelectedDeviceOption = DeviceOptions.FirstOrDefault();
            return;
        }

        try
        {
            var resp = await _api.GetDeviceOptionsAsync(FactoryCode, ProcessCode, LineCode);
            if (resp?.result != null)
            {
                foreach (var d in resp.result)
                {
                    if (string.IsNullOrWhiteSpace(d.deviceCode)) continue;
                    _deviceCache.Add(d);
                    DeviceOptions.Add(new StatusOption
                    {
                        Text = d.deviceName ?? d.deviceCode!,
                        Value = d.deviceCode
                    });
                }
            }
        }
        catch
        {
            // 下拉加载失败不阻塞主流程
        }

        SelectedDeviceOption = DeviceOptions.FirstOrDefault();
    }

    private bool CanCallApi()
    {
        if (!string.IsNullOrWhiteSpace(FactoryCode)
            && !string.IsNullOrWhiteSpace(ProcessCode)
            && !string.IsNullOrWhiteSpace(SchemeNo)
            && !string.IsNullOrWhiteSpace(WorkOrderNo))
            return true;

        _ = ShowTip("缺少工单上下文参数，无法请求设备绑定接口");
        return false;
    }

    private static Task ShowTip(string message)
        => Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;
}
