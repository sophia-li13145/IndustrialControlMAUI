using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class RepairRunDetailPage : ContentPage
{
    private readonly RepairRunDetailViewModel _vm;
    /// <summary>执行 RepairRunDetailPage 初始化逻辑。</summary>
    public RepairRunDetailPage() : this(ServiceHelper.GetService<RepairRunDetailViewModel>()) { }

    /// <summary>执行 RepairRunDetailPage 初始化逻辑。</summary>
    public RepairRunDetailPage(RepairRunDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();


    }

    private async void OnPickImagesClicked(object sender, EventArgs e)
         => await _vm.PickImagesAsync();

    private async void OnPickFileClicked(object sender, EventArgs e)
        => await _vm.PickFilesAsync();

}