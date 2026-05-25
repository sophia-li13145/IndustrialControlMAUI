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
        await _vm.InitializeAsync();
    }

    private async void OnAddRecordClicked(object sender, EventArgs e)
    {
        await DisplayAlert("料框管理", "新增记录页面待接入", "确定");
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView cv) return;
        if (e.CurrentSelection.FirstOrDefault() is not Models.FrameUseRecordOperation record) return;
        cv.SelectedItem = null;
        var key = FrameLoadOperationNavigationStore.Put(record.id);
        await Shell.Current.GoToAsync($"{nameof(FrameEmptyOperationDetailPage)}?recordKey={Uri.EscapeDataString(key)}");
    }
}
