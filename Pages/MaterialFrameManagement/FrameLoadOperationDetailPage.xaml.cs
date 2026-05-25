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
            if (FrameLoadOperationNavigationStore.TryTake(key, out var record))
                _vm.Apply(record);
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
    private static readonly Dictionary<string, FrameUseRecordOperation> Cache = new();

    public static string Put(FrameUseRecordOperation record)
    {
        var key = Guid.NewGuid().ToString("N");
        Cache[key] = record;
        return key;
    }

    public static bool TryTake(string key, out FrameUseRecordOperation? record)
    {
        if (Cache.TryGetValue(key, out var found))
        {
            Cache.Remove(key);
            record = found;
            return true;
        }

        record = null;
        return false;
    }
}
