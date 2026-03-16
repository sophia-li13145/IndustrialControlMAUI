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
        await vm.BindByInputCodeAsync(result.Trim());
    }

    private async void OnDeviceCodeCompleted(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        await vm.BindByInputCodeAsync(DeviceCodeEntry.Text?.Trim());
    }

    private async void OnManualBindClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceScanBindViewModel vm)
            return;

        var popup = new ManualDeviceBindPopup(vm.DeviceOptions, vm.SelectedDeviceOption);
        var result = await this.ShowPopupAsync(popup);
        if (result is not StatusOption opt || string.IsNullOrWhiteSpace(opt.Value))
            return;

        await vm.BindManualDeviceByCodeAsync(opt.Value);
    }
}
