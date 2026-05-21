using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameUnloadOperationViewModel : FrameUseRecordOperationListViewModel
{
    public FrameUnloadOperationViewModel(IMaterialFrameApi api) : base(api, "拆框操作", "frame_removal")
    {
    }
}
