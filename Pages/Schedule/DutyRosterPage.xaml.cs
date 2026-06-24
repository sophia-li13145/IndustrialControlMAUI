using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class DutyRosterPage : ContentPage
{
    private readonly DutyRosterViewModel _vm;

    public DutyRosterPage(DutyRosterViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Dates.Count == 0 && !_vm.IsBusy)
            await _vm.LoadAsync(DateTime.Today);
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
