using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class DeviceMoldRelationPage : ContentPage
{
    public DeviceMoldRelationPage(DeviceMoldRelationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnScanDeviceClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceMoldRelationViewModel vm)
            return;

        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        await vm.HandleScannedDeviceCodeAsync(result);
    }

    private async void OnDeviceCodeCompleted(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceMoldRelationViewModel vm)
            return;

        await vm.QueryAsync();
    }
}
