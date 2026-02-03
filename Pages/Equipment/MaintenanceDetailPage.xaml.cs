using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MaintenanceDetailPage : ContentPage
{
    private readonly MaintenanceDetailViewModel _vm;
    /// <summary>执行 MaintenanceDetailPage 初始化逻辑。</summary>
    public MaintenanceDetailPage() : this(ServiceHelper.GetService<MaintenanceDetailViewModel>()) { }

    /// <summary>执行 MaintenanceDetailPage 初始化逻辑。</summary>
    public MaintenanceDetailPage(MaintenanceDetailViewModel vm)
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

    

}