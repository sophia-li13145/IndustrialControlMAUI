using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IQualityApi
    {
        Task<PageResponeResult<QualityRecordDto>> PageQueryAsync(
         int pageNo,
         int pageSize,
         string? qualityNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? inspectStatus,
         string? qualityType,
         bool searchCount,
         CancellationToken ct = default);
        Task<DictQuality> GetQualityDictsAsync(CancellationToken ct = default);
        Task<ApiResp<QualityDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default);
        
        Task<ApiResp<bool?>> ExecuteSaveAsync(QualityDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(QualityDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<DefectPage>> GetDefectPageAsync(
    int pageNo, int pageSize,
    string? defectCode = null,
    string? defectName = null,
    string? levelCode = null,
    string? status = null,
    bool? searchCount = null,
    string? createdTimeBegin = null,
    string? createdTimeEnd = null,
    CancellationToken ct = default);

        Task<ApiResp<bool>> DeleteAttachmentAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectDeviceOption>>?> GetInspectDevicesAsync(CancellationToken ct = default);
        Task<ApiResp<List<InspectParamOption>>?> GetInspectParamsAsync(string deviceCode, CancellationToken ct = default);
        Task<ApiResp<bool?>> CheckQcItemLimitAsync(
            string deviceCode,
            string paramCode,
            string qsOrderItemId,
            string? collectTimeBegin,
            string? collectTimeEnd,
            CancellationToken ct = default);
        Task<PageResponeResult<InspectionDetailRecord>?> GetInspectionDetailPageAsync(
            string deviceCode,
            string paramCode,
            string? collectTimeBegin,
            string? collectTimeEnd,
            int pageNo,
            int pageSize,
            bool? searchCount,
            CancellationToken ct = default);
    }
}
