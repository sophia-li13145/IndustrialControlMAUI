using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameLoadOperationDetailViewModel : ObservableObject
{
    public ObservableCollection<FrameLoadTargetFrameItemVm> TargetFrames { get; } = new();

    [ObservableProperty] private string recordNoDisplay = "-";
    [ObservableProperty] private string operationTimeDisplay = "-";
    [ObservableProperty] private string operatorDisplay = "-";
    [ObservableProperty] private string materialNameDisplay = "-";

    public void Apply(FrameUseRecordOperation? record)
    {
        TargetFrames.Clear();
        if (record is null) return;

        RecordNoDisplay = string.IsNullOrWhiteSpace(record.recordNo) ? "-" : record.recordNo!;
        OperationTimeDisplay = string.IsNullOrWhiteSpace(record.operationTime) ? "-" : record.operationTime!;
        OperatorDisplay = string.IsNullOrWhiteSpace(record.@operator) ? "-" : record.@operator!;
        MaterialNameDisplay = string.IsNullOrWhiteSpace(record.materialName) ? "-" : record.materialName!;

        var grouped = (record.materialList ?? new List<FrameUseRecordMaterial>())
            .Where(x => !string.IsNullOrWhiteSpace(x.frameNo))
            .GroupBy(x => x.frameNo!)
            .Select((g, i) => new FrameLoadTargetFrameItemVm
            {
                Title = $"目标框 {i + 1}",
                FrameNoDisplay = g.Key,
                QtyDisplay = (g.Sum(x => x.qty ?? 0m)).ToString("0.##")
            })
            .ToList();

        if (grouped.Count == 0 && !string.IsNullOrWhiteSpace(record.targetLocation))
        {
            var targets = record.targetLocation.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < targets.Length; i++)
            {
                grouped.Add(new FrameLoadTargetFrameItemVm
                {
                    Title = $"目标框 {i + 1}",
                    FrameNoDisplay = targets[i],
                    QtyDisplay = "-"
                });
            }
        }

        foreach (var item in grouped) TargetFrames.Add(item);
    }
}

public class FrameLoadTargetFrameItemVm
{
    public string Title { get; set; } = "";
    public string FrameNoDisplay { get; set; } = "-";
    public string QtyDisplay { get; set; } = "-";
}
