using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameEmptyOperationPage : ContentPage
{
    private readonly FrameEmptyOperationViewModel _vm;

    public FrameEmptyOperationPage(FrameEmptyOperationViewModel vm)
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
        await Shell.Current.GoToAsync(nameof(FrameEmptyAddPage));
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Models.FrameUseRecordOperation record) return;
        var key = FrameLoadOperationNavigationStore.Put(record.id);
        await Shell.Current.GoToAsync($"{nameof(FrameEmptyOperationDetailPage)}?recordKey={Uri.EscapeDataString(key)}");
    }
}
