using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(FrameNo), "frameNo")]
public partial class MaterialFrameDetailPage : ContentPage
{
    private readonly MaterialFrameDetailViewModel _vm;

    public string? FrameNo
    {
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (MaterialFrameNavigationStore.TryTake(value!, out var record))
                _vm.Apply(record);
        }
    }

    public MaterialFrameDetailPage(MaterialFrameDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}

public static class MaterialFrameNavigationStore
{
    private static readonly Dictionary<string, MaterialFrameRecord> Cache = new();

    public static string Put(MaterialFrameRecord record)
    {
        var key = Guid.NewGuid().ToString("N");
        Cache[key] = record;
        return key;
    }

    public static bool TryTake(string key, out MaterialFrameRecord? record)
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
