using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameDumpOperationViewModel : FrameUseRecordOperationListViewModel
{
    public FrameDumpOperationViewModel(IMaterialFrameApi api) : base(api, "倒框操作", "frame_turnover")
    {
    }
}
