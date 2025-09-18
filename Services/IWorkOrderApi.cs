using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IWorkOrderApi
    {
        // ① 工单分页列表
        Task<WorkOrderPageResult> GetWorkOrdersAsync(WorkOrderQuery q, CancellationToken ct = default);

        Task<DictBundle> GetWorkOrderDictsAsync(CancellationToken ct = default);
        Task<WorkflowResp?> GetWorkOrderWorkflowAsync(string id, CancellationToken ct = default);
        Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(string workOrderNo, int pageNo = 1, int pageSize = 50, CancellationToken ct = default);
    }

    // ===================== 实现 =====================
    public class WorkOrderApi : IWorkOrderApi
    {
        private readonly HttpClient _http;

        // 统一由 appconfig.json 管理的端点路径
        private readonly string _pageEndpoint;
        private readonly string _workflowEndpoint;
        private readonly string _processTasksEndpoint;
        private readonly string _dictEndpoint;

        public WorkOrderApi(HttpClient http, IConfigLoader configLoader)
        {
            _http = http;

            // 读取 appconfig.json（AppData 下的生效配置）
            JsonNode cfg = configLoader.Load();

            // 优先新结构 apiEndpoints.workOrder.*；其次兼容旧键名；最后兜底硬编码
            _pageEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["page"]
                ?? (string?)cfg?["apiEndpoints"]?["pageWorkOrders"]
                ?? "/normalService/pda/pmsWorkOrder/pageWorkOrders";

            _workflowEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["workflow"]
                ?? (string?)cfg?["apiEndpoints"]?["getWorkOrderWorkflow"]
                ?? "/normalService/pda/pmsWorkOrder/getWorkOrderWorkflow";

            _processTasksEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["processTasks"]
                ?? (string?)cfg?["apiEndpoints"]?["pageWorkProcessTasks"]
                ?? "/normalService/pda/pmsWorkOrder/pageWorkProcessTasks";
            _dictEndpoint =
            (string?)cfg?["apiEndpoints"]?["workOrder"]?["dictList"]
            ?? "/normalService/pda/pmsWorkOrder/getWorkOrderDictList";
        }
        private static string BuildQuery(IDictionary<string, string> p)
            => string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        // using System.Text.Json;
        // using System.Text;

        public async Task<WorkOrderPageResult> GetWorkOrdersAsync(WorkOrderQuery q, CancellationToken ct = default)
        {
            // 1) 先把所有要传的参数放进字典（只加有值的）
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = q.PageNo.ToString(),
                ["pageSize"] = q.PageSize.ToString()
            };
            if (q.CreatedTimeStart.HasValue) p["createdTimeStart"] = q.CreatedTimeStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (q.CreatedTimeEnd.HasValue) p["createdTimeEnd"] = q.CreatedTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrWhiteSpace(q.WorkOrderNo)) p["workOrderNo"] = q.WorkOrderNo!.Trim();
            if (!string.IsNullOrWhiteSpace(q.MaterialName)) p["materialName"] = q.MaterialName!.Trim();

            // 2) 逐项进行 Uri.EscapeDataString 编码，避免出现“空格没编码”的情况
            string qs = string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            var url = _pageEndpoint + "?" + qs;

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] GET " + (_http.BaseAddress?.ToString() ?? "") + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] Resp: " + json[..Math.Min(300, json.Length)] + "...");

            if (!httpResp.IsSuccessStatusCode)
                return new WorkOrderPageResult { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resp = JsonSerializer.Deserialize<WorkOrderPageResult>(json, options) ?? new WorkOrderPageResult();

            // 兼容 result.records
            var nested = resp.result?.records;
            if (nested is not null && resp.result is not null)
            {
                if (resp.result.records is null || resp.result.records.Count == 0)
                    resp.result.records = nested;
                if (resp.result.pageNo == 0) resp.result.pageNo = resp.result.list.pageNo;
                if (resp.result.pageSize == 0) resp.result.pageSize = resp.result.list.pageSize;
                if (resp.result.total == 0) resp.result.total = resp.result.list.total;
            }

            return resp;
        }


        /// <summary>
        /// 工单流程：/getWorkOrderWorkflow?id=...
        /// 返回 result 为数组（statusValue/statusName/statusTime）
        /// </summary>
        public async Task<WorkflowResp?> GetWorkOrderWorkflowAsync(string id, CancellationToken ct = default)
        {
            var p = new Dictionary<string, string> { ["id"] = id?.Trim() ?? "" };
            var url = _workflowEndpoint + "?" + BuildQuery(p);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] GET " + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] Resp(getWorkOrderWorkflow): " + json[..Math.Min(300, json.Length)] + "...");

            if (!httpResp.IsSuccessStatusCode)
                return new WorkflowResp { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resp = JsonSerializer.Deserialize<WorkflowResp>(json, options) ?? new WorkflowResp();
            return resp;
        }

        /// <summary>
        /// 工序分页：/pageWorkProcessTasks?pageNo=&pageSize=&workOrderNo=
        /// 返回分页结构，数据在 result.records[]
        /// </summary>
        public async Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(
            string workOrderNo, int pageNo = 1, int pageSize = 50, CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString()
            };
            if (!string.IsNullOrWhiteSpace(workOrderNo)) p["workOrderNo"] = workOrderNo.Trim();

            var url = _processTasksEndpoint + "?" + BuildQuery(p);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] GET " + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[WorkOrderApi] Resp(pageWorkProcessTasks): " + json[..Math.Min(300, json.Length)] + "...");

            if (!httpResp.IsSuccessStatusCode)
                return new PageResp<ProcessTask> { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resp = JsonSerializer.Deserialize<PageResp<ProcessTask>>(json, options) ?? new PageResp<ProcessTask>();

            // 兼容 result.records（你的实际返回就是 records，结构示例如你发的 JSON）
            // 如果后端某些场景包在 result.list.records，也一并兼容
            var nested = resp.result?.records ?? resp.result?.records;
            if (nested is not null && resp.result is not null)
            {
                if (resp.result.records is null || resp.result.records.Count == 0)
                    resp.result.records = nested;

                if (resp.result.pageNo == 0 && resp.result is not null) resp.result.pageNo = resp.result.pageNo;
                if (resp.result.pageSize == 0 && resp.result is not null) resp.result.pageSize = resp.result.pageSize;
                if (resp.result.total == 0 && resp.result is not null) resp.result.total = resp.result.total;
            }

            return resp;
        }

        public async Task<DictBundle> GetWorkOrderDictsAsync(CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, _dictEndpoint);
            using var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            var opt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = System.Text.Json.JsonSerializer.Deserialize<DictResponse>(json, opt);

            var all = dto?.result ?? new List<DictField>();
            var audit = all.FirstOrDefault(f => string.Equals(f.field, "auditStatus", StringComparison.OrdinalIgnoreCase))
                          ?.dictItems ?? new List<DictItem>();
            var urgent = all.FirstOrDefault(f => string.Equals(f.field, "urgent", StringComparison.OrdinalIgnoreCase))
                          ?.dictItems ?? new List<DictItem>();

            return new DictBundle { AuditStatus = audit, Urgent = urgent };
        }


    }

    // ===================== 请求模型 =====================
    public class WorkOrderQuery
    {
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        // 0 待执行；1 执行中；2 入库中；3 已完成
        public string? AuditStatus { get; set; }

        public DateTime? CreatedTimeStart { get; set; }
        public DateTime? CreatedTimeEnd { get; set; }

        public string? WorkOrderNo { get; set; }
        public string? MaterialName { get; set; }
    }

    // ===================== 返回模型：分页 =====================
    // 顶层：{ code, message, success, result: {...}, costTime }
    public class WorkOrderPageResult
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public WorkOrderPageData? result { get; set; }
        public long costTime { get; set; }
    }

    // result 可能是 {pageNo,pageSize,records} 或 {list:{pageNo,pageSize,records}}
    public class WorkOrderPageData
    {
        public WorkOrderPageList? list { get; set; }

        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<WorkOrderRecord> records { get; set; } = new();
    }

    public class WorkOrderPageList
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<WorkOrderRecord> records { get; set; } = new();
    }

    // 只列页面需要字段；后续按实际补
    public class WorkOrderRecord
    {
        public string? id { get; set; }
        public string? workOrderNo { get; set; }
        public string? workOrderName { get; set; }

        public string? auditStatus { get; set; }     // ★ "1" 这样的字符串

        public decimal? curQty { get; set; }

        public string? materialCode { get; set; }
        public string? materialName { get; set; }

        public string? line { get; set; }
        public string? lineName { get; set; }
        public string? workShop { get; set; }
        public string? workShopName { get; set; }
        public string? urgent { get; set; }

        // ★ 这些时间都是 "yyyy-MM-dd HH:mm:ss" 字符串
        public string? schemeStartDate { get; set; }
        public string? schemeEndDate { get; set; }
        public string? createdTime { get; set; }
        public string? modifiedTime { get; set; }
        public string? commitedTime { get; set; }

        public string? bomCode { get; set; }
        public string? routeName { get; set; }
    }

    // ===================== 返回模型：工作流 =====================
    public class WorkOrderWorkflowResp
    {
        public int code { get; set; }
        public long costTime { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public WorkOrderWorkflow? result { get; set; }
    }

    public class WorkOrderWorkflow
    {
        public string? statusName { get; set; }   // 待执行/执行中/入库中/已完成
        public string? statusTime { get; set; }   // 时间字符串
        public int? statusValue { get; set; }     // 0/1/2/3
    }

    // ===================== 返回模型：工序节点 =====================
    public class ProcessTasksPageResult
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public long costTime { get; set; }
        public ProcessTasksList? result { get; set; }
    }

    public class ProcessTasksList
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<ProcessTaskRecord> records { get; set; } = new();
    }

    public class ProcessTaskRecord
    {
        public string? processName { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int? sortNumber { get; set; }
    }
    public class DictResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<DictField>? result { get; set; }
        public long costTime { get; set; }
    }

    public class DictField
    {
        public string? field { get; set; }
        public List<DictItem> dictItems { get; set; } = new();
    }

    public class DictItem
    {
        public string? dictItemValue { get; set; } // 参数值（"0"/"1"/"2"/"3"/"4"...）
        public string? dictItemName { get; set; } // 显示名（"待执行"/"执行中"...）
    }
    public class DictBundle
    {
        public List<DictItem> AuditStatus { get; set; } = new();
        public List<DictItem> Urgent { get; set; } = new();
    }
    public sealed class WorkflowResp { public bool success { get; set; } public string? message { get; set; } public int code { get; set; } public List<WorkflowItem>? result { get; set; } }
    public sealed class WorkflowItem { public string? statusValue { get; set; } public string? statusName { get; set; } public string? statusTime { get; set; } }

    public sealed class PageResp<T> { public bool success { get; set; } public string? message { get; set; } public int code { get; set; } public PageResult<T>? result { get; set; } }
    public sealed class PageResult<T> { public int pageNo { get; set; } public int pageSize { get; set; } public int total { get; set; } public List<T>? records { get; set; } }

    public sealed class ProcessTask
    {
        public string? id { get; set; }
        public string? processCode { get; set; }
        public string? processName { get; set; }
        public decimal? scheQty { get; set; }
        public decimal? completedQty { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int? sortNumber { get; set; }
        public string? auditStatus { get; set; }
    }
}
