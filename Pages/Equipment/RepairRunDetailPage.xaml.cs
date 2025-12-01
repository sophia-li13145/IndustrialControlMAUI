using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class RepairRunDetailPage : ContentPage
{
    private readonly RepairRunDetailViewModel _vm;
    public RepairRunDetailPage() : this(ServiceHelper.GetService<RepairRunDetailViewModel>()) { }

    public RepairRunDetailPage(RepairRunDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();


    }

    private async void OnPickImagesClicked(object sender, EventArgs e)
         => await _vm.PickImagesAsync();

    private async void OnPickFileClicked(object sender, EventArgs e)
        => await _vm.PickFilesAsync();

}