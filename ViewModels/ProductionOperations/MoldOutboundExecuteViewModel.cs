using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class MoldOutboundExecuteViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;

    [ObservableProperty] private string? orderNo;
    [ObservableProperty] private string? orderId;
    [ObservableProperty] private string? statusText;      // 状态
    [ObservableProperty] private string? orderName;       // 工单名称
    [ObservableProperty] private string? urgent;          // 优先级(中文)
    [ObservableProperty] private string? productName;     // 产品/物料名称
    [ObservableProperty] private string? planQtyText;     // 生产数量(文本)
    [ObservableProperty] private string? planStartText;   // 计划开始日期(文本)
    [ObservableProperty] private string? createDateText;  // 创建日期(文本)
    [ObservableProperty] private string? bomCode;         // BOM编号
    [ObservableProperty] private string? routeName;       // 工艺路线名称

    public ObservableCollection<BaseInfoItem> BaseInfos { get; } = new();
    public ObservableCollection<MoldOutboundDetailRow> ScanDetails { get; } = new();
    public ObservableCollection<WoStep> WorkflowSteps { get; } = new();
    public ObservableCollection<UiProcessTask> ProcessTasks { get; } = new();

    private Dictionary<string, string> _auditMap = new();

    public MoldOutboundExecuteViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    public async Task LoadAsync(string orderNo, string? orderId = null, IEnumerable<BaseInfoItem>? baseInfos = null)
    {
        OrderNo = orderNo;
        OrderId = orderId;

        await EnsureDictsLoadedAsync();
        InitWorkflowStepsFromDict();

        // 先把 BaseInfos（如果有）落位到固定字段，保证中间表格有值
        if (baseInfos != null)
        {
            BaseInfos.Clear();
            foreach (var it in baseInfos) BaseInfos.Add(it);
            PopulateFixedFieldsFromBaseInfos(); // ★ 新增调用（你之前缺少这句）
        }

        var statusTextFromSearch = BaseInfos.FirstOrDefault(x => x.Key.Replace("：", "") == "状态")?.Value;
        ApplyStatusFromSearch(statusTextFromSearch);

        await FillWorkflowTimesFromApi();
        ProcessTasks.Clear();
        await LoadProcessTasksAsync();
    }


    private async Task EnsureDictsLoadedAsync()
    {
        var bundle = await _api.GetWorkOrderDictsAsync();
        _auditMap = bundle.AuditStatus
            .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
            .ToDictionary(d => d.dictItemValue!, d => d.dictItemName ?? d.dictItemValue!);
    }

    public void SetFixedFieldsFromBaseInfos(IEnumerable<BaseInfoItem> items)
    {
        BaseInfos.Clear();
        foreach (var it in items) BaseInfos.Add(it);
        PopulateFixedFieldsFromBaseInfos(); // ← 把值填到 StatusText/OrderName/... 等固定属性
    }

    private async Task LoadProcessTasksAsync()
    {
        ProcessTasks.Clear();
        if (string.IsNullOrWhiteSpace(OrderNo)) return;

        var page = await _api.PageWorkProcessTasksAsync(OrderNo!, 1, 50);
        var records = page?.result?.records;
        if (records == null || records.Count == 0) return;

        int i = 1;
        foreach (var t in records.OrderBy(x => x.sortNumber ?? int.MaxValue))
        {
            var statusName = MapByDict(_auditMap, t.auditStatus); // e.g. "未开始"/"进行中"/"完成"
            ProcessTasks.Add(new UiProcessTask
            {
                Index = i++,
                Code = t.processCode ?? "",
                Name = t.processName ?? "",
                PlanQty = (t.scheQty ?? 0).ToString("0.####"),
                DoneQty = (t.completedQty ?? 0).ToString("0.####"),
                Start = ToShort(t.startDate),
                End = ToShort(t.endDate),
                StatusText = statusName
            });
        }
    }
    // 用字典生成步骤后，记得打上首尾标记
    private void InitWorkflowStepsFromDict()
    {
        WorkflowSteps.Clear();
        foreach (var kv in _auditMap.OrderBy(kv => int.Parse(kv.Key)))
        {
            WorkflowSteps.Add(new WoStep
            {
                Index = int.Parse(kv.Key),
                Title = kv.Value
            });
        }
        if (WorkflowSteps.Count > 0)
        {
            WorkflowSteps[0].IsFirst = true;
            WorkflowSteps[^1].IsLast = true;
        }
    }

    // 从接口把 statusTime 灌到对应节点上（保证 Time 有值就能显示）
    private async Task FillWorkflowTimesFromApi()
    {
        if (string.IsNullOrWhiteSpace(OrderId)) return;

        var resp = await _api.GetWorkOrderWorkflowAsync(OrderId!);
        var list = resp?.result;
        if (list is null || list.Count == 0) return;

        foreach (var step in WorkflowSteps)
        {
            var match = list.FirstOrDefault(x => x.statusValue == step.Index.ToString());
            if (match != null && DateTime.TryParse(match.statusTime, out var dt))
                step.Time = dt;
        }
    }



    private void ApplyStatusFromSearch(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText)) return;

        var match = _auditMap.FirstOrDefault(x => x.Value == statusText);
        if (string.IsNullOrEmpty(match.Key)) return;
        if (!int.TryParse(match.Key, out int cur)) return;

        foreach (var s in WorkflowSteps)
        {
            s.IsActive = (s.Index == cur);
            s.IsDone = (s.Index < cur);
        }
    }





    private static string MapByDict(Dictionary<string, string> map, string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;
        return map.TryGetValue(code, out var name) ? name : code;
    }

    private static string ToShort(string? s)
        => DateTime.TryParse(s, out var d) ? d.ToString("MM-dd HH:mm") : "";

    [RelayCommand] public Task ConfirmAsync() => Task.CompletedTask;
    [RelayCommand] public Task CancelScanAsync() => Task.CompletedTask;
    private string GetInfo(params string[] keys)
    {
        if (BaseInfos.Count == 0) return "";
        foreach (var key in keys)
        {
            var hit = BaseInfos.FirstOrDefault(x =>
                string.Equals(x.Key?.Replace("：", ""), key, StringComparison.OrdinalIgnoreCase));
            if (hit is not null) return hit.Value ?? "";
        }
        return "";
    }
    private void PopulateFixedFieldsFromBaseInfos()
    {
        StatusText = GetInfo("状态");
        OrderName = GetInfo("工单名称", "工单名", "订单名称");
        Urgent = GetInfo("优先级", "紧急程度");
        ProductName = GetInfo("产品名称", "物料名称");
        PlanQtyText = GetInfo("生产数量", "计划数量");
        PlanStartText = GetInfo("计划开始日期", "计划开始时间", "计划开始");
        CreateDateText = GetInfo("创建日期", "创建时间");
        BomCode = GetInfo("BOM编号", "BOM码", "BOM");
        RouteName = GetInfo("工艺路线名称", "工艺路线", "工艺名");
    }
}

// ==== 模型类 ====
public class BaseInfoItem { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
public class MoldOutboundDetailRow { public int Index { get; set; } public bool Selected { get; set; } public string MoldCode { get; set; } = ""; public string MoldModel { get; set; } = ""; public int Qty { get; set; } public string Bin { get; set; } = ""; }
public partial class WoStep : ObservableObject
{
    [ObservableProperty] private int index;
    [ObservableProperty] private string title = "";

    // 当 Time 变化时，自动通知 TimeText 一起变更
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TimeText))]
    private DateTime? time;

    [ObservableProperty] private bool isActive;
    [ObservableProperty] private bool isDone;
    [ObservableProperty] private bool isFirst;   // 用于隐藏左连接线
    [ObservableProperty] private bool isLast;    // 用于隐藏右连接线

    // 给 XAML 用的已格式化文本（只保留到“日”）
    public string TimeText => time.HasValue ? time.Value.ToString("yyyy-MM-dd") : string.Empty;
}

public sealed class UiProcessTask { public int Index { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string PlanQty { get; set; } = ""; public string DoneQty { get; set; } = ""; public string Start { get; set; } = ""; public string End { get; set; } = ""; public string StatusText { get; set; } = ""; }
