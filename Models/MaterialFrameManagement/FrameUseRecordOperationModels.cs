using IndustrialControlMAUI.ViewModels;

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
    public int? sourceFrameCount { get; set; }
    public string? targetFrameNo { get; set; }
    public FrameUseRecordDetail? sourceFrame { get; set; }
    public List<FrameUseRecordDetail>? targetFrames { get; set; }
    public FrameUseRecordDetail? targetFrame { get; set; }

    public string BizNoDisplay => string.IsNullOrWhiteSpace(recordNo) ? "-" : recordNo!;
    public string MaterialNameDisplay => string.IsNullOrWhiteSpace(materialName) ? "-" : materialName!;
    public string TargetLocationDisplay => string.IsNullOrWhiteSpace(targetLocation) ? "-" : targetLocation!;
    public string FrameCountDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(targetLocation))
            {
                var targetCount = targetLocation.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
                if (targetCount > 0)
                {
                    return targetCount.ToString();
                }
            }

            return (frameCount ?? 0).ToString();
        }
    }
    public string OperationTimeDisplay => string.IsNullOrWhiteSpace(operationTime) ? "-" : operationTime!;
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
    public string? sourceFrameNo { get; set; }
    public List<FrameUseRecordMaterial>? materialList { get; set; }
    public List<FrameUseRecordMaterial>? materials { get; set; }
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

public sealed class ListResp<T>
{
    public bool? success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public List<T>? result { get; set; }
}

public sealed class ObjResp<T>
{
    public bool? success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public T? result { get; set; }
}

public sealed class BoolResp
{
    public int? code { get; set; }
    public long? costTime { get; set; }
    public string? message { get; set; }
    public bool? result { get; set; }
    public bool? success { get; set; }
}

public sealed class AddLoadingRecordReq
{
    public List<TargetFrameSelectableItem>? frameStatusList { get; set; }
    public List<AddLoadingDetail>? loadDetailList { get; set; }
    public AddLoadingMaterial? material { get; set; }
}

public sealed class AddLoadingDetail
{
    public string? frameNo { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal qty { get; set; }
}

public sealed class AddLoadingMaterial
{
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? unit { get; set; }
}

public sealed class AddUnloadingRecordReq
{
    public string? sourceFrameId { get; set; }
    public string? sourceFrameNo { get; set; }
    public string? sourceFrameTypeCode { get; set; }
    public string? sourceFrameTypeName { get; set; }
    public List<AddUnloadingMaterial>? unloadMaterials { get; set; }
}

public sealed class AddUnloadingMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal sourceQty { get; set; }
    public List<AddUnloadingDetail>? unloadDetailList { get; set; }
}

public sealed class AddUnloadingDetail
{
    public string? targetFrameId { get; set; }
    public string? targetFrameNo { get; set; }
    public string? targetFrameTypeCode { get; set; }
    public string? targetFrameTypeName { get; set; }
    public decimal unloadQty { get; set; }
}
