using IndustrialControlMAUI.ViewModels;
namespace IndustrialControlMAUI.Pages;
public partial class FrameMergeAddPage : ContentPage
{
    private readonly FrameMergeAddViewModel _vm;
    public FrameMergeAddPage(FrameMergeAddViewModel vm){ InitializeComponent(); BindingContext=_vm=vm; }
    private async void OnScanTargetClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndPickTargetFrameAsync(Navigation);
    }
}
