using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialControlMAUI.Models;

public partial class FrameLoadAddTargetFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public bool? fullLoadStatus { get; set; }
    [ObservableProperty] public bool isSelected;
}

public class FrameMergeAddLoadDetailItem
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? batchNo { get; set; }
    public string? unit { get; set; }
    public decimal? quantity { get; set; }
    public decimal? currentQty { get; set; }
    public decimal? currentQuantity { get; set; }
}

public partial class FrameMergeAddFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public List<FrameMergeAddLoadDetailItem>? loadDetailList { get; set; }
    [ObservableProperty] public bool isSelected;
}

public partial class FrameMergeAddTargetFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    [ObservableProperty] public bool isSelected;
}

public class FramePourAddLoadDetailItem
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
}

public partial class FramePourAddSourceFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public List<FramePourAddLoadDetailItem>? loadDetailList { get; set; }
    [ObservableProperty] public bool isSelected;
}

public partial class FramePourAddTargetFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    [ObservableProperty] public bool isSelected;
}

public partial class FrameEmptyAddFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    [ObservableProperty] public bool isSelected;
}
