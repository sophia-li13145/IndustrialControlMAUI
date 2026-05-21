using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameLoadOperationViewModel : MaterialFrameQueryViewModel
{
    public FrameLoadOperationViewModel(IMaterialFrameApi api) : base(api)
    {
        ApplyOperation("装框", "framing");
    }
}
