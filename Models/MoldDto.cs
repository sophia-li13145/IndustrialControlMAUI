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

