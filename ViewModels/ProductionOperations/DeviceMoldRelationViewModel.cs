using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class DeviceMoldRelationViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;

    [ObservableProperty] private string? deviceCode;
    [ObservableProperty] private string? moldCode;
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

    private static Task ShowTip(string message)
        => Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;
}
