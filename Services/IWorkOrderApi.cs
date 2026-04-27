using IndustrialControlMAUI.Services.Common;
using GoogleGson;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using Org.Apache.Http.Authentication;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuthState = IndustrialControlMAUI.Tools.AuthState;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IWorkOrderApi
    {
        Task<WorkOrderPageResult> GetWorkOrdersAsync(WorkOrderQuery q, CancellationToken ct = default);
        Task<DictBundle> GetWorkOrderDictsAsync(CancellationToken ct = default);
        Task<WorkflowResp?> GetWorkOrderWorkflowAsync(string id, CancellationToken ct = default);
        Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(
         string? workOrderNo,
         IEnumerable<string>? auditStatusList,   // ★ 改为数组
         string? processCode,
         DateTime? createdTimeStart = null,
         DateTime? createdTimeEnd = null,
         string? materialName = null,
         string? platPlanNo = null,
         string? schemeNo = null,
         bool? searchCount = null,      // 是否计算总记录数（可选）
         int pageNo = 1,
         int pageSize = 50,
         CancellationToken ct = default);
        Task<ApiResp<List<FieldDict>>> GetWorkProcessTaskDictListAsync(CancellationToken ct = default);
        Task<ApiResp<List<ProcessInfo>>> GetProcessInfoListAsync(CancellationToken ct = default);
        Task<ApiResp<WorkProcessTaskDetail>> GetWorkProcessTaskDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<ShiftInfo>>> GetShiftOptionsAsync(
        string factoryCode,
        string workshopsCode,
        CancellationToken ct = default);
        Task<ApiResp<List<DevicesInfo>>> GetDeviceOptionsAsync(
        string factoryCode,
        string processCode,
        string? line = null,
        CancellationToken ct = default);
        Task<SimpleOk> UpdateWorkProcessTaskAsync(
            string id, string? productionMachine, string? productionMachineName, int? taskReportedQty, string? teamCode, string? teamName, int? workHours, string? startDate, string? endDate, CancellationToken ct = default);
        Task<ApiResp<bool>> StartWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> CompleteWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> PauseWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> AddWorkProcessTaskMaterialInputAsync(AddWorkProcessTaskMaterialInputReq req);

        Task<ApiResp<bool>> AddWorkProcessTaskProductOutputAsync(AddWorkProcessTaskProductOutputReq req);

        Task<PageResp<MaterialAuRecord>?> PageWorkProcessTaskMaterialInputs(
                string factoryCode,          // 工厂编码（必填）
                string processCode,          // 工序编码（必填）
                string workOrderNo,          // 工单号（必填）
                int pageNo = 1,              // 当前页（必填）
                int pageSize = 50,           // 每页条数（必填）
                string? materialCode = null, // 物料编码（可选）
                bool? searchCount = null,    // 是否计算总记录数（可选）
                CancellationToken ct = default);

        Task<PageResp<OutputAuRecord>?> PageWorkProcessTaskOutputs(
                string factoryCode,          // 工厂编码（必填）
                string processCode,          // 工序编码（必填）
                string workOrderNo,          // 工单号（必填）
                int pageNo = 1,              // 当前页（必填）
                int pageSize = 50,           // 每页条数（必填）
                string? materialCode = null, // 物料编码（可选）
                bool? searchCount = null,    // 是否计算总记录数（可选）
                CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteWorkProcessTaskMaterialInputAsync(
    string id,
    CancellationToken ct = default);

        Task<ApiResp<bool>> DeleteWorkProcessTaskOutputAsync(
   string id,
   CancellationToken ct = default);
        Task<ApiResp<bool>> EditWorkProcessTaskMaterialInputAsync(
            string id,
            decimal? qty = null,
            string? memo = null,
            string? rawMaterialProductionDate = null,
            CancellationToken ct = default);

        Task<ApiResp<List<WorkOrderDeviceBindItem>>> GetWorkOrderDeviceListAsync(
            string factoryCode,
            string processCode,
            string schemeNo,
            string workOrderNo,
            CancellationToken ct = default);

        Task<ApiResp<bool>> BindWorkOrderDeviceAsync(
            BindWorkOrderDeviceReq req,
            CancellationToken ct = default);

        Task<ApiResp<bool?>> EditWorkOrderDeviceBindTimeAsync(
            EditWorkOrderDeviceBindTimeReq req,
            CancellationToken ct = default);

        Task<ApiResp<bool?>> UnbindWorkOrderDeviceAsync(
            UnbindWorkOrderDeviceReq req,
            CancellationToken ct = default);

        Task<WorkOrderDomainResp?> GetWorkOrderDomainAsync(string id, CancellationToken ct = default);
        Task<ApiResp<DeviceMoldInfoResultDto>?> GetDeviceMoldInfoAsync(
            string deviceCode,
            CancellationToken ct = default);
        Task<ApiResp<DeviceMoldRelationDto>?> GetMoldByMoldCodeAsync(
            string moldCode,
            CancellationToken ct = default);
        Task<ApiResp<bool>?> AddDeviceMoldRelationAsync(
            AddDeviceMoldRelationReq req,
            CancellationToken ct = default);
        Task<ApiResp<DeviceMoldRelationDto>?> GetDeviceMoldRelationAsync(
            string id,
            CancellationToken ct = default);
        Task<ApiResp<bool>?> ConfirmUnloadMoldAsync(
            string id,
            CancellationToken ct = default);

        Task<ApiResp<List<FieldDict>>> GetReworkDictListAsync(CancellationToken ct = default);
        Task<ReworkOrderDomainResp?> GetReworkWorkOrderDomainAsync(string workOrderNo, CancellationToken ct = default);
        Task<ApiResp<List<ReworkBomDetailFlattenItem>>> GetReworkBomFlattenDetailsAsync(string workOrderNo, CancellationToken ct = default);
        Task<ApiResp<bool?>> SaveReworkOrderAsync(SaveReworkOrderReq req, CancellationToken ct = default);
        Task<ApiResp<bool?>> SaveAndSubmitReworkOrderAsync(SaveReworkOrderReq req, CancellationToken ct = default);
        Task<PageResp<InventoryRecord>?> PageInventoryAsync(
    string? barcode,          // 库位或者物料条码
    int pageNo = 1,           // 当前页
    int pageSize = 50,        // 页大小
    bool? searchCount = null, // 是否计算总数
    CancellationToken ct = default);

        Task<PageResp<StockCheckOrderItem>?> PageStockCheckOrdersAsync(
    string? checkNo,
    DateTime? beginDate = null,
    DateTime? endDate = null,
    bool? searchCount = null,
    int pageNo = 1,
    int pageSize = 50,
    CancellationToken ct = default);

        Task<PageResp<StockCheckDetailItem>?> PageStockCheckDetailsAsync(
    string checkNo,
    string? location,
    string? materialBarcode,
    bool? searchCount = null,
    int pageNo = 1,
    int pageSize = 50,
    CancellationToken ct = default);

        Task<SimpleOk> EditStockCheckAsync(
    StockCheckEditReq req,
    CancellationToken ct = default);
        Task<SimpleOk> AddFlexibleStockCheckAsync(
    FlexibleStockCheckAddReq req,
    CancellationToken ct = default);


    }
}
