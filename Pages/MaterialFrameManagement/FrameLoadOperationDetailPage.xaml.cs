using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(RecordKey), "recordKey")]
public partial class FrameLoadOperationDetailPage : ContentPage
{
    private readonly FrameLoadOperationDetailViewModel _vm;

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

    public FrameLoadOperationDetailPage(FrameLoadOperationDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}

public static class FrameLoadOperationNavigationStore
{
    private static readonly Dictionary<string, string?> Cache = new();

    public static string Put(string? recordId)
    {
        var key = Guid.NewGuid().ToString("N");
        Cache[key] = recordId;
        return key;
    }

    public static bool TryTake(string key, out string? recordId)
    {
        if (Cache.TryGetValue(key, out var foundId))
        {
            Cache.Remove(key);
            recordId = foundId;
            return true;
        }

        recordId = null;
        return false;
    }
}
