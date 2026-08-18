namespace IndustrialControlMAUI.Models;

public sealed class TodoTaskItem
{
    public string? id { get; set; }
    public string? taskType { get; set; }
    public string? taskTypeName { get; set; }
    public string? taskNo { get; set; }
    public string? taskName { get; set; }
    public string? status { get; set; }
    public string? statusName { get; set; }
    public string? createdTime { get; set; }

    public Color StatusColor => taskType?.ToUpperInvariant() switch
    {
        "PROCESS" => Color.FromArgb("#F28C28"),
        "INSPECTION" => Color.FromArgb("#3478F6"),
        "REPAIR" when status == "2" => Color.FromArgb("#F28C28"),
        "REPAIR" => Color.FromArgb("#3478F6"),
        _ => Color.FromArgb("#687386")
    };

    public Color StatusBackgroundColor => taskType?.ToUpperInvariant() switch
    {
        "PROCESS" => Color.FromArgb("#FFF4E8"),
        "INSPECTION" => Color.FromArgb("#EEF5FF"),
        "REPAIR" when status == "2" => Color.FromArgb("#FFF4E8"),
        "REPAIR" => Color.FromArgb("#EEF5FF"),
        _ => Color.FromArgb("#F1F3F6")
    };
}
