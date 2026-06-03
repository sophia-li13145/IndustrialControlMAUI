using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FinalProcessCompletePopupPage : ContentPage
{
    public FinalProcessCompletePopupPage(FinalProcessCompletePopupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public static async Task<FinalProcessCompletePopupResult?> ShowAsync(IServiceProvider? sp, decimal? initialActQty = null)
    {
        var tcs = new TaskCompletionSource<FinalProcessCompletePopupResult?>();
        var provider = sp ?? Application.Current?.Handler?.MauiContext?.Services;
        var vm = provider is not null
            ? ActivatorUtilities.CreateInstance<FinalProcessCompletePopupViewModel>(provider)
            : new FinalProcessCompletePopupViewModel();
        vm.SetResultTcs(tcs);
        vm.Initialize(initialActQty);

        var page = new FinalProcessCompletePopupPage(vm);
        if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PushModalAsync(page);

        return await tcs.Task;
    }
}
