using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameUnloadAddPage : ContentPage
{
    private readonly FrameUnloadAddViewModel _vm;

    public FrameUnloadAddPage(FrameUnloadAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async void OnScanSourceClicked(object sender, TappedEventArgs e)
    {
        await _vm.ScanAndPickSourceFrameAsync(Navigation);
    }

    private async void OnScanTargetClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndAddTargetFrameAsync(Navigation);
    }
}
