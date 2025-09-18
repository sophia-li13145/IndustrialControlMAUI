using IndustrialControlMAUI.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IMoldApi
    {
        // ① 工单分页列表
        Task<WorkOrderPageResult> GetMoldsAsync(MoldQuery q, CancellationToken ct = default);

        Task<DictBundle> GetMoldDictsAsync(CancellationToken ct = default);
        Task<WorkflowResp?> GetMoldWorkflowAsync(string id, CancellationToken ct = default);
        Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(string workOrderNo, int pageNo = 1, int pageSize = 50, CancellationToken ct = default);
        // Task<IEnumerable<MoldOrderSummary>> ListInboundOrdersAsync(
        //string? orderNoOrBarcode,
        //DateTime startDate,
        //DateTime endDate,
        //string orderType,
        //string[] orderTypeList,
        //CancellationToken ct = default);

        Task<IReadOnlyList<InboundScannedRow>> GetInStockScanDetailAsync(string instockId, CancellationToken ct = default);
        /// <summary>扫描条码入库</summary>
        Task<SimpleOk> InStockByBarcodeAsync(string instockId, string barcode, CancellationToken ct = default);
        /// <summary>PDA 扫描通过（确认当前入库单已扫描项）</summary>
        /// 
        ///---------------------------------------------------------------------
        /// <summary>模具入库的扫描查询接口</summary>
        Task<MoldScanQueryResp?> InStockScanQueryAsync(string code, CancellationToken ct = default);

        Task<SimpleOk> ConfirmInStockByListAsync(InStockConfirmReq req, CancellationToken ct = default);

    }

    // ===================== 实现 =====================
    public class MoldApi : IMoldApi
    {
        private readonly HttpClient _http;

        // 统一由 appconfig.json 管理的端点路径
        private readonly string _pageEndpoint;
        private readonly string _workflowEndpoint;
        private readonly string _processTasksEndpoint;
        private readonly string _dictEndpoint;
        private readonly string _scanDetailEndpoint;
        private readonly string _scanByBarcodeEndpoint;

        private readonly string _scanQueryEndpoint;
        private readonly string _confirmInStockEndpoint;

        public MoldApi(HttpClient http, IConfigLoader configLoader)
        {
            _http = http;

            // 读取 appconfig.json（AppData 下的生效配置）
            JsonNode cfg = configLoader.Load();

            // 优先新结构 apiEndpoints.workOrder.*；其次兼容旧键名；最后兜底硬编码
            _pageEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["page"]
                ?? (string?)cfg?["apiEndpoints"]?["pageMolds"]
                ?? "/normalService/pda/pmsMold/pageMolds";

            _workflowEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["workflow"]
                ?? (string?)cfg?["apiEndpoints"]?["getMoldWorkflow"]
                ?? "/normalService/pda/pmsMold/getMoldWorkflow";

            _processTasksEndpoint =
                (string?)cfg?["apiEndpoints"]?["workOrder"]?["processTasks"]
                ?? (string?)cfg?["apiEndpoints"]?["pageWorkProcessTasks"]
                ?? "/normalService/pda/pmsMold/pageWorkProcessTasks";
            _dictEndpoint =
            (string?)cfg?["apiEndpoints"]?["workOrder"]?["dictList"]
            ?? "/normalService/pda/pmsMold/getMoldDictList";
            _scanDetailEndpoint = "";
            _scanByBarcodeEndpoint = "";
            _scanQueryEndpoint = EnsureSlash((string?)cfg?["apiEndpoints"]?["mold"]?["inStockScanQuery"])
                                      ?? "/normalService/pda/mold/inStockScanQuery";
            _confirmInStockEndpoint = EnsureSlash((string?)cfg?["apiEndpoints"]?["mold"]?["inStock"])
                                      ?? "/normalService/pda/mold/inStock";
        }
        private static string? EnsureSlash(string? ep)
        {
            if (string.IsNullOrWhiteSpace(ep)) return null;
            return ep[0] == '/' ? ep : "/" + ep;
        }
        private static string BuildQuery(IDictionary<string, string> p)
        => string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        // using System.Text.Json;
        // using System.Text;

        public async Task<WorkOrderPageResult> GetMoldsAsync(MoldQuery q, CancellationToken ct = default)
        {
            // 1) 先把所有要传的参数放进字典（只加有值的）
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = q.PageNo.ToString(),
                ["pageSize"] = q.PageSize.ToString()
            };
            if (q.CreatedTimeStart.HasValue) p["createdTimeStart"] = q.CreatedTimeStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (q.CreatedTimeEnd.HasValue) p["createdTimeEnd"] = q.CreatedTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrWhiteSpace(q.MoldNo)) p["workOrderNo"] = q.MoldNo!.Trim();
            if (!string.IsNullOrWhiteSpace(q.MaterialName)) p["materialName"] = q.MaterialName!.Trim();

            // 2) 逐项进行 Uri.EscapeDataString 编码，避免出现“空格没编码”的情况
            string qs = string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            var url = _pageEndpoint + "?" + qs;

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            System.Diagnostics.Debug.WriteLine("[MoldApi] GET " + (_http.BaseAddress?.ToString() ?? "") + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[MoldApi] Resp: " + json[..Math.Min(300, json.Length)] + "...");

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
        /// 工单流程：/getMoldWorkflow?id=...
        /// 返回 result 为数组（statusValue/statusName/statusTime）
        /// </summary>
        public async Task<WorkflowResp?> GetMoldWorkflowAsync(string id, CancellationToken ct = default)
        {
            var p = new Dictionary<string, string> { ["id"] = id?.Trim() ?? "" };
            var url = _workflowEndpoint + "?" + BuildQuery(p);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            System.Diagnostics.Debug.WriteLine("[MoldApi] GET " + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[MoldApi] Resp(getMoldWorkflow): " + json[..Math.Min(300, json.Length)] + "...");

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
            System.Diagnostics.Debug.WriteLine("[MoldApi] GET " + url);

            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);
            System.Diagnostics.Debug.WriteLine("[MoldApi] Resp(pageWorkProcessTasks): " + json[..Math.Min(300, json.Length)] + "...");

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

        public async Task<DictBundle> GetMoldDictsAsync(CancellationToken ct = default)
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
        static int ToInt(decimal? v) => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : 0;
        public async Task<IReadOnlyList<InboundScannedRow>> GetInStockScanDetailAsync(
    string instockId,
    CancellationToken ct = default)
        {
            // 文档为 GET + x-www-form-urlencoded，这里用 query 传递（关键在大小写常为 InstockId）
            var url = $"{_scanDetailEndpoint}?InstockId={Uri.EscapeDataString(instockId)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<GetInStockScanDetailResp>(json, opt);

            if (dto?.success != true || dto.result is null || dto.result.Count == 0)
                return Array.Empty<InboundScannedRow>();

            static int ToIntSafe(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return 0;
                // 去除千分位、空格
                s = s.Trim().Replace(",", "");
                return int.TryParse(s, out var v) ? v : 0;
            }

            // 映射：InstockId <- id（截图注释“入库单明细主键id”）
            var list = dto.result.Select(x => new InboundScannedRow(
                Barcode: (x.barcode ?? string.Empty).Trim(),
                DetailId: (x.id ?? string.Empty).Trim(),
                Location: (x.location ?? string.Empty).Trim(),
                MaterialName: (x.materialName ?? string.Empty).Trim(),
                Qty: ToInt(x.qty),
                Spec: (x.spec ?? string.Empty).Trim(),
                ScanStatus: x.scanStatus ?? false,
                WarehouseCode: x.warehouseCode?.Trim()
            )).ToList();

            return list;
        }

        // ========= 扫码入库实现 =========
        public async Task<SimpleOk> InStockByBarcodeAsync(string instockId, string barcode, CancellationToken ct = default)
        {
            var body = JsonSerializer.Serialize(new { barcode, instockId });
            using var req = new HttpRequestMessage(HttpMethod.Post, _scanByBarcodeEndpoint)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);
            var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<ScanByBarcodeResp>(json, opt);

            // 按文档：以 success 判断；message 作为失败提示
            var ok = dto?.success == true;
            return new SimpleOk(ok, dto?.message);
        }


        // =============== 扫描查询（GET，相对URL + BaseAddress） ===============
        public async Task<MoldScanQueryResp?> InStockScanQueryAsync(string code, CancellationToken ct = default)
        {
            var url = $"{_scanQueryEndpoint}?code={Uri.EscapeDataString(code ?? string.Empty)}";
            using var res = await _http.GetAsync(url, ct);     // 依赖 BaseAddress
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync(ct);
            return System.Text.Json.JsonSerializer.Deserialize<MoldScanQueryResp>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // =============== 确认入库（POST，相对URL + BaseAddress） ===============
        public async Task<SimpleOk> ConfirmInStockByListAsync(InStockConfirmReq req, CancellationToken ct = default)
        {
            var body = System.Text.Json.JsonSerializer.Serialize(req);
            using var msg = new HttpRequestMessage(HttpMethod.Post, _confirmInStockEndpoint)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };

            using var res = await _http.SendAsync(msg, ct);     // 依赖 BaseAddress
            var txt = await res.Content.ReadAsStringAsync(ct);

            var dto = System.Text.Json.JsonSerializer.Deserialize<ConfirmResp>(
                txt, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = dto?.success == true && dto.result == true;
            return new SimpleOk(ok, dto?.message ?? (ok ? "确认入库成功" : "确认入库失败"));
        }

    }

    // ===================== 请求模型 =====================
    public class MoldQuery
    {
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        // 0 待执行；1 执行中；2 入库中；3 已完成
        public string? AuditStatus { get; set; }

        public DateTime? CreatedTimeStart { get; set; }
        public DateTime? CreatedTimeEnd { get; set; }

        public string? MoldNo { get; set; }
        public string? MaterialName { get; set; }
    }

    // ===================== 返回模型：分页 =====================
    // 顶层：{ code, message, success, result: {...}, costTime }
    public class MoldPageResult
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public MoldPageData? result { get; set; }
        public long costTime { get; set; }
    }

    // result 可能是 {pageNo,pageSize,records} 或 {list:{pageNo,pageSize,records}}
    public class MoldPageData
    {
        public MoldPageList? list { get; set; }

        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<MoldRecord> records { get; set; } = new();
    }

    public class MoldPageList
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<MoldRecord> records { get; set; } = new();
    }

    // 只列页面需要字段；后续按实际补
    public class MoldRecord
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
    public class MoldWorkflowResp
    {
        public int code { get; set; }
        public long costTime { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public MoldWorkflow? result { get; set; }
    }

    public class MoldWorkflow
    {
        public string? statusName { get; set; }   // 待执行/执行中/入库中/已完成
        public string? statusTime { get; set; }   // 时间字符串
        public int? statusValue { get; set; }     // 0/1/2/3
    }
    public class MoldScanQueryResp
    {
        public int code { get; set; }
        public long costTime { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public MoldScanQueryData? result { get; set; }
    }

    public class MoldScanQueryData
    {
        public string? location { get; set; }
        public string? moldCode { get; set; }
        public string? moldModel { get; set; }
        public string? outstockDate { get; set; }
        public bool? usageStatus { get; set; } // 文档：0-未使用 / 1-使用中（这里用 bool 接）
        public string? workOrderNo { get; set; }
        public string? warehouseCode { get; set; }
        public string? warehouseName { get; set; }
    }

    // ============== 新增：请求/响应模型 ==============
    public sealed class InStockConfirmReq
    {
        public string? instockDate { get; set; }                 // 建议: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        public string? memo { get; set; }
        public string? @operator { get; set; }
        public string? orderType { get; set; }
        public string? orderTypeName { get; set; }
        public List<InStockDetail> wmsMaterialInstockDetailList { get; set; } = new();
        public string? workOrderNo { get; set; }
    }
    public sealed class InStockDetail
    {
        public int instockQty { get; set; }
        public string? instockWarehouse { get; set; }            // 若页面无该列，可为 null/空
        public string? instockWarehouseCode { get; set; }        // 映射 ScannedList.WarehouseCode
        public string? location { get; set; }                    // 映射 ScannedList.Bin
        public string? materialCode { get; set; }                // 页面没有就留空
        public string? materialName { get; set; }                // 映射 ScannedList.Name
        public string? model { get; set; }                       // 映射 ScannedList.Spec
    }
}
