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

    public StatusOption? FindDeviceOptionByCode(string? code)
    {
        var inputCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(inputCode))
            return null;

        return DeviceOptions.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.Value)
            && string.Equals(x.Value, inputCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task BindByInputCodeAsync(string? code, DateTime operationTime)
    {
        var inputCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(inputCode))
        {
            await ShowTip("请先输入或扫码设备编号");
            return;
        }

        var matchedOption = FindDeviceOptionByCode(inputCode);
        if (matchedOption is null || string.IsNullOrWhiteSpace(matchedOption.Value))
        {
            await ShowTip("此设备不在当前工序可绑定设备列表中");
            return;
        }

        DeviceCodeInput = inputCode;
        SelectedDeviceOption = matchedOption;

        var existedItem = FindBoundDevice(inputCode);
        if (existedItem is null)
        {
            await BindNewDeviceAsync(inputCode);
            return;
        }

        var startTime = TryParseDateTime(existedItem.startTime) ?? operationTime;
        await UpdateBoundDeviceTimeAsync(inputCode, startTime, operationTime);
    }

    public async Task BindManualDeviceByCodeAsync(string? code)
    {
        var inputCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(inputCode))
        {
            await ShowTip("请先选择设备");
            return;
        }

        var matchedOption = FindDeviceOptionByCode(inputCode);
        if (matchedOption is null || string.IsNullOrWhiteSpace(matchedOption.Value))
        {
            await ShowTip("请选择有效设备");
            return;
        }

        SelectedDeviceOption = matchedOption;
        await BindNewDeviceAsync(inputCode);
    }

    public async Task<ApiResp<bool?>> EditBoundDeviceTimeAsync(
        WorkOrderDeviceBindItem? item,
        DateTime? startTime,
        DateTime? endTime)
    {
        if (item is null)
            return new ApiResp<bool?> { success = false, message = "缺少设备信息", result = false };

        var deviceCode = item.deviceCode?.Trim();
        if (string.IsNullOrWhiteSpace(deviceCode))
            return new ApiResp<bool?> { success = false, message = "缺少设备编号", result = false };

        if (!startTime.HasValue || !endTime.HasValue)
            return new ApiResp<bool?> { success = false, message = "请选择开始时间和结束时间", result = false };

        if (startTime > endTime)
            return new ApiResp<bool?> { success = false, message = "开始时间不能晚于结束时间", result = false };

        if (IsBusy || !CanCallApi())
            return new ApiResp<bool?> { success = false, message = "当前无法提交编辑", result = false };

        try
        {
            IsBusy = true;
            var req = new EditWorkOrderDeviceBindTimeReq
            {
                deviceCode = deviceCode,
                factoryCode = item.factoryCode?.Trim() ?? FactoryCode,
                processCode = item.processCode?.Trim() ?? ProcessCode,
                workOrderNo = item.workOrderNo?.Trim() ?? WorkOrderNo,
                startTime = startTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                endTime = endTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var resp = await _api.EditWorkOrderDeviceBindTimeAsync(req);
            if (resp?.success == true && resp.result == true)
            {
                item.startTime = req.startTime;
                item.endTime = req.endTime;
            }

            return resp ?? new ApiResp<bool?> { success = false, message = "编辑失败", result = false };
        }
        catch (Exception ex)
        {
            return new ApiResp<bool?> { success = false, message = $"编辑失败：{ex.Message}", result = false };
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBoundDeviceAsync(WorkOrderDeviceBindItem? item)
    {
        if (item is null)
            return;

        var deviceCode = item.deviceCode?.Trim();
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            await ShowTip("缺少设备编号，无法解绑");
            return;
        }

        if (IsBusy || !CanCallApi())
            return;

        var confirm = await (Shell.Current?.DisplayAlert(
            "删除提醒",
            $"确认删除设备【{item.deviceName ?? deviceCode}】的绑定关系吗？",
            "确定",
            "取消") ?? Task.FromResult(false));

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            var req = new UnbindWorkOrderDeviceReq
            {
                deviceCode = deviceCode,
                factoryCode = item.factoryCode?.Trim() ?? FactoryCode,
                processCode = item.processCode?.Trim() ?? ProcessCode,
                workOrderNo = item.workOrderNo?.Trim() ?? WorkOrderNo
            };

            var resp = await _api.UnbindWorkOrderDeviceAsync(req);
            if (resp?.success == true && resp.result == true)
            {
                BoundDevices.Remove(item);
                await ShowTip("解绑成功");
                return;
            }

            await ShowTip(resp?.message ?? "解绑失败");
        }
        catch (Exception ex)
        {
            await ShowTip($"解绑失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BindNewDeviceAsync(string deviceCode)
    {
        if (IsBusy || !CanCallApi())
            return;

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

    private async Task UpdateBoundDeviceTimeAsync(string deviceCode, DateTime startTime, DateTime endTime)
    {
        if (IsBusy || !CanCallApi())
            return;

        if (startTime > endTime)
        {
            await ShowTip("开始时间不能晚于结束时间");
            return;
        }

        var bindOk = false;

        try
        {
            IsBusy = true;
            var req = new EditWorkOrderDeviceBindTimeReq
            {
                deviceCode = deviceCode,
                factoryCode = FactoryCode,
                processCode = ProcessCode,
                workOrderNo = WorkOrderNo,
                startTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                endTime = endTime.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var resp = await _api.EditWorkOrderDeviceBindTimeAsync(req);
            if (resp?.success == true && resp.result == true)
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

    private WorkOrderDeviceBindItem? FindBoundDevice(string deviceCode) =>
        BoundDevices.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.deviceCode)
            && string.Equals(x.deviceCode.Trim(), deviceCode.Trim(), StringComparison.OrdinalIgnoreCase));

    private static DateTime? TryParseDateTime(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt : null;

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
