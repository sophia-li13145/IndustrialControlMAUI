using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(RecordKey), "recordKey")]
public partial class FrameEmptyOperationDetailPage : ContentPage
{
    private readonly FrameEmptyOperationDetailViewModel _vm;

    public string? RecordKey
    {
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var key = Uri.UnescapeDataString(value);
            if (FrameLoadOperationNavigationStore.TryTake(key, out var recordId))
                MainThread.BeginInvokeOnMainThread(async () => await _vm.LoadAsync(recordId));
        }
    }

    public FrameEmptyOperationDetailPage(FrameEmptyOperationDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}
