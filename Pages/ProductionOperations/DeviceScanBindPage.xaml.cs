using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class DeviceScanBindPage : ContentPage, IQueryAttributable
{
    private readonly DeviceScanBindViewModel _vm;

    public DeviceScanBindPage() : this(ServiceHelper.GetService<DeviceScanBindViewModel>()) { }

    public DeviceScanBindPage(DeviceScanBindViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
        => _vm.ApplyQueryAttributes(query);

    private async void OnScanDeviceClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        var deviceCode = result.Trim();
        DeviceCodeEntry.Text = deviceCode;
        await PromptScanConfirmAndBindAsync(vm, deviceCode);
    }

    private async void OnDeviceCodeCompleted(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        await PromptScanConfirmAndBindAsync(vm, DeviceCodeEntry.Text?.Trim());
    }

    private async void OnManualBindClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        var popup = new ManualDeviceBindPopup(
            vm.DeviceOptions,
            vm.SelectedDeviceOption,
            allowDeviceSelection: true,
            title: "确认绑定");

        var result = await this.ShowPopupAsync(popup);
        if (result is not DeviceBindConfirmResult confirmResult
            || confirmResult.SelectedDeviceOption is not StatusOption opt
            || string.IsNullOrWhiteSpace(opt.Value))
            return;

        await vm.BindManualDeviceByCodeAsync(opt.Value);
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

    private async Task PromptScanConfirmAndBindAsync(DeviceScanBindViewModel vm, string? deviceCode)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            await DisplayAlert("提示", "请先输入或扫码设备编号", "确定");
            return;
        }

        var selectedOption = vm.FindDeviceOptionByCode(deviceCode) ?? new StatusOption
        {
            Text = deviceCode.Trim(),
            Value = deviceCode.Trim()
        };

        var popup = new ManualDeviceBindPopup(
            new[] { selectedOption },
            selectedOption,
            allowDeviceSelection: false,
            title: "确认绑定");

        var result = await this.ShowPopupAsync(popup);
        if (result is not DeviceBindConfirmResult)
            return;

        await vm.BindByInputCodeAsync(deviceCode);
    }
}
