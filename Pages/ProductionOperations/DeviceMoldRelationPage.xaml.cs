using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
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

    private async void OnScanMoldClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceMoldRelationViewModel vm)
            return;

        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        MoldCodeEntry.Text = result.Trim();
        await TryConfirmInstallAsync(vm, result);
    }

    private async void OnMoldCodeCompleted(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceMoldRelationViewModel vm)
            return;

        await TryConfirmInstallAsync(vm, MoldCodeEntry.Text);
    }

    private async Task TryConfirmInstallAsync(DeviceMoldRelationViewModel vm, string? moldCode)
    {
        DeviceMoldRelationDto? detail = await vm.QueryMoldDetailAsync(moldCode);
        if (detail is null)
            return;

        var popup = new MoldInstallConfirmPopup(detail);
        var result = await this.ShowPopupAsync(popup);
        if (result is true)
            await vm.ConfirmInstallAsync(detail);
    }

    private async void OnUnloadClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DeviceMoldRelationViewModel vm
            || sender is not Button btn
            || btn.CommandParameter is not DeviceMoldRelationDto row)
            return;

        var detail = await vm.QueryRelationDetailByIdAsync(row.id);
        if (detail is null)
            return;

        var popup = new MoldUnloadConfirmPopup(detail);
        var result = await this.ShowPopupAsync(popup);
        if (result is true)
            await vm.ConfirmUnloadAsync(detail.id);
    }
}
