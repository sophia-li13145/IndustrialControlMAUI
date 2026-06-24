using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface IScheduleApi
{
    Task<ApiResp<SchedulePlanDetailResult>> QuerySchedulePlanDetailAsync(DateTime selectedDate, CancellationToken ct = default);
}
