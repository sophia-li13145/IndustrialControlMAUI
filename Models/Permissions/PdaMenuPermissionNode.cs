using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Models.Permissions;

public sealed class PdaMenuPermissionNode
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("parentMenuId")] public string? ParentMenuId { get; set; }
    [JsonPropertyName("menuCode")] public string? MenuCode { get; set; }
    [JsonPropertyName("menuName")] public string? MenuName { get; set; }
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("children")] public List<PdaMenuPermissionNode> Children { get; set; } = [];
}

public sealed class PdaPermissionResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("result")] public List<PdaMenuPermissionNode> Result { get; set; } = [];
}

public static class PdaMenuCodes
{
    public const string ScheduleManagement = "pda_sms";
    public const string DutyRoster = "pda_duty";
    public const string ShiftHandover = "pda_handover";
    public const string WarehouseManagement = "pda_wms";
    public const string MaterialInbound = "pda_material_instock";
}
