using IndustrialControlMAUI.ViewModels;
using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Pages;

public partial class FrameLoadOperationPage : ContentPage
{
    private readonly FrameLoadOperationViewModel _vm;

    public FrameLoadOperationPage(FrameLoadOperationViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    private async void OnAddRecordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(FrameLoadAddPage));
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FrameUseRecordOperation record) return;
        var key = FrameLoadOperationNavigationStore.Put(record.id);
        await Shell.Current.GoToAsync($"{nameof(FrameLoadOperationDetailPage)}?recordKey={Uri.EscapeDataString(key)}");
    }
}
