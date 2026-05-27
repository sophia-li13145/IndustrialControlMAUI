using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class MaterialFrameDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

    public MaterialFrameDetailViewModel(IMaterialFrameApi api)
    {
        _api = api;
    }

    public ObservableCollection<MaterialFrameDetailLoadItemVm> LoadDetails { get; } = new();

    [ObservableProperty] private string frameNoDisplay = "-";
    [ObservableProperty] private string currentLocationDisplay = "未分配位置";
    [ObservableProperty] private string useStatusText = "空闲";
    [ObservableProperty] private string useStatusColor = "#22C55E";

    public int DetailCount => LoadDetails.Count;

    public void Apply(MaterialFrameQueryRecord? record)
    {
        LoadDetails.Clear();
        if (record == null)
        {
            OnPropertyChanged(nameof(DetailCount));
            return;
        }

        FrameNoDisplay = string.IsNullOrWhiteSpace(record.frameNo) ? "-" : record.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(record.currentLocation) ? "未分配位置" : record.currentLocation!;
        EnsureFrameStatusDictLoaded();
        UseStatusText = ResolveFrameStatusDisplay(record.frameStatus);
        UseStatusColor = ResolveFrameStatusColor(record.frameStatus);

        foreach (var detail in record.loadDetailList ?? new List<MaterialFrameQueryLoadDetail>())
            LoadDetails.Add(new MaterialFrameDetailLoadItemVm(detail));

        OnPropertyChanged(nameof(DetailCount));
    }

    private void EnsureFrameStatusDictLoaded()
    {
        if (_frameStatusDict.Count > 0) return;
        var fields = _api.GetStatusDictListAsync().GetAwaiter().GetResult();
        var statusField = fields?.FirstOrDefault(x => string.Equals(x.field, "frameStatus", StringComparison.OrdinalIgnoreCase));
        _frameStatusDict = statusField?.dictItems?
            .Where(x => !string.IsNullOrWhiteSpace(x.value))
            .GroupBy(x => x.value!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().name ?? g.First().value ?? g.Key, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }

    private static string ResolveFrameStatusColor(string? frameStatus)
    {
        var key = frameStatus?.Trim().ToLowerInvariant();
        return key switch
        {
            "warehouse" => "#22C55E",
            "disable" => "#9CA3AF",
            "damaged" => "#EF4444",
            _ => "#3B82F6"
        };
    }
}
