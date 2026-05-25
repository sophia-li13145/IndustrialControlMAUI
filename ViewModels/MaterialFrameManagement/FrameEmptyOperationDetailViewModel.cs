using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameEmptyOperationDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;

    public FrameEmptyOperationDetailViewModel(IMaterialFrameApi api) => _api = api;

    [ObservableProperty] private string recordNoDisplay = "-";
    [ObservableProperty] private string operationTimeDisplay = "-";
    [ObservableProperty] private string operatorDisplay = "-";
    [ObservableProperty] private string frameCountDisplay = "0";

    public ObservableCollection<FrameEmptyFrameItemVm> ReleasedFrames { get; } = new();

    public async Task LoadAsync(string? useRecordId)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(useRecordId)) return;
        var resp = await _api.GetFrameReturnDetailAsync(useRecordId.Trim());
        Apply(resp?.result);
    }

    private void Reset()
    {
        RecordNoDisplay = OperationTimeDisplay = OperatorDisplay = "-";
        FrameCountDisplay = "0";
        ReleasedFrames.Clear();
    }

    private void Apply(FrameUseRecordOperation? record)
    {
        if (record is null) return;

        RecordNoDisplay = string.IsNullOrWhiteSpace(record.recordNo) ? "-" : record.recordNo!;
        OperationTimeDisplay = string.IsNullOrWhiteSpace(record.operationTime) ? "-" : record.operationTime!;
        OperatorDisplay = string.IsNullOrWhiteSpace(record.@operator) ? "-" : record.@operator!;

        var details = record.detailList ?? new List<FrameUseRecordDetail>();
        FrameCountDisplay = (record.frameCount ?? details.Count).ToString();

        foreach (var d in details)
        {
            ReleasedFrames.Add(new FrameEmptyFrameItemVm
            {
                FrameNoDisplay = string.IsNullOrWhiteSpace(d.frameNo) ? "-" : d.frameNo!,
                BeforeStatusDisplay = string.IsNullOrWhiteSpace(d.beforeStatus) ? "-" : d.beforeStatus!
            });
        }
    }
}

public class FrameEmptyFrameItemVm
{
    public string FrameNoDisplay { get; set; } = "-";
    public string BeforeStatusDisplay { get; set; } = "-";
}
