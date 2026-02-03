using IndustrialControl.ViewModels.Energy;

namespace IndustrialControlMAUI.Pages;

public partial class ManualReadingPage : ContentPage
{
    /// <summary>执行 ManualReadingPage 初始化逻辑。</summary>
    public ManualReadingPage(ManualReadingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ManualReadingViewModel vm)
            await vm.EnsureUsersAsync();
    }
}