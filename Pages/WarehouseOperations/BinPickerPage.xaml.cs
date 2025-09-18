using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class BinPickerPage : ContentPage
{
    private readonly TaskCompletionSource<BinInfo?> _tcs = new();

    public BinPickerPage(string? preselectBinCode, BinPickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.Initialize(preselectBinCode);
    }

    public static async Task<BinInfo?> ShowAsync(string? preselectBinCode)
    {
        var sp = Application.Current!.Handler!.MauiContext!.Services!;
        var vm = sp.GetRequiredService<BinPickerViewModel>();
        var page = new BinPickerPage(preselectBinCode, vm);

        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation
                  ?? throw new InvalidOperationException("Navigation not ready");

        vm.SetCloseHandlers(
    onPicked: async (bin) =>
    {
        page._tcs.TrySetResult(bin);
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
            if (nav != null && nav.ModalStack.Count > 0 && nav.ModalStack.Last() is BinPickerPage)
                await nav.PopModalAsync();
        });
    },
    onCanceled: async () =>
    {
        page._tcs.TrySetResult(null);
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
            if (nav != null && nav.ModalStack.Count > 0 && nav.ModalStack.Last() is BinPickerPage)
                await nav.PopModalAsync();
        });
    });


        await nav.PushModalAsync(page);
        return await page._tcs.Task;
    }



    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (BindingContext is BinPickerViewModel vm)
            await vm.CancelAsync();
    }
}
