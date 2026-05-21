using IndustrialControlMAUI.Models;
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

    private void OnPickClicked(object sender, EventArgs e) => PickerOverlay.IsVisible = true;
    private void OnClosePicker(object sender, EventArgs e) => PickerOverlay.IsVisible = false;

    private async void OnScanClicked(object sender, EventArgs e)
    {
        await _vm.ScanAndBindAsync(Navigation);
    }

    private void OnMaterialSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not BasMaterialRecord item) return;
        _vm.SelectMaterial(item);
        if (sender is CollectionView cv) cv.SelectedItem = null;
        PickerOverlay.IsVisible = false;
    }
}
