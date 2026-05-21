using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameLoadOperationPage : MaterialFrameQueryPage
{
    public FrameLoadOperationPage(FrameLoadOperationViewModel vm) : base(vm)
    {
    }

    protected override async Task HandleAddRecordAsync()
    {
        await Shell.Current.GoToAsync(nameof(FrameLoadAddPage));
    }
}
