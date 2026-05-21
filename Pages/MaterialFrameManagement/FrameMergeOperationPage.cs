using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameMergeOperationPage : MaterialFrameQueryPage
{
    public FrameMergeOperationPage(MaterialFrameQueryViewModel vm) : base(vm)
    {
        ConfigureAsOperationPage("合框", "frame_closing");
    }
}
