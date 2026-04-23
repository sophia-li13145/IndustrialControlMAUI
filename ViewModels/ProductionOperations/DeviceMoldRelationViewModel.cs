using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace IndustrialControlMAUI.ViewModels;

public partial class DeviceMoldRelationViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;

    [ObservableProperty] private string? deviceCode;
    [ObservableProperty] private string? moldCode;
    [ObservableProperty] private string? deviceModel;
    [ObservableProperty] private string? deviceName;
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<DeviceMoldRelationDto> Records { get; } = new();

    public DeviceMoldRelationViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task QueryAsync()
    {
        var code = DeviceCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            await ShowTip("请先输入或扫码设备码");
            return;
        }

        try
        {
            IsBusy = true;
            var resp = await _api.PageDeviceMoldRelationsAsync(code, 1, 100, true);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Records.Clear();
                var list = resp?.result?.records;
                if (list == null)
                    return;

                foreach (var row in list)
                    Records.Add(row);

                var first = list.FirstOrDefault();
                if (first is not null)
                {
                    DeviceModel = first.deviceModel;
                    DeviceName = first.deviceName;
                }
            });
        }
        catch (Exception ex)
        {
            await ShowTip($"查询设备装模关系失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task HandleScannedDeviceCodeAsync(string? code)
    {
        var val = code?.Trim();
        if (string.IsNullOrWhiteSpace(val))
            return;

        DeviceCode = val;
        await QueryAsync();
    }

    public async Task<DeviceMoldRelationDto?> QueryMoldDetailAsync(string? moldCode)
    {
        var code = moldCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            await ShowTip("请先输入或扫码模具码");
            return null;
        }

        MoldCode = code;

        try
        {
            IsBusy = true;
            var resp = await _api.GetDeviceMoldRelationByMoldCodeAsync(code);
            if (resp?.success == true && resp.result is not null)
                return resp.result;

            await ShowTip(resp?.message ?? "查询模具信息失败");
            return null;
        }
        catch (Exception ex)
        {
            await ShowTip($"查询模具信息失败：{ex.Message}");
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> ConfirmInstallAsync(DeviceMoldRelationDto detail)
    {
        var device = DeviceCode?.Trim();
        if (string.IsNullOrWhiteSpace(device))
        {
            await ShowTip("请先输入或扫码设备码");
            return false;
        }

        if (string.IsNullOrWhiteSpace(detail.moldCode) || string.IsNullOrWhiteSpace(detail.moldModel))
        {
            await ShowTip("模具信息不完整，无法安装");
            return false;
        }

        try
        {
            IsBusy = true;
            var req = new AddDeviceMoldRelationReq
            {
                deviceCode = device,
                deviceModel = DeviceModel ?? detail.deviceModel,
                deviceName = DeviceName ?? detail.deviceName,
                moldCode = detail.moldCode,
                moldModel = detail.moldModel
            };

            var resp = await _api.AddDeviceMoldRelationAsync(req);
            if (resp?.success == true && resp.result == true)
            {
                await ShowTip("安装成功");
                await QueryAsync();
                return true;
            }

            await ShowTip(resp?.message ?? "安装失败");
            return false;
        }
        catch (Exception ex)
        {
            await ShowTip($"安装失败：{ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<DeviceMoldRelationDto?> QueryRelationDetailByIdAsync(string? id)
    {
        var relationId = id?.Trim();
        if (string.IsNullOrWhiteSpace(relationId))
        {
            await ShowTip("缺少关联主键，无法卸模");
            return null;
        }

        try
        {
            IsBusy = true;
            var resp = await _api.GetDeviceMoldRelationAsync(relationId);
            if (resp?.success == true && resp.result is not null)
                return resp.result;

            await ShowTip(resp?.message ?? "查询卸模详情失败");
            return null;
        }
        catch (Exception ex)
        {
            await ShowTip($"查询卸模详情失败：{ex.Message}");
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> ConfirmUnloadAsync(string? id)
    {
        var relationId = id?.Trim();
        if (string.IsNullOrWhiteSpace(relationId))
        {
            await ShowTip("缺少关联主键，无法卸模");
            return false;
        }

        try
        {
            IsBusy = true;
            var resp = await _api.ConfirmUnloadMoldAsync(relationId);
            if (resp?.success == true && resp.result == true)
            {
                await ShowTip("卸模成功");
                await QueryAsync();
                return true;
            }

            await ShowTip(resp?.message ?? "卸模失败");
            return false;
        }
        catch (Exception ex)
        {
            await ShowTip($"卸模失败：{ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static Task ShowTip(string message)
        => Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;
}
