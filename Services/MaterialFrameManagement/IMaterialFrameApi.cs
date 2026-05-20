using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface IMaterialFrameApi
{
    Task<PageResp<MaterialFrameRecord>?> PageMaterialFrameInfoAsync(
        int pageNo = 1,
        int pageSize = 10,
        string? frameNo = null,
        CancellationToken ct = default);
}
