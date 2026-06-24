namespace IndustrialControlMAUI.Models;

public class SchedulePlanDetailResult
{
    public List<ScheduleDateItem> DateList { get; set; } = new();
    public string? PlanId { get; set; }
    public string? SelectedDate { get; set; }
    public List<ScheduleShiftItem> ShiftList { get; set; } = new();
    public string? Today { get; set; }
}

public class ScheduleDateItem
{
    public string? Date { get; set; }
    public int Day { get; set; }
    public bool IsSelected { get; set; }
    public bool IsToday { get; set; }
    public string? WeekName { get; set; }
}

public class ScheduleShiftItem
{
    public string? EndTime { get; set; }
    public string? ShiftCode { get; set; }
    public string? ShiftName { get; set; }
    public string? StartTime { get; set; }
    public List<ScheduleTeamItem> TeamList { get; set; } = new();
    public List<ScheduleTeamTag> TeamTags { get; set; } = new();
    public string? TimeRange { get; set; }
}

public class ScheduleTeamItem
{
    public string? EmptyText { get; set; }
    public string? Id { get; set; }
    public List<ScheduleMemberItem> MemberList { get; set; } = new();
    public string? TeamCode { get; set; }
    public string? TeamName { get; set; }
}

public class ScheduleMemberItem
{
    public string? DisplayName { get; set; }
    public bool HasWarning { get; set; }
    public bool IsLeader { get; set; }
    public string? PostTag { get; set; }
    public string? RealName { get; set; }
    public string? UserId { get; set; }
    public string? WarningText { get; set; }
}

public class ScheduleTeamTag
{
    public string? TeamCode { get; set; }
    public string? TeamName { get; set; }
}
