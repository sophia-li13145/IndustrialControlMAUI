using AndroidX.Core.Location;
using IndustrialControlMAUI.Services;
using System.Text.Json;

namespace IndustrialControlMAUI.Models;

// Models/MoldDto.cs  （文件路径随你工程）
public class MoldDto
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

public class MoldSummary
{
    public string OrderNo { get; set; } = "";
    public string OrderName { get; set; } = "";
    public string Status { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime CreateDate { get; set; }
}
public class WorkOrderMoldView
{
    public string WorkOrderNo { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public List<MoldModelView> Models { get; set; } = new();
    public List<ScannedItemView> Scanned { get; set; } = new();
}

public class ScannedItemView
{
    public string MoldCode { get; set; } = "";
    public string MoldModel { get; set; } = "";
    public string? Location { get; set; }
    public bool IzOutStock { get; set; }           // 是否已出库
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public string? OutstockDate { get; set; }
}

public class MoldGroupVm
{
    public string ModelCode { get; set; } = "";
    public int BaseQty { get; set; }
    public List<MoldItemVm> Items { get; set; } = new();
}

public sealed class MoldItemVm
{
    public int Index { get; set; }
    public string MoldNumber { get; set; } = "";
}

// ===================== 接口返回模型（按接口文档截图） =====================

public  class QueryForWorkOrderResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public WorkOrderMoldDetail? result { get; set; }
}

public  class WorkOrderMoldDetail
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }                 // 用于“产品名称”
    public string? workOrderNo { get; set; }

    public List<PdaBasMoldInfoDTO> pdaBasMoldInfoDTOS { get; set; } = new();
    public List<PlanProcessRouteResourceDemandDTO> planProcessRouteResourceDemandDTOS { get; set; } = new();
}

/// <summary>左侧：模具型号 + 基础需求数量</summary>
// 已扫描/已出库记录 DTO（来自 result.pdaBasMoldInfoDTOS）
public class PdaBasMoldInfoDTO
{
    /// <summary>是否已出库（false=未出库，true=已出库）</summary>
    public bool izOutStock { get; set; }

    /// <summary>库位</summary>
    public string? location { get; set; }

    /// <summary>模具编码</summary>
    public string? moldCode { get; set; }

    /// <summary>模具型号</summary>
    public string? moldModel { get; set; }

    /// <summary>出库日期（原样字符串返回）</summary>
    public string? outstockDate { get; set; }

    /// <summary>使用状态（0=未使用 / 1=使用中；有些环境返回布尔，故用可空 bool 更稳）</summary>
    public bool? usageStatus { get; set; }

    /// <summary>仓库编码</summary>
    public string? warehouseCode { get; set; }

    /// <summary>仓库名称</summary>
    public string? warehouseName { get; set; }

    /// <summary>当前使用中的工单号</summary>
    public string? workOrderNo { get; set; }
}

public class PlanProcessRouteResourceDemandDTO
{
    public string? model { get; set; }
    public decimal? demandQty { get; set; }   // 若后端是 decimal/long，请相应调整
    public List<PlanProcessRouteResourceAllocationDTO>? planProcessRouteResourceAllocationDTOS { get; set; }
}


/// <summary>右侧：资源清单（设备/模具）</summary>
public class PlanProcessRouteResourceAllocationDTO
{
    public string? model { get; set; }         // 型号（分组依据）
    public string? code { get; set; }  // 设备/模具编号（显示用）
    public string? resourceType { get; set; }  // dev-设备；mold-模具（如需筛选可用）
}

public class OutMoldScannedItem
{
    public bool IsSelected { get; set; }
    public int Index { get; set; }
    public string Barcode { get; set; } = "";
    public string MoldCode { get; set; } = "";
    public string MoldModel { get; set; } = "";
    public int OutQty { get; set; } = 1;
    public string Location { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string WarehouseCode { get; set; } = "";
}
public sealed class MoldOutScanQueryResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public MoldOutScanQueryData? result { get; set; }
}

public sealed class MoldOutScanQueryData
{
    public bool? izOutStock { get; set; }
    public string? location { get; set; }
    public string? moldCode { get; set; }
    public string? moldModel { get; set; }
    public string? outstockDate { get; set; }
    public bool? usageStatus { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? workOrderNo { get; set; }
}

public class MoldOutConfirmReq
{
    public string? memo { get; set; }              // 备注
    public string? @operator { get; set; }         // 操作人
    public string? orderType { get; set; }         // 可留空或按后端字典
    public string? orderTypeName { get; set; }     // 可留空
    public string? outstockDate { get; set; }      // 建议: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    public List<MoldOutDetail> wmsMaterialOutstockDetailList { get; set; } = new();
    public string? workOrderNo { get; set; }       // 工单号
}

public  class MoldOutDetail
{
    public string? location { get; set; }              // 出库库位
    public string? materialCode { get; set; }          // 对于模具，可用 moldCode 代填
    public string? materialName { get; set; }          // 对于模具，可用 moldModel 代填
    public string? model { get; set; }                 // 模具型号（moldModel）
    public int outstockQty { get; set; }               // 出库数量
    public string? outstockWarehouse { get; set; }     // 出库仓库名（若有）
    public string? outstockWarehouseCode { get; set; } // 出库仓库编码（若有）
}
// —— DTO：按返回示例定义，全部 string? 更宽容 —— //
public class LocationResp
{
    public bool success { get; set; }
    public int code { get; set; }
    public string? message { get; set; }
    public List<LocationGroup>? result { get; set; }
    public int? costTime { get; set; }
}

public class LocationGroup
{
    public string? zone { get; set; }

    // 二维数组：每个 zone 下有多组层数据；每组里是 LocationLiteDto 列表
    public List<LocationLiteDto>[]? layerData { get; set; }
}
public class LocationSegment
{
    // 该段公共的 Zone 名（来自 result[].zone），可为空
    public string Zone { get; init; } = "";
    // 该段内的库位条目
    public List<LocationItem> Items { get; init; } = new();
}
public class LocationLiteDto
{
    public string? id { get; set; }
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? zone { get; set; }
    public string? zoneName { get; set; }
    public string? rack { get; set; }
    public string? rackName { get; set; }
    public string? layer { get; set; }
    public string? layerName { get; set; }
    public string? location { get; set; }
    public string? inventoryStatus { get; set; }  // 可能为 null
    public string? status { get; set; }
    public string? memo { get; set; }
    public bool? delStatus { get; set; }
    public string? creator { get; set; }
    public string? createdTime { get; set; }      // 按字符串接收，避免反序列化异常
    public string? modifier { get; set; }
    public string? modifiedTime { get; set; }
}
// 注意：layerData 用 JsonElement 接收，避免类型不匹配
public class LocationGroupRaw
{
    public string? zone { get; set; }
    public JsonElement? layerData { get; set; }
}

public class WarehouseWrap
{
    public bool success { get; set; }
    public string? message { get; set; }
    public List<WarehouseItem>? result { get; set; }
}

// ===================== 请求模型 =====================
public class MoldQuery
{
    public int PageNo { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    // 0 待执行；1 执行中；2 入库中；3 已完成
    public string? AuditStatus { get; set; }

    public DateTime? CreatedTimeStart { get; set; }
    public DateTime? CreatedTimeEnd { get; set; }

    public string? MoldNo { get; set; }
    public string? MaterialName { get; set; }
}

// ===================== 返回模型（分页/流程/扫描等） =====================
// 这里保留你原有的模型定义（节选），其余维持不变

public class MoldScanQueryResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public MoldScanQueryData? result { get; set; }
}

public class MoldScanQueryData
{
    public string? location { get; set; }
    public string? moldCode { get; set; }
    public string? moldModel { get; set; }
    public string? outstockDate { get; set; }
    public bool? usageStatus { get; set; }
    public string? workOrderNo { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
}

public sealed class InStockConfirmReq
{
    public string? instockDate { get; set; }
    public string? memo { get; set; }
    public string? @operator { get; set; }
    public string? orderType { get; set; }
    public string? orderTypeName { get; set; }
    public List<InStockDetail> wmsMaterialInstockDetailList { get; set; } = new();
    public string? workOrderNo { get; set; }
}

public sealed class InStockDetail
{
    public int instockQty { get; set; }
    public string? instockWarehouse { get; set; }
    public string? instockWarehouseCode { get; set; }
    public string? location { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
}



public class MoldModelView
{
    public string ModelCode { get; set; } = "";
    public int BaseQty { get; set; }
    public List<string> MoldNumbers { get; set; } = new();
}

public class QueryForWorkOrderData
{
    public string? workOrderNo { get; set; }
    public string? materialName { get; set; }

    public List<PdaBasMoldInfoDTO>? pdaBasMoldInfoDTOS { get; set; } = new();
    public List<PlanProcessRouteResourceAllocationDTO>? planProcessRouteResourceAllocationDTOS { get; set; } = new();
}
