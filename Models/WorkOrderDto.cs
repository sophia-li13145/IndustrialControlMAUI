using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Models;

// Models/WorkOrderDto.cs  （文件路径随你工程）
public class WorkOrderDto
{
    public string Id { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string OrderName { get; set; } = "";

    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string LineName { get; set; } = "";

    /// <summary>中文状态：待执行 / 执行中 / 入库中 / 已完成</summary>
    public string Status { get; set; } = "";

    /// <summary>创建时间（已格式化字符串）</summary>
    public string CreateDate { get; set; } = "";

    public string Urgent { get; set; } = "";
    public int? CurQty { get; set; }
    public string? BomCode { get; set; }
    public string? RouteName { get; set; }
    public string? WorkShopName { get; set; }
    public string? PlanStartText { get; set; }
}
public enum TaskRunState { NotStarted, Running, Paused, Finished }
public class WorkOrderSummary
{
    public string OrderNo { get; set; } = "";
    public string OrderName { get; set; } = "";
    public string Status { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime CreateDate { get; set; }
}

// 服务层 DTO（已把数量转成 int，便于前端直接用）
public record InboundPendingRow(
    string? Barcode,
    string? DetailId,
    string? Location,
    string? MaterialName,
    int PendingQty,   // instockQty
    int ScannedQty,   // qty
    string? Spec);

public record InboundScannedRow(
    string Barcode,
    string DetailId,
    string Location,
    string MaterialName,
    int Qty,
    string Spec,
    bool ScanStatus,
    string? WarehouseCode
    );




public record OutboundPendingRow(
    string? MaterialName,
    string? MaterialCode,
    string? Spec,
    string? Location,
    string? ProductionBatch,
    string? StockBatch,
    int OutstockQty,
    int Qty
);



public record OutboundScannedRow(
    string Barcode,
    string DetailId,
    string Location,
    string MaterialName,
    int Qty,
    int OutstockQty,
    string Spec,
    bool ScanStatus,
    string? WarehouseCode
    );

public class ApiResp<T>
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public T? result { get; set; }
}

public class FieldDict
{
    public string? field { get; set; }
    public List<DictItem> dictItems { get; set; } = new();
}

public  class DictItem
{
    public string? dictItemValue { get; set; }
    public string? dictItemName { get; set; }
}
public class WorkOrderQuery
{
    public int PageNo { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? AuditStatus { get; set; }
    public DateTime? CreatedTimeStart { get; set; }
    public DateTime? CreatedTimeEnd { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? MaterialName { get; set; }
}

public class WorkOrderPageResult
{
    public int code { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public WorkOrderPageData? result { get; set; }
    public long costTime { get; set; }
}

public class WorkOrderPageData
{
    public WorkOrderPageList? list { get; set; }
    public int pageNo { get; set; }
    public int pageSize { get; set; }
    public long total { get; set; }
    public List<WorkOrderRecord> records { get; set; } = new();
}

public class WorkOrderPageList
{
    public int pageNo { get; set; }
    public int pageSize { get; set; }
    public long total { get; set; }
    public List<WorkOrderRecord> records { get; set; } = new();
}

public class WorkOrderRecord
{
    public string? id { get; set; }
    public string? workOrderNo { get; set; }
    public string? workOrderName { get; set; }
    public string? auditStatus { get; set; }
    public decimal? curQty { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? line { get; set; }
    public string? lineName { get; set; }
    public string? workShop { get; set; }
    public string? workShopName { get; set; }
    public string? urgent { get; set; }
    public string? schemeStartDate { get; set; }
    public string? schemeEndDate { get; set; }
    public string? createdTime { get; set; }
    public string? modifiedTime { get; set; }
    public string? commitedTime { get; set; }
    public string? bomCode { get; set; }
    public string? routeName { get; set; }

}

public class DictResponse
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int code { get; set; }
    public List<DictField>? result { get; set; }
    public long costTime { get; set; }
}

public class DictField
{
    public string? field { get; set; }
    public List<DictItem> dictItems { get; set; } = new();
}


public class DictBundle
{
    public List<DictItem> AuditStatus { get; set; } = new();
    public List<DictItem> Urgent { get; set; } = new();
}

public sealed class WorkflowResp
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int code { get; set; }
    public List<WorkflowItem>? result { get; set; }
}

public sealed class WorkflowItem
{
    public string? statusValue { get; set; }
    public string? statusName { get; set; }
    public string? statusTime { get; set; }
}

public sealed class PageResp<T>
{
    public bool? success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public PageResult<T>? result { get; set; }
}

public sealed class PageResult<T>
{
    public int pageNo { get; set; }
    public int pageSize { get; set; }
    public int total { get; set; }
    public List<T>? records { get; set; }
}

public class ProcessTask
{
    public string? Id { get; set; }
    public string? ProcessCode { get; set; }
    public string? ProcessName { get; set; }
    public string? MaterialName { get; set; }
    public decimal? ScheQty { get; set; }
    public decimal? CompletedQty { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }

    public string? CreatedTime { get; set; }
    public int? SortNumber { get; set; }
    public string? WorkOrderNo { get; set; }

    public string? WorkOrderName { get; set; }

    [JsonPropertyName("WorkOrderAuditStatus")]
    public string? WorkOrderAuditStatus { get; set; }

    [JsonPropertyName("AuditStatus")]
    public string? AuditStatus { get; set; } // 接口返回的原始值
    [JsonIgnore] // 不参与序列化
    public string? AuditStatusName { get; set; } // 页面绑定用
}

public class MaterialAuRecord
{
    public string? CreatedTime { get; set; }
    public string? Creator { get; set; }
    public bool DelStatus { get; set; }
    public string? FactoryCode { get; set; }
    public string? Id { get; set; }
    public string? MaterialClassName { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public string? MaterialTypeName { get; set; }
    public string? Memo { get; set; }
    public string? ModifiedTime { get; set; }
    public string? Modifier { get; set; }
    public string? OperateTime { get; set; }
    public string? PlatPlanNo { get; set; }
    public string? ProcessCode { get; set; }
    public string? ProcessName { get; set; }
    public decimal Qty { get; set; }
    public string? RawMaterialProductionDate { get; set; }
    public string? SchemeNo { get; set; }
    public string? Unit { get; set; }
    public string? WorkOrderNo { get; set; }
}

public class OutputAuRecord
{
    public string? CreatedTime { get; set; }
    public string? Creator { get; set; }
    public bool DelStatus { get; set; }
    public string? FactoryCode { get; set; }
    public string? Id { get; set; }
    public string? MaterialClassName { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public string? MaterialTypeName { get; set; }
    public string? Memo { get; set; }
    public string? ModifiedTime { get; set; }
    public string? Modifier { get; set; }
    public string? OperateTime { get; set; }
    public string? PlatPlanNo { get; set; }
    public string? ProcessCode { get; set; }
    public string? ProcessName { get; set; }
    public decimal Qty { get; set; }
    public string? RawMaterialProductionDate { get; set; }
    public string? SchemeNo { get; set; }
    public string? Unit { get; set; }
    public string? WorkOrderNo { get; set; }
}
public class StatusOption
{
    public string Text { get; set; } = "";     // 显示：dictItemName
    public string? Value { get; set; }         // 参数：dictItemValue（"0"/"1"...，全部用 null）
    public override string ToString() => Text; // 某些平台用 ToString() 展示
}

public  class ProcessInfo
{
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public string? status { get; set; }
}

public class ShiftInfo
{
    public string? workshopsName { get; set; }
    public string? workshopsCode { get; set; }
}

public class DevicesInfo
{
    public string? deviceName { get; set; }
    public string? deviceCode { get; set; }
}
public class WorkProcessTaskDetail
{
    public string? id { get; set; }
    public string? workOrderNo { get; set; }
    public string? materialName { get; set; }
    public string? workOrderName { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public decimal? scheQty { get; set; }
    public decimal? completedQty { get; set; }
    public string? auditStatus { get; set; }
    public string? teamCode { get; set; }
    public string? teamName { get; set; }
    public decimal? taskReportedQty { get; set; }
    public string? productionMachine { get; set; }
    public string? productionMachineName { get; set; }
    public string? workShop { get; set; }
    public string?  factoryCode { get; set; }
    public string? line { get; set; }

    public string? periodExecute { get; set; }

    public string? schemeNo { get; set; }

    public string? platPlanNo { get; set; }

    public List<TaskMaterialInput> materialInputList { get; set; } = new();
    public List<TaskMaterialOutput> materialOutputList { get; set; } = new();
    public List<RouteResourceDemand> planProcessRouteResourceDemandList { get; set; } = new();

    // 页面显示
    [System.Text.Json.Serialization.JsonIgnore] public string? AuditStatusName { get; set; }
}

public class TaskMaterialInput
{
    public string? id { get; set; }
    public string? materialClassName { get; set; }
    public string? materialName { get; set; }
    public string? materialCode { get; set; }
    public string? materialTypeName { get; set; }
    
    public string? unit { get; set; }
    public decimal? qty { get; set; }
    public decimal? hasInputQty { get; set; }
    public int? hasInputCount { get; set; }
    public string? memo { get; set; }
}

public class TaskMaterialOutput
{
    public string? id { get; set; }
    public string? materialClassName { get; set; }
    public string? materialName { get; set; }
    public string? materialCode { get; set; }
    public string? materialTypeName { get; set; }
    public string? unit { get; set; }
    public decimal? qty { get; set; }
    public decimal? hasOutputQty { get; set; }
    public int? hasOutputCount { get; set; }
    public string? memo { get; set; }
}

public class RouteResourceDemand
{
    public string? id { get; set; }
    public string? resourceType { get; set; } // dev/mold...
    public string? model { get; set; }
    public decimal? outputQty { get; set; }
    public decimal? demandQty { get; set; }
    public string? memo { get; set; }
}


public sealed class MaterialIO
{
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? unit { get; set; }
    public decimal? qty { get; set; }
    public decimal? hasInputQty { get; set; }   // 输入/输出都有，展示时自行用
    public decimal? hasOutputQty { get; set; }
    public int? hasInputCount { get; set; }
    public int? hasOutputCount { get; set; }
    public string? memo { get; set; }
    public string? createdTime { get; set; }
}
public enum DetailTab { Input, Output }
public class WorkProcessTaskTeamUpdateReq
{
    public string id { get; set; } = "";      
    public string? productionMachine { get; set; }     
    public string? productionMachineName { get; set; }
    public int? taskReportedQty { get; set; }
    public string? teamCode { get; set; }
    public string? teamName { get; set; }
    public int? workHours { get; set; }
    public string? startDate { get; set; }
    public string? endDate { get; set; }
}
public class WorkProcessTaskDeviceUpdateReq
{
    public string workOrderNo { get; set; } = "";      
    public string? deviceCode { get; set; }     
    public string? factoryCode { get; set; }
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? schemeNo { get; set; }
}

public class AddWorkProcessTaskMaterialInputReq
{
    public string? materialClassName { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? materialTypeName { get; set; }
    public string? memo { get; set; }
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public double qty { get; set; }
    public string? rawMaterialProductionDate { get; set; } // "yyyy-MM-dd HH:mm:ss"
    public string? schemeNo { get; set; }
    public string? unit { get; set; }
    public string workOrderNo { get; set; } = "";
    public string operationTime { get; set; }
    
}


// “投料记录”表格的数据行
public class MaterialInputRecord
{
    public int Index { get; set; }
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public double Qty { get; set; }
    public DateTime? RawMaterialProductionDate { get; set; }
    public DateTime? OperationDate { get; set; }
    public string? Memo { get; set; }
}

public class MaterialInputItem
{
    public int index { get; set; }
    public string materialName { get; set; } = string.Empty;
    public string unit { get; set; } = string.Empty;
    public double shouldQty { get; set; }
    public double actualQty { get; set; }
    public int inputCount { get; set; }

    // 下面这些可能在字典里取，此处保留位
    public string? materialCode { get; set; }
    public string? materialClassName { get; set; }
    public string? materialTypeName { get; set; }
}

// 产出记录行
public class OutputRecord
{
    public int Index { get; set; }
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public double Qty { get; set; }
    public DateTime? RawMaterialProductionDate { get; set; }
    public DateTime? OperationDate { get; set; }
    public string? Memo { get; set; }
}


public class AddWorkProcessTaskProductOutputReq
{
    public string? materialClassName { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? materialTypeName { get; set; }
    public string? memo { get; set; }
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public double qty { get; set; }
    public string? rawMaterialProductionDate { get; set; } // "yyyy-MM-dd HH:mm:ss"
    public string? schemeNo { get; set; }
    public string? unit { get; set; }
    public string workOrderNo { get; set; } = "";
    public string operationTime { get; set; }
}

public class OutputPlanItem
{
    public int index { get; set; }
    public string materialName { get; set; } = "";
    public string unit { get; set; } = "";
    public double shouldQty { get; set; }
    public double actualQty { get; set; }
    public int outputCount { get; set; }

    public string? materialCode { get; set; }
    public string? materialClassName { get; set; }
    public string? materialTypeName { get; set; }
}

public class OutputPopupResult
{
    public string materialClassName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string materialTypeName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Memo { get; set; }
    public string? Unit { get; set; }
    public DateTime? OperationTime { get; set; }
}

public class MaterialInputResult
{
    public string materialClassName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string materialTypeName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Memo { get; set; }
    public string? Unit { get; set; }
    public DateTime? OperationTime { get; set; }
}

public class DeleteWorkProcessTaskMaterialInputReq
{
    public string? id { get; set; }
}

public class EditWorkProcessTaskMaterialInputReq
{
    public string? id { get; set; }                       // 主键
    public string? memo { get; set; }                     // 备注
    public decimal? qty { get; set; }                     // 数量
    public string? rawMaterialProductionDate { get; set; } // 原料生产日期（"yyyy-MM-dd" 或 "yyyy-MM-dd HH:mm:ss"）
}
public class WorkOrderDomainResp
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int code { get; set; }
    public WorkOrderDomainResult? result { get; set; }
}

public class WorkOrderDomainResult
{
    public string? id { get; set; }
    public List<PlanChildProductSchemeDetail> planChildProductSchemeDetailList { get; set; } = new();
}

public class PlanChildProductSchemeDetail
{
    public string? id { get; set; }
    public string? schemeNo { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public PlanProcessRoute? planProcessRoute { get; set; }
}

public class PlanProcessRoute
{
    public string? routeCode { get; set; }
    public string? routeName { get; set; }
    public List<RouteDetail> routeDetailList { get; set; } = new();
}

public class RouteDetail
{
    public string? id { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public int? sortNumber { get; set; }
}
/// <summary>
/// 库存明细记录（对应 /pda/wmsInstock/pageQuery 返回的 records）
/// </summary>
public class InventoryRecord
{
    public int index { get; set; }
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }

    public string? spec { get; set; }
    public string? model { get; set; }

    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? location { get; set; }

    public string? stockBatch { get; set; }
    public string? productionBatch { get; set; }

    public string? supplierCode { get; set; }
    public string? supplierName { get; set; }
    public string? manufacturerName { get; set; }

    public string? productionDate { get; set; } // 或 DateTime?，看后端
    public int? shelfLife { get; set; }         // ★ 必须可空
    public string? shelfLifeUnit { get; set; }

    public decimal instockQty { get; set; }
    public decimal pendingQuantity { get; set; }
    public decimal availableQuantity { get; set; }

    public decimal? safeStock { get; set; }     // ★ 必须可空

    public string? unit { get; set; }

    public bool isDeliverInspect { get; set; }
    public string? inspectResult { get; set; }

    public string? dataBelong { get; set; }
    public string? consignor { get; set; }
}

public class StockCheckOrderItem
{
    public string? id { get; set; }
    public string? checkNo { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? auditStatus { get; set; }
    public string? createdTime { get; set; }
    public string? modifiedTime { get; set; }
    public string? memo { get; set; }
    public string? creator { get; set; }

    // UI 用的状态名称
    public string AuditStatusText =>
        auditStatus switch
        {
            "0" => "待执行",
            "1" => "执行中",
            "2" => "已完成",
            _ => "未知"
        };
}
/// <summary>
/// 库存盘点明细
/// 对应 /pda/wmsInstockCheck/pageQueryDetails 的 records
/// </summary>
public partial class StockCheckDetailItem : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public string? id { get; set; }

    public string? checkNo { get; set; }

    public string? location { get; set; }

    public string? stockBatch { get; set; }

    public string? materialCode { get; set; }

    public string? materialName { get; set; }

    public string? spec { get; set; }

    public string? model { get; set; }

    public decimal? instockQty { get; set; }

    public decimal? checkQty { get; set; }

    public decimal? profitLossQty { get; set; }

    public string? unit { get; set; }

    public string? memo { get; set; }

    /// <summary>列表序号（前端自己编号）</summary>
    public int index { get; set; }

    /// <summary>是否有差异，用于卡片变绿</summary>
    public bool HasDiff => profitLossQty != 0;
    public string? dataBelong { get; set; }   // 后端字段，有就映射上
    public string? headerId { get; set; }     // 如果后端返回主表 id，可用这个；没有也没关系
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? productionBatch { get; set; }
    public string? productionDate { get; set; }
    public int? shelfLife { get; set; }
    public string? shelfLifeUnit { get; set; }
    public string? supplierCode { get; set; }
    public string? supplierName { get; set; }
    public string? materialClass { get; set; }
    public string? materialClassName { get; set; }
    public string? materialType { get; set; }
    public string? materialTypeName { get; set; }
    public string? manufacturerName { get; set; }
    public string? consignor { get; set; }
    public string? inspectResult { get; set; }
    public bool? isDeliverInspect { get; set; }


}

/// <summary>
/// 盘点保存请求体（对应 /pda/wmsInstockCheck/edit）
/// </summary>
public class StockCheckEditReq
{
    /// <summary>盘点单主表 id</summary>
    public string? id { get; set; }

    /// <summary>主表备注（可不填）</summary>
    public string? memo { get; set; }

    /// <summary>保存/提交 标记（具体值按后端约定，可为 null）</summary>
    public string? saveOrHand { get; set; }

    /// <summary>明细列表</summary>
    public List<StockCheckEditDetailReq> wmsInstockCheckDetailList { get; set; } = new();
}

/// <summary>
/// 盘点明细编辑项
/// </summary>
public class StockCheckEditDetailReq
{
    /// <summary>明细 id（必填）</summary>
    public string? id { get; set; }

    /// <summary>盘点数量</summary>
    public decimal? checkQty { get; set; }

    /// <summary>盈亏数量 = 盘点数量 - 账存数量</summary>
    public decimal? profitLossQty { get; set; }

    /// <summary>数据归属（如果后端有要求就带上，没要求可以为 null）</summary>
    public string? dataBelong { get; set; }

    /// <summary>明细备注</summary>
    public string? memo { get; set; }
}

/// <summary>
/// 灵活盘点结存请求体（/pda/wmsInstockCheck/add）
/// </summary>
public class FlexibleStockCheckAddReq
{
    public string? memo { get; set; }
    public string? saveOrHand { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }

    public List<FlexibleStockCheckAddDetailReq> wmsInstockCheckDetailList { get; set; }
        = new();
}

/// <summary>
/// 灵活盘点结存明细
/// 字段基本照接口示例来，模型里没有的字段可以删掉
/// </summary>
public class FlexibleStockCheckAddDetailReq
{
    public decimal? checkQty { get; set; }
    public string? consignor { get; set; }
    public string? dataBelong { get; set; }
    public string? inspectResult { get; set; }
    public decimal? instockQty { get; set; }
    public bool? isDeliverInspect { get; set; }
    public string? location { get; set; }
    public string? manufacturerName { get; set; }
    public string? materialClass { get; set; }
    public string? materialClassName { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? materialType { get; set; }
    public string? materialTypeName { get; set; }
    public string? memo { get; set; }
    public string? model { get; set; }
    public string? productionBatch { get; set; }
    public string? productionDate { get; set; }
    public decimal? profitLossQty { get; set; }
    public int? shelfLife { get; set; }
    public string? shelfLifeUnit { get; set; }
    public string? spec { get; set; }
    public string? stockBatch { get; set; }
    public string? supplierCode { get; set; }
    public string? supplierName { get; set; }
    public string? unit { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
}