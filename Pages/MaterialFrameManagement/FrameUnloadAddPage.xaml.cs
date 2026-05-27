using IndustrialControlMAUI.ViewModels;
using System.ComponentModel;

namespace IndustrialControlMAUI.Pages;

public partial class FrameUnloadAddPage : ContentPage
{
    private readonly FrameUnloadAddViewModel _vm;

    public FrameUnloadAddPage(FrameUnloadAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        UpdateOverlayInputState();
    }

    private async void OnScanSourceClicked(object sender, TappedEventArgs e)
    {
        await _vm.ScanAndPickSourceFrameAsync(Navigation);
    }

    private async void OnScanTargetClicked(object sender, EventArgs e)
    {
        if (!_vm.HasSelectedSourceFrame)
        {
            await DisplayAlert("提示", "请先选择或扫码来源料框", "确定");
            return;
        }

        await _vm.ScanAndAddTargetFrameAsync(Navigation);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FrameUnloadAddViewModel.IsSourcePickerVisible)
            || e.PropertyName == nameof(FrameUnloadAddViewModel.IsTargetPickerVisible))
        {
            UpdateOverlayInputState();
        }
    }

    private void UpdateOverlayInputState()
    {
        SourcePickerOverlay.InputTransparent = !_vm.IsSourcePickerVisible;
        TargetPickerOverlay.InputTransparent = !_vm.IsTargetPickerVisible;
    }
}
