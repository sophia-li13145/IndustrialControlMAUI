using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IEquipmentApi
    {
        Task<PageResponeResult<InspectionRecordDto>> PageQueryAsync(
         int pageNo,
         int pageSize,
         string? inspectNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? inspectStatus,
         bool searchCount,
         CancellationToken ct = default);
        Task<DictInspection> GetInspectionDictsAsync(CancellationToken ct = default);
        Task<ApiResp<InspectionDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default);

        Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default);
        //--------------------------------------------
        Task<PageResponeResult<MaintenanceRecordDto>> MainPageQueryAsync(
            int pageNo,
            int pageSize,
            string? upkeepNo,
            string? planUpkeepTimeBegin,
            string? planUpkeepTimeEnd,
            string? upkeepStatus,
            bool searchCount,
            CancellationToken ct = default);
        Task<DictMaintenance> GetMainDictsAsync(CancellationToken ct = default);
        Task<ApiResp<MaintenanceDetailDto>?> GetMainDetailAsync(string id, CancellationToken ct = default);
       
        Task<ApiResp<List<MaintenanceWorkflowNode>>> GetMainWorkflowAsync(string id, CancellationToken ct = default);

        Task<PageResponeResult<RepairRecordDto>> RepairPageQueryAsync(
           int pageNo,
           int pageSize,
           string? workOrderNo,
           string? submitTimeBegin,
           string? submitTimeEnd,
           string? auditStatus,
           bool searchCount,
           CancellationToken ct = default);
        Task<DictRepair> GetRepairDictsAsync(CancellationToken ct = default);
        Task<ApiResp<RepairDetailDto>?> GetRepairDetailAsync(string id, CancellationToken ct = default);

        Task<ApiResp<List<RepairWorkflowNode>>> GetRepairWorkflowAsync(string id, CancellationToken ct = default);

    }


    // ===================== 实现 =====================
    public class EquipmentApi : IEquipmentApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;
        private readonly string _pageEndpoint;
        private readonly string _dictEndpoint;
        private readonly string _detailsEndpoint;
        private readonly string _workflowPath;
        private readonly string _mainPageEndpoint;
        private readonly string _dictMainEndpoint;
        private readonly string _mainDetailsEndpoint;
        private readonly string _mainWorkflowPath;
        private readonly string _repPageEndpoint;
        private readonly string _dictRepEndpoint;
        private readonly string _repDetailsEndpoint;
        private readonly string _repWorkflowPath;


        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public EquipmentApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
        {
            _http = http;
            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _pageEndpoint = NormalizeRelative(
                configLoader.GetApiPath("quality.page", "/pda/dev/inspectTask/pageQuery"), servicePath);
            _dictEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.dictList", "/pda/dev/inspectTask/getDictList"), servicePath);
            _detailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.detailList", "/pda/dev/inspectTask/detail"), servicePath);
            _workflowPath = NormalizeRelative(
            configLoader.GetApiPath("quality.workflow", "/pda/dev/inspectTask/getWorkflow"),
            servicePath);
            _mainPageEndpoint = NormalizeRelative(
                configLoader.GetApiPath("quality.mainpage", "/pda/dev/upkeepTask/pageQuery"), servicePath);
            _dictMainEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.maindictList", "/pda/dev/upkeepTask/getDictList"), servicePath);
            _mainDetailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.maindetailList", "/pda/dev/upkeepTask/detail"), servicePath);
            _mainWorkflowPath = NormalizeRelative(
            configLoader.GetApiPath("quality.mainworkflow", "/pda/dev/upkeepTask/getWorkflow"),
            servicePath);
            _repPageEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.reppage", "/pda/dev/maintainWorkOrder/pageQuery"), servicePath);
            _dictRepEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.repdictList", "/pda/dev/maintainWorkOrder/getDictList"), servicePath);
            _repDetailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.repdetailList", "/pda/dev/maintainWorkOrder/detail"), servicePath);
            _repWorkflowPath = NormalizeRelative(
            configLoader.GetApiPath("quality.repworkflow", "/pda/dev/maintainWorkOrder/getWorkflow"),
            servicePath);
            _auth = auth;
        }
        // ===== 公共工具 =====
        private static string BuildFullUrl(Uri? baseAddress, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("url 不能为空", nameof(url));

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            if (baseAddress is null)
                throw new InvalidOperationException("HttpClient.BaseAddress 未配置");

            var baseUrl = baseAddress.AbsoluteUri;
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            return baseUrl + url.TrimStart('/');
        }

        private static string BuildQuery(IDictionary<string, string> p)
            => string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        private static string NormalizeRelative(string? endpoint, string servicePath)
        {
            var ep = (endpoint ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(ep)) return "/";

            if (ep.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                ep.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return ep;

            if (string.IsNullOrWhiteSpace(servicePath)) servicePath = "/";
            if (!servicePath.StartsWith("/")) servicePath = "/" + servicePath;
            servicePath = servicePath.TrimEnd('/');

            if (!string.IsNullOrEmpty(servicePath) &&
                servicePath != "/" &&
                ep.StartsWith(servicePath + "/", StringComparison.OrdinalIgnoreCase))
            {
                ep = ep[servicePath.Length..];
            }

            if (!ep.StartsWith("/")) ep = "/" + ep;
            return ep;
        }

        // ===== 方法实现 =====
        public async Task<PageResponeResult<InspectionRecordDto>> PageQueryAsync(
             int pageNo,
             int pageSize,
             string? inspectNo,
             string? createdTimeBegin,
             string? createdTimeEnd,
             string? inspectStatus,
             bool searchCount,
             CancellationToken ct = default)
        {
            // 1) 组装查询参数（仅在有值时加入）
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(inspectNo)) p["inspectNo"] = inspectNo!.Trim();
            if (!string.IsNullOrWhiteSpace(createdTimeBegin)) p["planInspectTimeBegin"] = createdTimeBegin!;
            if (!string.IsNullOrWhiteSpace(createdTimeEnd)) p["planInspectTimeEnd"] = createdTimeEnd!;
            if (!string.IsNullOrWhiteSpace(inspectStatus)) p["inspectStatus"] = inspectStatus!;

            // 2) 拼接 URL（与现有工具方法保持一致）
            var url = _pageEndpoint + "?" + BuildQuery(p);                  // BuildQuery 会做 UrlEncode
            var full = BuildFullUrl(_http.BaseAddress, url);

            // 3) 发送 GET
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            // 4) 非 2xx -> 返回一个失败的包装
            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<InspectionRecordDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            // 5) 反序列化（大小写不敏感）
            return JsonSerializer.Deserialize<PageResponeResult<InspectionRecordDto>>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new PageResponeResult<InspectionRecordDto> { success = false, message = "Empty body" };
        }


        public async Task<DictInspection> GetInspectionDictsAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _dictEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            var dto = JsonSerializer.Deserialize<DictResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var all = dto?.result ?? new List<DictField>();
            var inspectStatus = all.FirstOrDefault(f =>
       string.Equals(f.field, "inspectStatus", StringComparison.OrdinalIgnoreCase))
       ?.dictItems ?? new List<DictItem>();

            return new DictInspection { InspectStatus = inspectStatus };
        }
        // Services/InspectionApi.cs 追加方法（沿用你 BuildQuery/BuildFullUrl 风格）
        public async Task<ApiResp<InspectionDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default)
        {
            var url = _detailsEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<InspectionDetailDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<InspectionDetailDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new ApiResp<InspectionDetailDto> { success = false, message = "Empty body" };
        }

        
        public async Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _workflowPath) + "?id=" + Uri.EscapeDataString(id ?? "");
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<List<InspectWorkflowNode>>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };

            return System.Text.Json.JsonSerializer.Deserialize<ApiResp<List<InspectWorkflowNode>>>(
                       body,
                       new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new ApiResp<List<InspectWorkflowNode>> { success = false, message = "Empty body" };
        }


        public async Task<PageResponeResult<MaintenanceRecordDto>> MainPageQueryAsync(
             int pageNo,
             int pageSize,
             string? upkeepNo,
             string? planUpkeepTimeBegin,
             string? planUpkeepTimeEnd,
             string? upkeepStatus,
             bool searchCount,
             CancellationToken ct = default)
        {
            // 1) 组装查询参数（仅在有值时加入）
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(upkeepNo)) p["upkeepNo"] = upkeepNo!.Trim();
            if (!string.IsNullOrWhiteSpace(planUpkeepTimeBegin)) p["planUpkeepTimeBegin"] = planUpkeepTimeBegin!;
            if (!string.IsNullOrWhiteSpace(planUpkeepTimeEnd)) p["planUpkeepTimeEnd"] = planUpkeepTimeEnd!;
            if (!string.IsNullOrWhiteSpace(upkeepStatus)) p["upkeepStatus"] = upkeepStatus!;

            // 2) 拼接 URL（与现有工具方法保持一致）
            var url = _mainPageEndpoint + "?" + BuildQuery(p);                  // BuildQuery 会做 UrlEncode
            var full = BuildFullUrl(_http.BaseAddress, url);

            // 3) 发送 GET
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            // 4) 非 2xx -> 返回一个失败的包装
            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<MaintenanceRecordDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            // 5) 反序列化（大小写不敏感）
            return JsonSerializer.Deserialize<PageResponeResult<MaintenanceRecordDto>>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new PageResponeResult<MaintenanceRecordDto> { success = false, message = "Empty body" };
        }


        public async Task<DictMaintenance> GetMainDictsAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _dictMainEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            var dto = JsonSerializer.Deserialize<DictResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var all = dto?.result ?? new List<DictField>();
            var maintenanceStatus = all.FirstOrDefault(f =>
       string.Equals(f.field, "upkeepStatus", StringComparison.OrdinalIgnoreCase))
       ?.dictItems ?? new List<DictItem>();

            return new DictMaintenance { MaintenanceStatus = maintenanceStatus };
        }


        public async Task<ApiResp<MaintenanceDetailDto>?> GetMainDetailAsync(string id, CancellationToken ct = default)
        {
            var url = _mainDetailsEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<MaintenanceDetailDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<MaintenanceDetailDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new ApiResp<MaintenanceDetailDto> { success = false, message = "Empty body" };
        }


        public async Task<ApiResp<List<MaintenanceWorkflowNode>>> GetMainWorkflowAsync(string id, CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _mainWorkflowPath) + "?id=" + Uri.EscapeDataString(id ?? "");
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<List<MaintenanceWorkflowNode>>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };

            return System.Text.Json.JsonSerializer.Deserialize<ApiResp<List<MaintenanceWorkflowNode>>>(
                       body,
                       new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new ApiResp<List<MaintenanceWorkflowNode>> { success = false, message = "Empty body" };
        }

        public async Task<PageResponeResult<RepairRecordDto>> RepairPageQueryAsync(
           int pageNo,
           int pageSize,
           string? workOrderNo,
           string? submitTimeBegin,
           string? submitTimeEnd,
           string? auditStatus,
           bool searchCount,
           CancellationToken ct = default)
        {
            // 1) 组装查询参数（仅在有值时加入）
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(workOrderNo)) p["workOrderNo"] = workOrderNo!.Trim();
            if (!string.IsNullOrWhiteSpace(submitTimeBegin)) p["submitTimeBegin"] = submitTimeBegin!;
            if (!string.IsNullOrWhiteSpace(submitTimeEnd)) p["submitTimeEnd"] = submitTimeEnd!;
            if (!string.IsNullOrWhiteSpace(auditStatus)) p["auditStatus"] = auditStatus!;

            // 2) 拼接 URL（与现有工具方法保持一致）
            var url = _repPageEndpoint + "?" + BuildQuery(p);                  // BuildQuery 会做 UrlEncode
            var full = BuildFullUrl(_http.BaseAddress, url);

            // 3) 发送 GET
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            // 4) 非 2xx -> 返回一个失败的包装
            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<RepairRecordDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            // 5) 反序列化（大小写不敏感）
            return JsonSerializer.Deserialize<PageResponeResult<RepairRecordDto>>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new PageResponeResult<RepairRecordDto> { success = false, message = "Empty body" };
        }


        public async Task<DictRepair> GetRepairDictsAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _dictRepEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            var dto = JsonSerializer.Deserialize<DictResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var all = dto?.result ?? new List<DictField>();
            var auditStatus = all.FirstOrDefault(f =>
       string.Equals(f.field, "auditStatus", StringComparison.OrdinalIgnoreCase))
       ?.dictItems ?? new List<DictItem>();
            var urgent = all.FirstOrDefault(f =>
      string.Equals(f.field, "urgent", StringComparison.OrdinalIgnoreCase))
      ?.dictItems ?? new List<DictItem>();
            var maintainType = all.FirstOrDefault(f =>
      string.Equals(f.field, "maintainType", StringComparison.OrdinalIgnoreCase))
      ?.dictItems ?? new List<DictItem>();

            return new DictRepair { AuditStatus = auditStatus, Urgent = urgent , MaintainType = maintainType };
        }


        public async Task<ApiResp<RepairDetailDto>?> GetRepairDetailAsync(string id, CancellationToken ct = default)
        {
            var url = _repDetailsEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<RepairDetailDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<RepairDetailDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new ApiResp<RepairDetailDto> { success = false, message = "Empty body" };
        }


        public async Task<ApiResp<List<RepairWorkflowNode>>> GetRepairWorkflowAsync(string id, CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _repWorkflowPath) + "?id=" + Uri.EscapeDataString(id ?? "");
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<List<RepairWorkflowNode>>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };

            return System.Text.Json.JsonSerializer.Deserialize<ApiResp<List<RepairWorkflowNode>>>(
                       body,
                       new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new ApiResp<List<RepairWorkflowNode>> { success = false, message = "Empty body" };
        }
    }
}