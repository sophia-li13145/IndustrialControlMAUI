using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class LineDowntimeSearchPage : ContentPage
{
    private readonly LineDowntimeSearchViewModel _vm;

    public LineDowntimeSearchPage(LineDowntimeSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Records.Count == 0)
            await _vm.SearchAsync();
    }
}
