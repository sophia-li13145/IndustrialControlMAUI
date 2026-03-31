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
    public string? schemeNo { get; set; }
    public string? platPlanNo { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal? curQty { get; set; }
    public List<ReworkProcessResourceDemand> planProcessRouteResourceDemandList { get; set; } = new();
    public List<PlanChildProductSchemeDetailEx> planChildProductSchemeDetailList { get; set; } = new();
}

public class ReworkProcessResourceDemand
{
    public string? id { get; set; }
    public string? schemeNo { get; set; }
    public string? routeCode { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public int? sortNumber { get; set; }
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

    public List<StatusOption> NeedSupplementOptions { get; } = new()
    {
        new StatusOption { Text = "是", Value = "1" },
        new StatusOption { Text = "否", Value = "0" }
    };

    [ObservableProperty]
    private StatusOption? selectedNeedSupplementOption;

    public decimal? standardQty { get; set; }
    public string? unit { get; set; }

    [ObservableProperty]
    private string? actualQtyText;

    partial void OnNeedSupplementChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedSupplementText));
        SelectedNeedSupplementOption = NeedSupplementOptions.FirstOrDefault(x => x.Value == (value ? "1" : "0"));
    }

    partial void OnSelectedNeedSupplementOptionChanged(StatusOption? value)
    {
        if (value == null) return;
        NeedSupplement = value.Value == "1";
    }
}

public class SaveReworkOrderReq
{
    public string defectTags { get; set; } = "";
    public bool hasReworkProcess { get; set; }
    public string? id { get; set; }
    public bool isFeedSupplement { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? memo { get; set; }
    public string? planNo { get; set; }
    public string? productionOrderNo { get; set; }
    public List<ReworkProcessSaveItem> reworkProcessList { get; set; } = new();
    public decimal? reworkQty { get; set; }
    public string? reworkType { get; set; }
    public string? reworkTypeName { get; set; }
    public bool submit { get; set; }
    public List<ReworkSupplementSaveItem> supplementList { get; set; } = new();
    public string? workOrderName { get; set; }
    public string? workOrderNo { get; set; }
}

public class ReworkProcessSaveItem
{
    public string? id { get; set; }
    public string? schemeNo { get; set; }
    public string? routeCode { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public int? sortNumber { get; set; }
}

public class ReworkSupplementSaveItem
{
    public decimal? actualReplenishmentQty { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public decimal? standardReplenishmentQty { get; set; }
    public string? unit { get; set; }
}
