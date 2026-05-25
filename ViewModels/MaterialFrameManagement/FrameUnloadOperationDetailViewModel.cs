using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameUnloadOperationDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public FrameUnloadOperationDetailViewModel(IMaterialFrameApi api)
    {
        _api = api;
    }

    public ObservableCollection<FrameUnloadFrameCardItemVm> TargetFrames { get; } = new();
    public ObservableCollection<FrameUnloadMaterialItemVm> SourceMaterials { get; } = new();

    [ObservableProperty] private string recordNoDisplay = "-";
    [ObservableProperty] private string operationTimeDisplay = "-";
    [ObservableProperty] private string operatorDisplay = "-";
    [ObservableProperty] private string sourceFrameNoDisplay = "-";

    public async Task LoadAsync(string? recordId)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(recordId)) return;

        var resp = await _api.GetUnloadRecordDetailAsync(recordId.Trim());
        Apply(resp?.result);
    }

    private void Reset()
    {
        TargetFrames.Clear();
        SourceMaterials.Clear();
        RecordNoDisplay = "-";
        OperationTimeDisplay = "-";
        OperatorDisplay = "-";
        SourceFrameNoDisplay = "-";
    }

    private void Apply(FrameUseRecordOperation? record)
    {
        if (record is null) return;

        RecordNoDisplay = string.IsNullOrWhiteSpace(record.recordNo) ? "-" : record.recordNo!;
        OperationTimeDisplay = string.IsNullOrWhiteSpace(record.operationTime) ? "-" : record.operationTime!;
        OperatorDisplay = string.IsNullOrWhiteSpace(record.@operator) ? "-" : record.@operator!;

        var sourceFrame = (record.sourceFrame ?? new FrameUseRecordDetail());
        SourceFrameNoDisplay = string.IsNullOrWhiteSpace(sourceFrame.frameNo) ? "-" : sourceFrame.frameNo!;

        foreach (var mat in sourceFrame.materials ?? new List<FrameUseRecordMaterial>())
        {
            SourceMaterials.Add(new FrameUnloadMaterialItemVm
            {
                MaterialNameDisplay = string.IsNullOrWhiteSpace(mat.materialName) ? "-" : mat.materialName!,
                BatchNoDisplay = string.IsNullOrWhiteSpace(mat.batchNo) ? "-" : mat.batchNo!,
                QtyDisplay = $"数量: {(mat.qty ?? 0m):0.##}"
            });
        }

        var targets = (record.targetFrames ?? new List<FrameUseRecordDetail>())
            .Where(x => !string.IsNullOrWhiteSpace(x.frameNo))
            .ToList();

        for (var i = 0; i < targets.Count; i++)
        {
            var frame = targets[i];
            var firstMat = frame.materials?.FirstOrDefault();
            TargetFrames.Add(new FrameUnloadFrameCardItemVm
            {
                Title = $"目标框 {i + 1}",
                FrameNoDisplay = frame.frameNo!,
                MaterialNameDisplay = string.IsNullOrWhiteSpace(firstMat?.materialName) ? "-" : firstMat.materialName!,
                BatchNoDisplay = string.IsNullOrWhiteSpace(firstMat?.batchNo) ? "-" : firstMat.batchNo!,
                QtyDisplay = $"数量: {(firstMat?.qty ?? 0m):0.##}"
            });
        }
    }
}

public class FrameUnloadFrameCardItemVm
{
    public string Title { get; set; } = "";
    public string FrameNoDisplay { get; set; } = "-";
    public string MaterialNameDisplay { get; set; } = "-";
    public string BatchNoDisplay { get; set; } = "-";
    public string QtyDisplay { get; set; } = "-";
}

public class FrameUnloadMaterialItemVm
{
    public string MaterialNameDisplay { get; set; } = "-";
    public string BatchNoDisplay { get; set; } = "-";
    public string QtyDisplay { get; set; } = "-";
}
