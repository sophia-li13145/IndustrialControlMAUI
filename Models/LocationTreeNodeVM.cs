// 修改点：给节点增加 CatalogueType / WarehouseCode / LayerCode，Level 支持到 3(货架层)
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Models;

public partial class LocationTreeNodeVM : ObservableObject
{
    public LocationTreeNodeVM(string name, int level, string path,
        string? catalogueType = null, string? warehouseCode = null, string? layerCode = null)
    {
        Name = name;
        Level = level;
        Path = path;
        CatalogueType = catalogueType ?? "";
        WarehouseCode = warehouseCode ?? "";
        LayerCode = layerCode ?? "";
        Indent = new Thickness(level * 16, 0, 0, 0);
    }

    [ObservableProperty] private string name;
    [ObservableProperty] private bool isExpanded;

    public int Level { get; }                  // 0 仓库 / 1 区域 / 2 货架 / 3 货架层
    public string Path { get; }                // 例：仓库/区域/货架/层
    public string CatalogueType { get; }       // warehouse/area/storage_rack/storage_rack_layer
    public string WarehouseCode { get; }       // FFLK
    public string LayerCode { get; }           // A1-1（用于 pageLocationQuery 的 layer 参数）
    public Thickness Indent { get; }
    public ObservableCollection<LocationTreeNodeVM> Children { get; } = new();

    public string ToggleIcon => Children.Count == 0 ? "" : (IsExpanded ? "▾" : "▸");
}

// ↓ DTO 保持不变
public class LocationNodeDto
{
    public string? id { get; set; }
    public string? warehouseCode { get; set; }
    public string? warehouseName { get; set; }
    public string? parentCode { get; set; }
    public string? parentName { get; set; }
    public string? catalogueCode { get; set; }
    public string? catalogueName { get; set; }
    public string? catalogueType { get; set; }       // warehouse/area/storage_rack/storage_rack_layer
    public string? catalogueTypeName { get; set; }
    public string? location { get; set; }
    public List<LocationNodeDto> children { get; set; } = new();
}

public class BinInfo
{
    public string Id { get; set; } = string.Empty;

    public string FactoryCode { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;

    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;

    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;

    public string RackCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;

    public string LayerCode { get; set; } = string.Empty;
    public string LayerName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 库存状态：true=instock, false=unstocked
    /// </summary>
    public bool InStock { get; set; }

    public string InventoryStatus { get; set; } = string.Empty;

    public int Status { get; set; }   // 0=正常,1=启用/其它

    public string Memo { get; set; } = string.Empty;

    public bool DelStatus { get; set; }

    public string Creator { get; set; } = string.Empty;
    public DateTime? CreatedTime { get; set; }

    public string Modifier { get; set; } = string.Empty;
    public DateTime? ModifiedTime { get; set; }
}

public class InStockLocationResp
{
    public bool? success { get; set; }
    public int? code { get; set; }
    public string? message { get; set; }
    public LocationNodeDto? result { get; set; } // 注意：是单个对象，不是 List
}
public class PageLocationResp
{
    public bool? success { get; set; }
    public int? code { get; set; }
    public string? message { get; set; }
    public PageLocationResult? result { get; set; }
    public int? costTime { get; set; }
}

public class PageLocationResult
{
    public int? pageNo { get; set; }
    public int? pageSize { get; set; }
    public int? total { get; set; }
    public List<PageLocationRecord>? records { get; set; }
}

public class PageLocationRecord
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

    // "instock" / "unstocked"
    public string? inventoryStatus { get; set; }

    // 返回里是 "1"（字符串），先用 string? 接；若你想要 int?，请确保 JsonOptions 允许字符串数字
    public string? status { get; set; }

    public string? memo { get; set; }

    public bool? delStatus { get; set; }

    public string? creator { get; set; }
    public string? createdTime { get; set; }
    public string? modifier { get; set; }
    public string? modifiedTime { get; set; }
}