using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameDumpOperationPage : MaterialFrameQueryPage
{
    public FrameDumpOperationPage(MaterialFrameQueryViewModel vm) : base(vm)
    {
        ConfigureAsOperationPage("倒框", "frame_turnover");
    }
}
