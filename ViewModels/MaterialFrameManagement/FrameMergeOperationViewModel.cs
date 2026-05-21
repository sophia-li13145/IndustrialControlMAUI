using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameMergeOperationViewModel : FrameUseRecordOperationListViewModel
{
    public FrameMergeOperationViewModel(IMaterialFrameApi api) : base(api, "合框操作", "frame_closing")
    {
    }
}
