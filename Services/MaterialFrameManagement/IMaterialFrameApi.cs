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

    Task<PageResp<BasMaterialRecord>?> PageBasMaterialsAsync(
        int pageNo = 1,
        int pageSize = 20,
        string? materialName = null,
        string? materialCode = null,
        CancellationToken ct = default);
}
