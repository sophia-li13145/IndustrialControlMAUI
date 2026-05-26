using IndustrialControlMAUI.ViewModels;
namespace IndustrialControlMAUI.Pages;
public partial class FramePourAddPage : ContentPage
{
    private readonly FramePourAddViewModel _vm;
    public FramePourAddPage(FramePourAddViewModel vm){ InitializeComponent(); BindingContext=_vm=vm; }
    private async void OnScanSourceClicked(object sender, TappedEventArgs e)
    {
        await _vm.ScanAndPickSourceFrameAsync(Navigation);
    }

    private async void OnScanTargetClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndPickTargetFrameAsync(Navigation);
    }
}
