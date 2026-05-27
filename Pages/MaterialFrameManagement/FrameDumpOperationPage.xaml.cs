using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameDumpOperationPage : ContentPage
{
    private readonly FrameDumpOperationViewModel _vm;

    public FrameDumpOperationPage(FrameDumpOperationViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
    }

    private async void OnAddRecordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(FramePourAddPage));
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Models.FrameUseRecordOperation record) return;
        var key = FrameLoadOperationNavigationStore.Put(record.id);
        await Shell.Current.GoToAsync($"{nameof(FrameDumpOperationDetailPage)}?recordKey={Uri.EscapeDataString(key)}");
    }
}
