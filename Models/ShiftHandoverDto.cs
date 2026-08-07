using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Models;

public class PendingShiftHandover
{
    public string? HandoverDate { get; set; }
    public string? HandoverTeamName { get; set; }
    public string? HandoverUserName { get; set; }
    public string? ReceiverTeamName { get; set; }
    public string? Id { get; set; }
    public string? Memo { get; set; }
}

public class ShiftHandoverInfo
{
    public string? HandoverDate { get; set; }
    public string? HandoverUserName { get; set; }
    public string? TeamName { get; set; }
}

public class ReceiverTeamOption
{
    public string? TeamCode { get; set; }
    public string? TeamName { get; set; }
}

public class AddShiftHandoverRequest
{
    [JsonPropertyName("memo")]
    public string? Memo { get; set; }

    [JsonPropertyName("receiverTeamCode")]
    public string ReceiverTeamCode { get; set; } = string.Empty;

    [JsonPropertyName("receiverTeamName")]
    public string? ReceiverTeamName { get; set; }
}

public class ShiftHandoverDetail
{
    public string? HandoverShiftName { get; set; }
    public int HandoverStatus { get; set; }
    public string? HandoverTeamName { get; set; }
    public string? HandoverUserName { get; set; }
    public string? Id { get; set; }
    public string? Memo { get; set; }
    public string? ReceiverShiftName { get; set; }
    public string? ReceiverTeamName { get; set; }
    public string? ReceiverUserName { get; set; }
    public string? RecordTime { get; set; }
}

public class ConfirmShiftHandoverRequest
{
    [JsonPropertyName("handoverId")]
    public string HandoverId { get; set; } = string.Empty;
}

public class ShiftHandoverPageResult
{
    public long PageNo { get; set; }
    public long PageSize { get; set; }
    public List<ShiftHandoverRecord> Records { get; set; } = new();
    public long Total { get; set; }
}

public class ShiftHandoverDictionary
{
    public string? Field { get; set; }
    public List<ShiftHandoverDictionaryItem> DictItems { get; set; } = new();
}

public class ShiftHandoverDictionaryItem
{
    public string? DictItemValue { get; set; }
    public string? DictItemName { get; set; }
}

public class ShiftHandoverRecord
{
    public string? CreatedTime { get; set; }
    public int HandoverStatus { get; set; }
    public string? HandoverTeamCode { get; set; }
    public string? HandoverTeamName { get; set; }
    public string? HandoverUserName { get; set; }
    public string? Id { get; set; }
    public string? Memo { get; set; }
    public string? ReceiverTeamCode { get; set; }
    public string? ReceiverTeamName { get; set; }
    public string? ReceiverUserName { get; set; }
}
