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
    string Spec,
    bool ScanStatus,
    string? WarehouseCode
    );