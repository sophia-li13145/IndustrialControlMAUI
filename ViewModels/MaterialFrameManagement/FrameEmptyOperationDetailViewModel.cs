using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class FrameEmptyOperationDetailViewModel : ObservableObject
{
    private readonly IMaterialFrameApi _api;
    private Dictionary<string, string> _frameStatusDict = new(StringComparer.OrdinalIgnoreCase);

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
        await EnsureFrameStatusDictLoadedAsync();
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
        FrameCountDisplay = (record.frameCount ?? 0).ToString();

        foreach (var d in details)
        {
            ReleasedFrames.Add(new FrameEmptyFrameItemVm
            {
                FrameNoDisplay = string.IsNullOrWhiteSpace(d.frameNo) ? "-" : d.frameNo!,
                BeforeStatusDisplay = ResolveFrameStatusDisplay(d.beforeStatus)
            });
        }
    }

    private async Task EnsureFrameStatusDictLoadedAsync()
    {
        if (_frameStatusDict.Count > 0) return;
        var fields = await _api.GetStatusDictListAsync();
        var statusField = fields?.FirstOrDefault(x => string.Equals(x.field, "frameStatus", StringComparison.OrdinalIgnoreCase));
        var dict = (statusField?.dictItems ?? new List<DictItem>())
             .Where(x => !string.IsNullOrWhiteSpace(x.dictItemValue))
             .GroupBy(x => x.dictItemValue!, StringComparer.OrdinalIgnoreCase)
             .ToDictionary(
                 k => k.Key,
                 v => string.IsNullOrWhiteSpace(v.First().dictItemName) ? v.First().dictItemValue! : v.First().dictItemName!,
                 StringComparer.OrdinalIgnoreCase);
        _frameStatusDict = dict;
    }

    private string ResolveFrameStatusDisplay(string? frameStatus)
    {
        var key = frameStatus?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return "-";
        return _frameStatusDict.TryGetValue(key, out var name) ? name : key;
    }
}

public class FrameEmptyFrameItemVm
{
    public string FrameNoDisplay { get; set; } = "-";
    public string BeforeStatusDisplay { get; set; } = "-";
}
