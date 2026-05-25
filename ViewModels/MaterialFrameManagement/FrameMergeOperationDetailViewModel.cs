using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameMergeOperationDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public FrameMergeOperationDetailViewModel(IMaterialFrameApi api) => _api = api;

    [ObservableProperty] private string recordNoDisplay = "-";
    [ObservableProperty] private string operationTimeDisplay = "-";
    [ObservableProperty] private string operatorDisplay = "-";
    [ObservableProperty] private string targetFrameNoDisplay = "-";

    public ObservableCollection<FrameUnloadFrameCardItemVm> SourceFrames { get; } = new();
    public ObservableCollection<FrameUnloadMaterialItemVm> TargetMaterials { get; } = new();

    public async Task LoadAsync(string? useRecordId)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(useRecordId)) return;
        var resp = await _api.GetFrameMergingDetailAsync(useRecordId.Trim());
        Apply(resp?.result);
    }

    private void Reset()
    {
        RecordNoDisplay = OperationTimeDisplay = OperatorDisplay = TargetFrameNoDisplay = "-";
        SourceFrames.Clear();
        TargetMaterials.Clear();
    }

    private void Apply(FrameUseRecordOperation? record)
    {
        if (record is null) return;
        RecordNoDisplay = string.IsNullOrWhiteSpace(record.recordNo) ? "-" : record.recordNo!;
        OperationTimeDisplay = string.IsNullOrWhiteSpace(record.operationTime) ? "-" : record.operationTime!;
        OperatorDisplay = string.IsNullOrWhiteSpace(record.@operator) ? "-" : record.@operator!;
        TargetFrameNoDisplay = string.IsNullOrWhiteSpace(record.targetFrameNo) ? (string.IsNullOrWhiteSpace(record.targetLocation) ? "-" : record.targetLocation!) : record.targetFrameNo!;

        var list = record.detailList ?? new List<FrameUseRecordDetail>();
        for (var i = 0; i < list.Count; i++)
        {
            var d = list[i];
            var first = (d.materialList ?? d.materials ?? new List<FrameUseRecordMaterial>()).FirstOrDefault();
            SourceFrames.Add(new FrameUnloadFrameCardItemVm
            {
                Title = $"原料框 {i + 1}",
                FrameNoDisplay = string.IsNullOrWhiteSpace(d.sourceFrameNo) ? "-" : d.sourceFrameNo!,
                MaterialNameDisplay = string.IsNullOrWhiteSpace(first?.materialName) ? "-" : first!.materialName!,
                BatchNoDisplay = string.IsNullOrWhiteSpace(first?.batchNo) ? "-" : first!.batchNo!,
                QtyDisplay = $"数量: {(first?.qty ?? 0m):0.##}"
            });
        }

        var target = list.SelectMany(x => x.materialList ?? x.materials ?? new List<FrameUseRecordMaterial>())
                         .GroupBy(x => new { x.materialName, x.batchNo })
                         .Select(g => new FrameUnloadMaterialItemVm
                         {
                             MaterialNameDisplay = string.IsNullOrWhiteSpace(g.Key.materialName) ? "-" : g.Key.materialName!,
                             BatchNoDisplay = string.IsNullOrWhiteSpace(g.Key.batchNo) ? "-" : g.Key.batchNo!,
                             QtyDisplay = $"数量: {g.Sum(x => x.qty ?? 0m):0.##}"
                         });
        foreach (var t in target) TargetMaterials.Add(t);
    }
}
