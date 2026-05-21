using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public class FrameLoadOperationPage : MaterialFrameQueryPage
{
    public FrameLoadOperationPage(MaterialFrameQueryViewModel vm) : base(vm)
    {
        ConfigureAsOperationPage("装框", "framing");
    }

    protected override async Task HandleAddRecordAsync()
    {
        await Shell.Current.GoToAsync(nameof(FrameLoadAddPage));
    }
}
