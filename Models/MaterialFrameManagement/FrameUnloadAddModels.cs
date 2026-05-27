using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialControlMAUI.Models;

public class FrameUnloadAddLoadDetailItem
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? productName { get; set; }
    public string? itemName { get; set; }
    public string? batchNo { get; set; }
    public decimal? qty { get; set; }
    public string? unit { get; set; }
}

public partial class FrameUnloadAddSourceFrameItem : ObservableObject
{
    public decimal? availableQty { get; set; }
    public decimal? currentLoadQty { get; set; }
    public string? frameInfoId { get; set; }
    public string? frameName { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public bool? fullLoadStatus { get; set; }
    public string? id { get; set; }
    public List<FrameUnloadAddLoadDetailItem>? loadDetailList { get; set; }
    [ObservableProperty] public bool isSelected;
    public string FrameStatusDisplayText => (string.IsNullOrWhiteSpace(frameStatusDisplay) ? frameStatus : frameStatusDisplay) ?? "-";
    public string MaterialTypeCountDisplay => $"物料: {(loadDetailList?.Count ?? 0)}种";
    public string StatusAndCountDisplay => $"状态: {FrameStatusDisplayText}   {MaterialTypeCountDisplay}";
}

public partial class FrameUnloadAddTargetFrameItem : ObservableObject
{
    public string? id { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameStatusDisplay { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    [ObservableProperty] public bool isSelected;
}
