using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

public class MaterialFrameItemVm
{
    public MaterialFrameRecord Source { get; }

    public MaterialFrameItemVm(MaterialFrameRecord r)
    {
        Source = r;
        FrameNoDisplay = string.IsNullOrWhiteSpace(r.frameNo) ? "-" : r.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(r.currentLocation) ? "未分配位置" : r.currentLocation!;
        var use = r.frameInfo?.useStatus ?? 0;
        UseStatusText = use == 1 ? "占用" : "空闲";
        UseStatusColor = use == 1 ? "#EF4444" : "#22C55E";
        var full = r.fullLoadStatus == true;
        FullLoadStatusText = full ? "已满载" : "未满载";
        FullLoadStatusColor = full ? "#F97316" : "#9CA3AF";
    }

    public string FrameNoDisplay { get; }
    public string CurrentLocationDisplay { get; }
    public string UseStatusText { get; }
    public string UseStatusColor { get; }
    public string FullLoadStatusText { get; }
    public string FullLoadStatusColor { get; }
}
