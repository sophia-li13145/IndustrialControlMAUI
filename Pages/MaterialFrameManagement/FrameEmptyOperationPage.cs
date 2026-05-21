using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameEmptyOperationPage : MaterialFrameQueryPage
{
    public FrameEmptyOperationPage(MaterialFrameQueryViewModel vm) : base(vm)
    {
        ConfigureAsOperationPage("空框", "empty_frame");
    }
}
