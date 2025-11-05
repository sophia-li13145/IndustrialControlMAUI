using IndustrialControl.ViewModels.Energy;

namespace IndustrialControlMAUI.Pages;

public partial class ManualReadingPage : ContentPage
{
    public ManualReadingPage(ManualReadingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ManualReadingViewModel vm)
            await vm.EnsureUsersAsync();
    }
}