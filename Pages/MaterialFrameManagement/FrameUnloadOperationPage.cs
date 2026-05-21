using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameUnloadOperationPage : MaterialFrameQueryPage
{
    public FrameUnloadOperationPage(MaterialFrameQueryViewModel vm) : base(vm)
    {
        ConfigureAsOperationPage("拆框", "frame_removal");
    }
}
