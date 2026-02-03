using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
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

    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<BaseInfoItem> BaseInfos { get; } = new();
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<MoldOutboundDetailRow> ScanDetails { get; } = new();
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<WoStep> WorkflowSteps { get; } = new();
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<UiProcessTask> ProcessTasks { get; } = new();

    private Dictionary<string, string> _auditMap = new();

    // ★ 默认顺序（根据你给的 0/1/4/2/3/5）
    private static readonly string[] WorkflowStatusOrder = new[]
    {
    "0", // 待执行
    "1", // 执行中
    "4", // 待入库
    "2", // 入库中
    "3", // 已完成
    "5"  // 终结
};

    /// <summary>执行 GetStatusOrder 逻辑。</summary>
    private static int GetStatusOrder(string statusValue)
    {
        var idx = Array.IndexOf(WorkflowStatusOrder, statusValue);
        return idx >= 0 ? idx : int.MaxValue;
    }
    /// <summary>执行 MoldOutboundExecuteViewModel 初始化逻辑。</summary>
    public MoldOutboundExecuteViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    /// <summary>执行 LoadAsync 逻辑。</summary>
    public async Task LoadAsync(string orderNo, string? orderId = null)
    {
        OrderNo = orderNo;
        OrderId = orderId;

        await EnsureDictsLoadedAsync();
        InitWorkflowStepsFromDict();
        // ★ 根据当前工单状态文字（StatusText）标记 IsActive / IsDone
        ApplyStatusFromSearch(StatusText);

        await FillWorkflowTimesFromApi();
        ProcessTasks.Clear();
        await LoadProcessTasksAsync();
    }


    /// <summary>执行 EnsureDictsLoadedAsync 逻辑。</summary>
    private async Task EnsureDictsLoadedAsync()
    {
        var bundle = await _api.GetWorkOrderDictsAsync();
        _auditMap = bundle.AuditStatus
            .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
            .ToDictionary(d => d.dictItemValue!, d => d.dictItemName ?? d.dictItemValue!);
    }


    /// <summary>执行 LoadProcessTasksAsync 逻辑。</summary>
    private async Task LoadProcessTasksAsync()
    {
        ProcessTasks.Clear();

        if (string.IsNullOrWhiteSpace(OrderId) && string.IsNullOrWhiteSpace(OrderNo))
            return;

        // 1) 调用工单域接口，拿到工艺路线上的“全部工序结点”
        var domain = await _api.GetWorkOrderDomainAsync(OrderId!, CancellationToken.None);
        var routeDetails = domain?.result?
            .planChildProductSchemeDetailList?
            .FirstOrDefault()?
            .planProcessRoute?
            .routeDetailList;

        if (routeDetails == null || routeDetails.Count == 0)
            return;

        // 2) 调用原来的分页接口，拿到“已经生成的工序任务”（可能只是部分结点）
        var byOrderNo = !string.IsNullOrWhiteSpace(OrderNo);
        var page = await _api.PageWorkProcessTasksAsync(
            workOrderNo: byOrderNo ? OrderNo?.Trim() : null,
            auditStatusList: null,
            processCode: null,
            createdTimeStart: null,
            createdTimeEnd: null,
            pageNo: 0,
            pageSize: 200,
            ct: CancellationToken.None);

        var taskRecords = page?.result?.records ?? new List<ProcessTask>();

        // 3) 把任务按工序编码做字典，方便匹配
        var taskMap = taskRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.ProcessCode))
            .GroupBy(x => x.ProcessCode!)
            .ToDictionary(g => g.Key, g => g.First());

        // 4) 先组装一个中间列表（包含：工艺结点 + 对应任务 + 是否已完成）
        var nodes = routeDetails
            .OrderBy(r => r.sortNumber ?? int.MaxValue)
            .Select((r, idx) =>
            {
                taskMap.TryGetValue(r.processCode ?? string.Empty, out var task);

                // 这里你原来是用 CompletedQty 判定完成，可以按需换成 CompletedQty 或 EndDate
                bool isCompleted = task != null && !string.IsNullOrWhiteSpace(task.EndDate);
                return new
                {
                    Index = idx + 1,
                    Route = r,
                    Task = task,
                    IsCompleted = isCompleted
                };
            })
            .ToList();

        // 5) 根据“第一个未完成结点”的规则给每个结点打状态：完成 / 进行中 / 未开始
        int firstNotCompletedIdx = nodes.FindIndex(n => !n.IsCompleted);

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            string status;

            if (firstNotCompletedIdx == -1)
            {
                // 全部都已完成
                status = "完成";
            }
            else if (i < firstNotCompletedIdx)
            {
                status = "完成";
            }
            else if (i == firstNotCompletedIdx)
            {
                status = "进行中";
            }
            else
            {
                status = "未开始";
            }

            // 日期显示：未开始 或 没有任务记录 => “未开始”
            string startText, endText;
            if (status == "未开始" || n.Task == null)
            {
                startText = "未开始";
                endText = "未开始";
            }
            else
            {
                startText = ToShort(n.Task.StartDate);
                endText = ToShort(n.Task.EndDate);
            }

            // 数量：没有任务时用 0
            var planQty = (n.Task?.ScheQty ?? 0).ToString("0.####");
            var doneQty = (n.Task?.CompletedQty ?? 0).ToString("0.####");

            ProcessTasks.Add(new UiProcessTask
            {
                Index = n.Index,
                Code = n.Route.processCode ?? "",
                Name = n.Route.processName ?? "",
                PlanQty = planQty,
                DoneQty = doneQty,
                Start = startText,
                End = endText,
                StatusText = status,
                IsLast = (i == nodes.Count - 1)
            });
        }
    }


    // 用字典生成步骤后，记得打上首尾标记
    /// <summary>执行 InitWorkflowStepsFromDict 逻辑。</summary>
    private void InitWorkflowStepsFromDict()
    {
        WorkflowSteps.Clear();

        var list = new List<WoStep>();
        int displayIndex = 1;

        // 先按默认顺序 WorkflowStatusOrder 生成节点
        foreach (var code in WorkflowStatusOrder)
        {
            if (_auditMap.TryGetValue(code, out var name))
            {
                list.Add(new WoStep
                {
                    Index = displayIndex++,   // UI 显示用：1,2,3,...
                    Title = name,
                    StatusValue = code       // 接口真实状态值：0/1/2/3/4/5
                });
            }
        }

        // 如果后台以后多给了其它状态（不在默认顺序里），就顺延追加
        foreach (var kv in _auditMap)
        {
            if (!WorkflowStatusOrder.Contains(kv.Key))
            {
                list.Add(new WoStep
                {
                    Index = displayIndex++,
                    Title = kv.Value,
                    StatusValue = kv.Key
                });
            }
        }

        // ★ 两排显示：3 个一行，设置每个节点的 IsFirst/IsLast（每行的“行首/行尾”）
        const int perRow = 3;
        for (int i = 0; i < list.Count; i++)
        {
            var step = list[i];
            var idx = i + 1; // 1-based

            // 行首：第 1 个，4 个，7 个……
            step.IsFirst = (idx == 1) || ((idx - 1) % perRow == 0);

            // 行尾：第 3、6、9…… 或者最后一个
            step.IsLast = (idx % perRow == 0) || (idx == list.Count);
        }

        foreach (var s in list)
            WorkflowSteps.Add(s);
    }


    // 从接口把 statusTime 灌到对应节点上（保证 Time 有值就能显示）
    /// <summary>执行 FillWorkflowTimesFromApi 逻辑。</summary>
    private async Task FillWorkflowTimesFromApi()
    {
        if (string.IsNullOrWhiteSpace(OrderId)) return;

        var resp = await _api.GetWorkOrderWorkflowAsync(OrderId!);
        var list = resp?.result;
        if (list is null || list.Count == 0) return;

        foreach (var step in WorkflowSteps)
        {
            // 用真实状态值匹配
            var match = list.FirstOrDefault(x => x.statusValue == step.StatusValue);
            if (match != null && DateTime.TryParse(match.statusTime, out var dt))
                step.Time = dt;
        }

    }



    /// <summary>执行 ApplyStatusFromSearch 逻辑。</summary>
    private void ApplyStatusFromSearch(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText)) return;

        var match = _auditMap.FirstOrDefault(x => x.Value == statusText);
        if (string.IsNullOrEmpty(match.Key)) return;

        // 当前状态在默认顺序里的“序号”
        var curOrder = GetStatusOrder(match.Key);

        foreach (var s in WorkflowSteps)
        {
            var stepOrder = GetStatusOrder(s.StatusValue);

            // 当前节点：真实状态值相等
            s.IsActive = (s.StatusValue == match.Key);

            // 已完成：在默认顺序中排在当前之前
            s.IsDone = stepOrder < curOrder;
        }
    }





    private static string ToShort(string? s)
        => DateTime.TryParse(s, out var d) ? d.ToString("yyyy-MM-dd") : "";

}

// ==== 模型类 ====
public class BaseInfoItem { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
public class MoldOutboundDetailRow { public int Index { get; set; } public bool Selected { get; set; } public string MoldCode { get; set; } = ""; public string MoldModel { get; set; } = ""; public int Qty { get; set; } public string Bin { get; set; } = ""; }
public partial class WoStep : ObservableObject
{
    [ObservableProperty] private int index;
    [ObservableProperty] private string title = "";
    // ★ 新增：接口真实状态值（0/1/2/3/4/5）
    [ObservableProperty] private string statusValue = "";

    // 当 Time 变化时，自动通知 TimeText 一起变更
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TimeText))]
    private DateTime? time;

    [ObservableProperty] private bool isActive;
    [ObservableProperty] private bool isDone;
    [ObservableProperty] private bool isFirst;   // 用于隐藏左连接线
    [ObservableProperty] private bool isLast;    // 用于隐藏右连接线

    // 给 XAML 用的已格式化文本（只保留到“日”）
    /// <summary>执行 ToString 逻辑。</summary>
    public string TimeText => time.HasValue ? time.Value.ToString("yyyy-MM-dd") : string.Empty;
}

public sealed class UiProcessTask { public int Index { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string PlanQty { get; set; } = ""; public string DoneQty { get; set; } = ""; public string Start { get; set; } = ""; public string End { get; set; } = ""; public string StatusText { get; set; } = ""; public bool IsLast { get; set; }
}
