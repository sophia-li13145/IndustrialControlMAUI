using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FrameEmptyAddPage : ContentPage
{
    public FrameEmptyAddPage(FrameEmptyAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
