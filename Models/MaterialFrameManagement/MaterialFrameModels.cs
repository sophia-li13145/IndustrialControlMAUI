using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialControlMAUI.Models;

public class MaterialFrameRecord
{
    public string? currentLocation { get; set; }
    public FrameInfoLite? frameInfo { get; set; }
    public string? frameNo { get; set; }
    public bool? fullLoadStatus { get; set; }
    public List<MaterialFrameLoadDetail>? loadDetailList { get; set; }
}

public class FrameInfoLite
{
    public int? useStatus { get; set; }
}

public class MaterialFrameLoadDetail
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? productName { get; set; }
    public string? itemName { get; set; }
    public string? batchNo { get; set; }
    public string? lotNo { get; set; }
    public decimal? quantity { get; set; }
    public decimal? currentQty { get; set; }
    public decimal? currentQuantity { get; set; }
}

public partial class FrameStatusItem : ObservableObject
{
    public decimal? availableQty { get; set; }
    public decimal? currentLoadQty { get; set; }
    public string? frameInfoId { get; set; }
    public string? frameName { get; set; }
    public string? frameNo { get; set; }
    public string? frameStatus { get; set; }
    public string? frameTypeCode { get; set; }
    public string? frameTypeName { get; set; }
    public bool? fullLoadStatus { get; set; }
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public List<FrameStatusMaterialItem>? materials { get; set; }
    public decimal? maxLimit { get; set; }
    public decimal? minLimit { get; set; }
    [ObservableProperty] public bool isSelected;
    public List<MaterialFrameLoadDetail>? loadDetailList { get; set; }
    public string StatusAndCountDisplay => $"状态: {frameStatus ?? "-"}   物料: {(loadDetailList?.Count ?? 0)}种";
}


public class FrameStatusMaterialItem
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal? minLimit { get; set; }
    public decimal? maxLimit { get; set; }
    public decimal? currentLoadQty { get; set; }
    public decimal? availableQty { get; set; }
}
