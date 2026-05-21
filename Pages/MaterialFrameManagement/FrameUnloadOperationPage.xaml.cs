using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameUnloadOperationPage : ContentPage
{
    private readonly FrameUnloadOperationViewModel _vm;

    public FrameUnloadOperationPage(FrameUnloadOperationViewModel vm)
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
}
