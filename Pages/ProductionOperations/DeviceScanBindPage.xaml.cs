using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class DeviceScanBindPage : ContentPage
{
    public DeviceScanBindPage(DeviceScanBindViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnScanDeviceClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        DeviceCodeEntry.Text = result.Trim();
        await PromptBindTimeAndBindAsync(vm, result.Trim(), isManualSelect: false);
    }

    private async void OnDeviceCodeCompleted(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        await PromptBindTimeAndBindAsync(vm, DeviceCodeEntry.Text?.Trim(), isManualSelect: false);
    }

    private async void OnManualBindClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        var popup = new ManualDeviceBindPopup(vm.DeviceOptions, vm.SelectedDeviceOption);
        var result = await this.ShowPopupAsync(popup);
        if (result is not StatusOption opt || string.IsNullOrWhiteSpace(opt.Value))
            return;

        await PromptBindTimeAndBindAsync(vm, opt.Value, isManualSelect: true);
    }

    private async void OnEditBoundDeviceClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm
            || sender is not Button button
            || button.CommandParameter is not WorkOrderDeviceBindItem item)
            return;

        static DateTime? ParseDateTime(string? value) =>
            DateTime.TryParse(value, out var dt) ? dt : null;

        var popup = new DeviceBindTimeEditPopup(ParseDateTime(item.startTime), ParseDateTime(item.endTime));
        var result = await this.ShowPopupAsync(popup);
        if (result is not DeviceBindTimeEditResult editResult || !editResult.Confirmed)
            return;

        var resp = await vm.EditBoundDeviceTimeAsync(item, editResult.StartTime, editResult.EndTime);
        if (resp?.success == true && resp.result == true)
        {
            await DisplayAlert("提示", "编辑成功", "确定");
            await vm.LoadBoundDevicesCommand.ExecuteAsync(null);
            return;
        }

        await DisplayAlert("提示", resp?.message ?? "编辑失败", "确定");
    }

    private async Task PromptBindTimeAndBindAsync(
        DeviceScanBindViewModel vm,
        string? deviceCode,
        bool isManualSelect)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            await DisplayAlert("提示", isManualSelect ? "请先选择设备" : "请先输入或扫码设备编号", "确定");
            return;
        }

        var popup = new DeviceBindTimeEditPopup(DateTime.Now, DateTime.Now);
        var result = await this.ShowPopupAsync(popup);
        if (result is not DeviceBindTimeEditResult editResult || !editResult.Confirmed)
            return;

        if (isManualSelect)
        {
            await vm.BindManualDeviceByCodeAsync(deviceCode, editResult.StartTime, editResult.EndTime);
            return;
        }

        await vm.BindByInputCodeAsync(deviceCode, editResult.StartTime, editResult.EndTime);
    }
}
