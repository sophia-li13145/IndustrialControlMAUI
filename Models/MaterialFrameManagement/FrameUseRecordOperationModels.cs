namespace IndustrialControlMAUI.Models;

public class FrameUseRecordOperation
{
    public string? bizNo { get; set; }
    public string? bizType { get; set; }
    public List<FrameUseRecordDetail>? detailList { get; set; }
    public int? frameCount { get; set; }
    public string? id { get; set; }
    public List<FrameUseRecordMaterial>? materialList { get; set; }
    public string? materialName { get; set; }
    public string? memo { get; set; }
    public string? operationTime { get; set; }
    public string? operationType { get; set; }
    public string? @operator { get; set; }
    public string? recordNo { get; set; }
    public string? sourceLocation { get; set; }
    public string? targetLocation { get; set; }
    public decimal? totalQty { get; set; }
}

public class FrameUseRecordDetail
{
    public bool? afterFullLoadStatus { get; set; }
    public string? afterLocation { get; set; }
    public string? afterStatus { get; set; }
    public bool? beforeFullLoadStatus { get; set; }
    public string? beforeLocation { get; set; }
    public string? beforeStatus { get; set; }
    public string? frameNo { get; set; }
    public string? frameRole { get; set; }
}

public class FrameUseRecordMaterial
{
    public string? batchNo { get; set; }
    public string? frameNo { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal? qty { get; set; }
    public string? unit { get; set; }
}
