using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialControlMAUI.Models;

public class ReworkOrderDomainResp
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public ReworkOrderDomain? result { get; set; }
}

public class ReworkOrderDomain
{
    public string? id { get; set; }
    public string? workOrderNo { get; set; }
    public string? workOrderName { get; set; }
    public string? materialName { get; set; }
    public decimal? curQty { get; set; }
    public List<PlanChildProductSchemeDetailEx> planChildProductSchemeDetailList { get; set; } = new();
}

public class PlanChildProductSchemeDetailEx
{
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public PlanBomEx? planBom { get; set; }
    public PlanProcessRouteEx? planProcessRoute { get; set; }
}

public class PlanBomEx
{
    public string? bomCode { get; set; }
    public List<PlanBomDetailEx> bomDetailList { get; set; } = new();
}

public class PlanBomDetailEx
{
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public bool? needCollect { get; set; }
    public decimal? qty { get; set; }
    public string? unit { get; set; }
}

public class PlanProcessRouteEx
{
    public string? routeCode { get; set; }
    public string? routeName { get; set; }
    public List<RouteDetailEx> routeDetailList { get; set; } = new();
}

public class RouteDetailEx
{
    public string? id { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public int? sortNumber { get; set; }
}

public partial class ReworkMaterialRow : ObservableObject
{
    public int Sequence { get; set; }
    public string? id { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }

    [ObservableProperty]
    private bool needSupplement;
    public string NeedSupplementText => NeedSupplement ? "是" : "否";

    public decimal? standardQty { get; set; }

    [ObservableProperty]
    private string? actualQtyText;

    partial void OnNeedSupplementChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedSupplementText));
    }
}
