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
    public bool success { get; set; }
    public string? message { get; set; }
    public int code { get; set; }
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