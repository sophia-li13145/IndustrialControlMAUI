using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public class FrameEmptyOperationViewModel : FrameUseRecordOperationListViewModel
{
    public FrameEmptyOperationViewModel(IMaterialFrameApi api) : base(api, "空框操作", "empty_frame")
    {
    }
}
