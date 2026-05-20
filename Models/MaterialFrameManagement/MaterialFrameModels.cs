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
    public string? materialName { get; set; }
    public string? productName { get; set; }
    public string? itemName { get; set; }
    public string? batchNo { get; set; }
    public string? lotNo { get; set; }
    public decimal? quantity { get; set; }
    public decimal? currentQty { get; set; }
    public decimal? currentQuantity { get; set; }
}
