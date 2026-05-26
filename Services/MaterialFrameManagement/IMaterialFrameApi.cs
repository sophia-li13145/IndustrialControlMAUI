using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface IMaterialFrameApi
{
    Task<PageResp<MaterialFrameRecord>?> PageMaterialFrameInfoAsync(
        int pageNo = 1,
        int pageSize = 10,
        string? operationType = null,
        string? frameNo = null,
        CancellationToken ct = default);

    Task<PageResp<MaterialFrameRecord>?> PageMaterialFrameOperationAsync(
        int pageNo = 1,
        int pageSize = 10,
        string operationType = "framing",
        string? frameNo = null,
        CancellationToken ct = default);


    Task<PageResp<FrameUseRecordOperation>?> PageFrameUseRecordPageAsync(
        int pageNo = 1,
        int pageSize = 10,
        string operationType = "framing",
        string? materialName = null,
        string? recordNo = null,
        CancellationToken ct = default);
    Task<ObjResp<FrameUseRecordOperation>?> GetLoadingRecordDetailAsync(
        string id,
        CancellationToken ct = default);

    Task<ObjResp<FrameUseRecordOperation>?> GetUnloadRecordDetailAsync(
        string recordId,
        CancellationToken ct = default);

    Task<ObjResp<FrameUseRecordOperation>?> GetPouringRecordDetailAsync(
        string recordId,
        CancellationToken ct = default);

    Task<ObjResp<FrameUseRecordOperation>?> GetFrameMergingDetailAsync(
        string useRecordId,
        CancellationToken ct = default);

    Task<ObjResp<FrameUseRecordOperation>?> GetFrameReturnDetailAsync(
        string useRecordId,
        CancellationToken ct = default);

    Task<PageResp<BasMaterialRecord>?> PageBasMaterialsAsync(
        int pageNo = 1,
        int pageSize = 20,
        string? materialName = null,
        string? materialCode = null,
        CancellationToken ct = default);

    Task<ListResp<FrameStatusItem>?> GetFrameStatusListAsync(
        string materialCode,
        string materialName,
        CancellationToken ct = default);
    Task<ListResp<FrameStatusItem>?> GetFrameStatusListByFrameNoAsync(
        string frameNo,
        string materialCode,
        CancellationToken ct = default);
    Task<ListResp<FrameStatusItem>?> GetMaterialFrameListAsync(
        string? frameNo = null,
        CancellationToken ct = default);

    Task<ListResp<FrameStatusItem>?> GetFrameStatusListForUnloadAsync(
        List<string> materialCodes,
        List<string> materialNames,
        CancellationToken ct = default);

    Task<BoolResp?> AddUnloadingRecordAsync(
        AddUnloadingRecordReq req,
        CancellationToken ct = default);

    Task<List<DictField>?> GetStatusDictListAsync(CancellationToken ct = default);

    Task<BoolResp?> AddLoadingRecordAsync(
        AddLoadingRecordReq req,
        CancellationToken ct = default);
}
