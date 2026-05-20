using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialControlMAUI.Pages;

public partial class ReportAddPopupPage : ContentPage
{
    public ReportAddPopupPage(ReportAddPopupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public static async Task<bool> ShowAsync(IServiceProvider? sp, WorkProcessTaskDetail detail)
    {
        var tcs = new TaskCompletionSource<bool>();
        var provider = sp ?? Application.Current?.Handler?.MauiContext?.Services;
        var vm = provider is not null
            ? ActivatorUtilities.CreateInstance<ReportAddPopupViewModel>(provider)
            : throw new InvalidOperationException("无法获取服务容器");
        await vm.InitAsync(detail);
        vm.SetResultTcs(tcs);
        var page = new ReportAddPopupPage(vm);
        if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PushModalAsync(page);
        return await tcs.Task;
    }
}
