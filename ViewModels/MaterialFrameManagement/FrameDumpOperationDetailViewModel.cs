using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameDumpOperationDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public FrameDumpOperationDetailViewModel(IMaterialFrameApi api)
    {
        _api = api;
    }

    [ObservableProperty] private string recordNoDisplay = "-";
    [ObservableProperty] private string operationTimeDisplay = "-";
    [ObservableProperty] private string operatorDisplay = "-";
    [ObservableProperty] private string sourceFrameNoDisplay = "-";
    [ObservableProperty] private string targetFrameNoDisplay = "-";
    public ObservableCollection<FrameUnloadMaterialItemVm> SourceMaterials { get; } = new();
    public ObservableCollection<FrameUnloadMaterialItemVm> TargetMaterials { get; } = new();

    public async Task LoadAsync(string? recordId)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(recordId)) return;

        var resp = await _api.GetPouringRecordDetailAsync(recordId.Trim());
        Apply(resp?.result);
    }

    private void Reset()
    {
        RecordNoDisplay = "-";
        OperationTimeDisplay = "-";
        OperatorDisplay = "-";
        SourceFrameNoDisplay = "-";
        TargetFrameNoDisplay = "-";
        SourceMaterials.Clear();
        TargetMaterials.Clear();
    }

    private void Apply(FrameUseRecordOperation? record)
    {
        if (record is null) return;
        RecordNoDisplay = string.IsNullOrWhiteSpace(record.recordNo) ? "-" : record.recordNo!;
        OperationTimeDisplay = string.IsNullOrWhiteSpace(record.operationTime) ? "-" : record.operationTime!;
        OperatorDisplay = string.IsNullOrWhiteSpace(record.@operator) ? "-" : record.@operator!;

        var source = record.sourceFrame ?? new FrameUseRecordDetail();
        var target = record.targetFrame ?? new FrameUseRecordDetail();
        SourceFrameNoDisplay = string.IsNullOrWhiteSpace(source.frameNo) ? "-" : source.frameNo!;
        TargetFrameNoDisplay = string.IsNullOrWhiteSpace(target.frameNo) ? "-" : target.frameNo!;

        foreach (var m in source.materials ?? new List<FrameUseRecordMaterial>())
            SourceMaterials.Add(ToMaterialVm(m));

        foreach (var m in target.materials ?? new List<FrameUseRecordMaterial>())
            TargetMaterials.Add(ToMaterialVm(m));
    }

    private static FrameUnloadMaterialItemVm ToMaterialVm(FrameUseRecordMaterial mat)
        => new()
        {
            MaterialNameDisplay = string.IsNullOrWhiteSpace(mat.materialName) ? "-" : mat.materialName!,
            BatchNoDisplay = string.IsNullOrWhiteSpace(mat.batchNo) ? "-" : mat.batchNo!,
            QtyDisplay = $"数量: {(mat.qty ?? 0m):0.##}"
        };
}
