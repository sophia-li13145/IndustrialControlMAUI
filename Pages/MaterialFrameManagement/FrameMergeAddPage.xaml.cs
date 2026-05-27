using IndustrialControlMAUI.ViewModels;
namespace IndustrialControlMAUI.Pages;
public partial class FrameMergeAddPage : ContentPage
{
    private readonly FrameMergeAddViewModel _vm;
    public FrameMergeAddPage(FrameMergeAddViewModel vm){ InitializeComponent(); BindingContext=_vm=vm; }
    private async void OnScanSourceClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndAddSourceFrameAsync(Navigation);
    }

    private async void OnScanTargetClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedSourceFrames.Count == 0)
        {
            await DisplayAlert("提示", "请先选择或扫码来源料框", "确定");
            return;
        }

        await _vm.ScanAndPickTargetFrameAsync(Navigation);
    }
}
