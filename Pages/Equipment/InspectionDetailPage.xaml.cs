using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class InspectionDetailPage : ContentPage
{
    private readonly InspectionDetailViewModel _vm;
    /// <summary>执行 InspectionDetailPage 初始化逻辑。</summary>
    public InspectionDetailPage() : this(ServiceHelper.GetService<InspectionDetailViewModel>()) { }

    /// <summary>执行 InspectionDetailPage 初始化逻辑。</summary>
    public InspectionDetailPage(InspectionDetailViewModel vm)
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