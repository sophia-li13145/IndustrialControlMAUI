using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;
namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IEquipmentApi
    {
        Task<PageResponeResult<InspectionRecordDto>> PageQueryAsync(
         int pageNo,
         int pageSize,
         string? inspectNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? inspectStatus,
         bool searchCount,
         string? workstationCode = null,
         CancellationToken ct = default);

        Task<DictInspection> GetInspectionDictsAsync(CancellationToken ct = default);
        Task<ApiResp<InspectDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteInspectAttachmentAsync(string id, CancellationToken ct = default);
        //--------------------------------------------

        Task<PageResponeResult<MaintenanceRecordDto>> MainPageQueryAsync(
            int pageNo,
            int pageSize,
            string? upkeepNo,
            string? planUpkeepTimeBegin,
            string? planUpkeepTimeEnd,
            string? upkeepStatus,
            bool searchCount,
            CancellationToken ct = default);

        Task<DictMaintenance> GetMainDictsAsync(CancellationToken ct = default);
        Task<ApiResp<MaintenanceDetailDto>?> GetMainDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<MaintenanceWorkflowNode>>> GetMainWorkflowAsync(string id, CancellationToken ct = default);

        Task<PageResponeResult<RepairRecordDto>> RepairPageQueryAsync(
           int pageNo,
           int pageSize,
           string? workOrderNo,
           string? submitTimeBegin,
           string? submitTimeEnd,
           string? auditStatus,
           bool searchCount,
           CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteMainAttachmentAsync(string id, CancellationToken ct = default);
        Task<DictRepair> GetRepairDictsAsync(CancellationToken ct = default);
        Task<ApiResp<RepairDetailDto>?> GetRepairDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<RepairWorkflowNode>>> GetRepairWorkflowAsync(string id, CancellationToken ct = default);

        Task<ApiResp<bool?>> ExecuteSaveAsync(InspectDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(InspectDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteMainSaveAsync(MaintenanceDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteMainCompleteAsync(MaintenanceDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteRepairSaveAsync(RepairDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteRepairCompleteAsync(RepairDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteRepairAttachmentAsync(string id, CancellationToken ct = default);
        Task<PageResponeResult<MaintenanceReportDto>> ESPageQueryAsync(
            int pageNo,
         int pageSize,
         string? maintainNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? auditStatus,
         bool searchCount,
         CancellationToken ct = default);
        Task<DictExcept> GetExceptDictsAsync(CancellationToken ct = default);
        Task<ApiResp<MaintenanceReportDto>?> GetExceptDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<ExceptWorkflowNode>>> GetExceptWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteExceptSaveAsync(BuildExceptRequest payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> SubmitExceptAsync(string id, CancellationToken ct = default);
        Task<ApiResp<bool?>> BuildExceptAsync(BuildExceptRequest payload, CancellationToken ct = default);
    }
}
