using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

public class MaterialFrameItemVm
{
    public MaterialFrameQueryRecord Source { get; }

    public MaterialFrameItemVm(MaterialFrameQueryRecord r, string frameStatusDisplay)
    {
        Source = r;
        FrameNoDisplay = string.IsNullOrWhiteSpace(r.frameNo) ? "-" : r.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(r.currentLocation) ? "未分配位置" : r.currentLocation!;
        UseStatusText = string.IsNullOrWhiteSpace(frameStatusDisplay) ? "-" : frameStatusDisplay;
        UseStatusColor = ResolveFrameStatusColor(r.frameStatus);
        var full = r.fullLoadStatus == true;
        FullLoadStatusText = full ? "已满载" : "未满载";
        FullLoadStatusColor = full ? "#F97316" : "#9CA3AF";
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

    public string FrameNoDisplay { get; }
    public string CurrentLocationDisplay { get; }
    public string UseStatusText { get; }
    public string UseStatusColor { get; }
    public string FullLoadStatusText { get; }
    public string FullLoadStatusColor { get; }
}
