namespace IndustrialControlMAUI.Models;

public class MaterialFrameRecord
{
    public string? currentLocation { get; set; }
    public FrameInfoLite? frameInfo { get; set; }
    public string? frameNo { get; set; }
    public bool? fullLoadStatus { get; set; }
}

public class FrameInfoLite
{
    public int? useStatus { get; set; }
}
