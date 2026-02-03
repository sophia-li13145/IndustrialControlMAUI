using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class RepairDetailPage : ContentPage
{
    private readonly RepairDetailViewModel _vm;
    /// <summary>执行 RepairDetailPage 初始化逻辑。</summary>
    public RepairDetailPage() : this(ServiceHelper.GetService<RepairDetailViewModel>()) { }

    /// <summary>执行 RepairDetailPage 初始化逻辑。</summary>
    public RepairDetailPage(RepairDetailViewModel vm)
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