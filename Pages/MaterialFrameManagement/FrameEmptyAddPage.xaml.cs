using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameEmptyAddPage : ContentPage
{
    private readonly FrameEmptyAddViewModel _vm;

    public FrameEmptyAddPage(FrameEmptyAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndAddFrameAsync(Navigation);
    }
}
