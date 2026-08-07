using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface IShiftHandoverApi
{
    Task<ApiResp<PendingShiftHandover>> GetPendingHandoverAsync(CancellationToken ct = default);
    Task<ApiResp<ShiftHandoverPageResult>> GetRecordsAsync(int pageNo, int pageSize, CancellationToken ct = default);
    Task<ApiResp<List<ShiftHandoverDictionary>>> GetDictListAsync(CancellationToken ct = default);
    Task<ApiResp<ShiftHandoverInfo>> GetHandoverInfoAsync(CancellationToken ct = default);
    Task<ApiResp<List<ReceiverTeamOption>>> GetReceiverTeamOptionsAsync(CancellationToken ct = default);
    Task<ApiResp<bool?>> AddAsync(AddShiftHandoverRequest request, CancellationToken ct = default);
    Task<ApiResp<ShiftHandoverDetail>> GetDetailAsync(string id, CancellationToken ct = default);
    Task<ApiResp<bool?>> ConfirmHandoverAsync(ConfirmShiftHandoverRequest request, CancellationToken ct = default);
}
