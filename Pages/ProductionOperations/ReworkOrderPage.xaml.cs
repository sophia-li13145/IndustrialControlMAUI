using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ReworkOrderPage : ContentPage
{
    public ReworkOrderPage() : this(ServiceHelper.GetService<ReworkOrderViewModel>()) { }

    public ReworkOrderPage(ReworkOrderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
