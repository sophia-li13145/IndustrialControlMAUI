using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameLoadOperationViewModel : FrameUseRecordOperationListViewModel
{
    public FrameLoadOperationViewModel(IMaterialFrameApi api) : base(api, "装框操作", "framing")
    {
    }
}
