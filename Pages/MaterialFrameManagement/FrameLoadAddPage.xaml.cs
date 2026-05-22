using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameLoadAddPage : ContentPage
{
    private readonly FrameLoadAddViewModel _vm;

    public FrameLoadAddPage(FrameLoadAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadMaterialsAsync();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndBindAsync(Navigation);
    }
}
