using GoogleGson;
using IndustrialControlMAUI.Models;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IWorkOrderApi
    {
        Task<WorkOrderPageResult> GetWorkOrdersAsync(WorkOrderQuery q, CancellationToken ct = default);
        Task<DictBundle> GetWorkOrderDictsAsync(CancellationToken ct = default);
        Task<WorkflowResp?> GetWorkOrderWorkflowAsync(string id, CancellationToken ct = default);
        Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(
         string? workOrderNo,
         IEnumerable<string>? auditStatusList,   // ★ 改为数组
         string? processCode,
         DateTime? createdTimeStart = null,
         DateTime? createdTimeEnd = null,
         string? materialName = null,
         string? platPlanNo = null,
         string? schemeNo = null,
         bool? searchCount = null,      // 是否计算总记录数（可选）
         int pageNo = 1,
         int pageSize = 50,
         CancellationToken ct = default);
        Task<ApiResp<List<FieldDict>>> GetWorkProcessTaskDictListAsync(CancellationToken ct = default);
        Task<ApiResp<List<ProcessInfo>>> GetProcessInfoListAsync(CancellationToken ct = default);
        Task<ApiResp<WorkProcessTaskDetail>> GetWorkProcessTaskDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<ShiftInfo>>> GetShiftOptionsAsync(
        string factoryCode,
        string workshopsCode,
        CancellationToken ct = default);
        Task<ApiResp<List<DevicesInfo>>> GetDeviceOptionsAsync(
        string factoryCode,
        string processCode,
        CancellationToken ct = default);
        Task<SimpleOk> UpdateWorkProcessTaskAsync(
            string id, string? productionMachine, string? productionMachineName, int? taskReportedQty, string? teamCode, string? teamName, int? workHours, string? startDate, string? endDate, CancellationToken ct = default);
        Task<ApiResp<bool>> StartWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> CompleteWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> PauseWorkAsync(string processCode, string workOrderNo, string? memo = null);

        Task<ApiResp<bool>> AddWorkProcessTaskMaterialInputAsync(AddWorkProcessTaskMaterialInputReq req);

        Task<ApiResp<bool>> AddWorkProcessTaskProductOutputAsync(AddWorkProcessTaskProductOutputReq req);

        Task<PageResp<MaterialAuRecord>?> PageWorkProcessTaskMaterialInputs(
                string factoryCode,          // 工厂编码（必填）
                string processCode,          // 工序编码（必填）
                string workOrderNo,          // 工单号（必填）
                int pageNo = 1,              // 当前页（必填）
                int pageSize = 50,           // 每页条数（必填）
                string? materialCode = null, // 物料编码（可选）
                bool? searchCount = null,    // 是否计算总记录数（可选）
                CancellationToken ct = default);

        Task<PageResp<OutputAuRecord>?> PageWorkProcessTaskOutputs(
                string factoryCode,          // 工厂编码（必填）
                string processCode,          // 工序编码（必填）
                string workOrderNo,          // 工单号（必填）
                int pageNo = 1,              // 当前页（必填）
                int pageSize = 50,           // 每页条数（必填）
                string? materialCode = null, // 物料编码（可选）
                bool? searchCount = null,    // 是否计算总记录数（可选）
                CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteWorkProcessTaskMaterialInputAsync(
    string id,
    CancellationToken ct = default);

        Task<ApiResp<bool>> DeleteWorkProcessTaskOutputAsync(
   string id,
   CancellationToken ct = default);
        Task<ApiResp<bool>> EditWorkProcessTaskMaterialInputAsync(
            string id,
            decimal? qty = null,
            string? memo = null,
            string? rawMaterialProductionDate = null,
            CancellationToken ct = default);
    }


    // ===================== 实现 =====================
    public class WorkOrderApi : IWorkOrderApi
    {
        private readonly HttpClient _http;
        private readonly string _pageEndpoint;
        private readonly string _workflowEndpoint;
        private readonly string _processTasksEndpoint;
        private readonly string _dictEndpoint;
        private readonly string _workProcessTaskDictEndpoint;
        private readonly string _processInfoListEndpoint;
        private readonly string _workProcessTaskDetailEndpoint;
        private readonly string _shiftEndpoint;
        private readonly string _deviceEndpoint;
        private readonly string _updateTeamEndpoint;
        private readonly string _startworkEndpoint;
        private readonly string _completeworkEndpoint;
        private readonly string _pauseworkEndpoint;
        private readonly string _addMaterialEndpoint;
        private readonly string _addOutputEndpoint;
        private readonly string _autMaterialListEndpoint;
        private readonly string _autOutputListEndpoint;
        private readonly string _deleteWorkProcessTaskMaterialInputEndpoint;
        private readonly string _deleteWorkProcessTaskOutputEndpoint;
        private readonly string _editWorkProcessTaskMaterialInputEndpoint;
        private readonly string _editWorkProcessTaskOutputEndpoint;

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public WorkOrderApi(HttpClient http, IConfigLoader configLoader)
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
                configLoader.GetApiPath("workOrder.page", "/pda/pmsWorkOrder/pageWorkOrders"), servicePath);
            _workflowEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.workflow", "/pda/pmsWorkOrder/getWorkOrderWorkflow"), servicePath);
            _processTasksEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.processTasks", "/pda/pmsWorkOrder/pageWorkProcessTasks"), servicePath);
            _dictEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.dictList", "/pda/pmsWorkOrder/getWorkOrderDictList"), servicePath);
            _workProcessTaskDictEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.dictProcessList", "/pda/pmsWorkOrder/getWorkProcessTaskDictList"), servicePath);
            _processInfoListEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.ProcessList", "/pda/pmsWorkOrder/PmsProcessInfoList"), servicePath);
            _workProcessTaskDetailEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.ProcessDetail", "/pda/pmsWorkOrder/getWorkProcessTaskDetail"), servicePath);
            _shiftEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.shift", "/pda/pmsWorkOrder/getClassesListByWorkShopLine"), servicePath);
            _deviceEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.device", "/pda/pmsWorkOrder/getPmsEqptPointListByLineProcess"), servicePath);
            _updateTeamEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.updateWorkProcess", "/pda/pmsWorkOrder/editWorkProcessTask"), servicePath);
            _startworkEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.startwork", "/pda/pmsWorkOrder/workProcessTaskWorkStart"), servicePath);
            _completeworkEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.completework", "/pda/pmsWorkOrder/workProcessTaskWorkChangeComplete"), servicePath);
            _pauseworkEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.pausework", "/pda/pmsWorkOrder/workProcessTaskWorkChange"), servicePath);
            _addMaterialEndpoint = NormalizeRelative(
                configLoader.GetApiPath("workOrder.addMaterial", "/pda/pmsWorkOrder/addWorkProcessTaskMaterialInput"), servicePath);
            _addOutputEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.addOutput", "/pda/pmsWorkOrder/addWorkProcessTaskMaterialOutput"), servicePath);
            _autMaterialListEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.autMaterialList", "/pda/pmsWorkOrder/pageWorkProcessTaskMaterialInputs"), servicePath);
            _autOutputListEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.autOutputList", "/pda/pmsWorkOrder/pageWorkProcessTaskMaterialOutputs"), servicePath);
            _deleteWorkProcessTaskMaterialInputEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.deleteWorkProcessTaskMaterialInput", "/pda/pmsWorkOrder/deleteWorkProcessTaskMaterialInput"), servicePath);
            _deleteWorkProcessTaskOutputEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.deleteWorkProcessTaskOutput", "/pda/pmsWorkOrder/deleteWorkProcessTaskMaterialOutput"), servicePath);
            _editWorkProcessTaskMaterialInputEndpoint = NormalizeRelative(
                    configLoader.GetApiPath("workOrder.editWorkProcessTaskMaterialInput", "/pda/pmsWorkOrder/editWorkProcessTaskMaterialInput"), servicePath);
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
        public async Task<WorkOrderPageResult> GetWorkOrdersAsync(WorkOrderQuery q, CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = q.PageNo.ToString(),
                ["pageSize"] = q.PageSize.ToString()
            };
            if (q.CreatedTimeStart.HasValue) p["createdTimeStart"] = q.CreatedTimeStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (q.CreatedTimeEnd.HasValue) p["createdTimeEnd"] = q.CreatedTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrWhiteSpace(q.WorkOrderNo)) p["workOrderNo"] = q.WorkOrderNo!.Trim();
            if (!string.IsNullOrWhiteSpace(q.MaterialName)) p["materialName"] = q.MaterialName!.Trim();

            var url = _pageEndpoint + "?" + BuildQuery(p);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);

            if (!httpResp.IsSuccessStatusCode)
                return new WorkOrderPageResult { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            return JsonSerializer.Deserialize<WorkOrderPageResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new WorkOrderPageResult();
        }

        public async Task<WorkflowResp?> GetWorkOrderWorkflowAsync(string id, CancellationToken ct = default)
        {
            var url = _workflowEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);

            if (!httpResp.IsSuccessStatusCode)
                return new WorkflowResp { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            return JsonSerializer.Deserialize<WorkflowResp>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new WorkflowResp();
        }

        public async Task<PageResp<ProcessTask>?> PageWorkProcessTasksAsync(
    string? workOrderNo,
    IEnumerable<string>? auditStatusList,   // ★ 改为数组
    string? processCode,
    DateTime? createdTimeStart = null,
    DateTime? createdTimeEnd = null,
    string? materialName = null,
    string? platPlanNo = null,
    string? schemeNo = null,
    bool? searchCount = null,      // 是否计算总记录数（可选）
    int pageNo = 1,
    int pageSize = 50,
    CancellationToken ct = default)
        {
            // 用 pair 列表承载，便于添加“重复 key”
            var pairs = new List<KeyValuePair<string, string>>
    {
        new("pageNo",   pageNo.ToString()),
        new("pageSize", pageSize.ToString())
    };

            void AddIf(string key, string? val)
            {
                if (!string.IsNullOrWhiteSpace(val))
                    pairs.Add(new KeyValuePair<string, string>(key, val.Trim()));
            }

            // 普通参数
            AddIf("workOrderNo", workOrderNo);
            AddIf("processCode", processCode);
            AddIf("materialName", materialName);
            AddIf("platPlanNo", platPlanNo);
            AddIf("schemeNo", schemeNo);

            if (createdTimeStart.HasValue)
                pairs.Add(new("createdTimeStart", createdTimeStart.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            if (createdTimeEnd.HasValue)
                pairs.Add(new("createdTimeEnd", createdTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss")));

            if (searchCount.HasValue)
                pairs.Add(new("searchCount", searchCount.Value ? "true" : "false"));

            // ★ 数组参数：auditStatusList（0/1/2…）
            if (auditStatusList != null)
            {
                foreach (var s in auditStatusList)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        pairs.Add(new KeyValuePair<string, string>("auditStatusList", s.Trim()));
                }
            }

            // 生成 querystring（key 重复）
            string BuildQueryMulti(IEnumerable<KeyValuePair<string, string>> kvs)
                => string.Join("&", kvs.Select(kv =>
                       $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            var url = _processTasksEndpoint + "?" + BuildQueryMulti(pairs);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);

            if (!httpResp.IsSuccessStatusCode)
                return new PageResp<ProcessTask> { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            return JsonSerializer.Deserialize<PageResp<ProcessTask>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new PageResp<ProcessTask>();
        }


        public async Task<DictBundle> GetWorkOrderDictsAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _dictEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            var dto = JsonSerializer.Deserialize<DictResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var all = dto?.result ?? new List<DictField>();
            var audit = all.FirstOrDefault(f => string.Equals(f.field, "auditStatus", StringComparison.OrdinalIgnoreCase))
                          ?.dictItems ?? new List<DictItem>();
            var urgent = all.FirstOrDefault(f => string.Equals(f.field, "urgent", StringComparison.OrdinalIgnoreCase))
                          ?.dictItems ?? new List<DictItem>();

            return new DictBundle { AuditStatus = audit, Urgent = urgent };
        }

        public async Task<ApiResp<List<FieldDict>>> GetWorkProcessTaskDictListAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _workProcessTaskDictEndpoint);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<List<FieldDict>>>(stream, _json, ct);
            return data ?? new ApiResp<List<FieldDict>> { success = false, message = "empty response", result = new List<FieldDict>() };
        }
        public async Task<ApiResp<List<ProcessInfo>>> GetProcessInfoListAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _processInfoListEndpoint);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<List<ProcessInfo>>>(stream, _json, ct);
            return data ?? new ApiResp<List<ProcessInfo>> { success = false, message = "empty response", result = new List<ProcessInfo>() };
        }
        public async Task<ApiResp<WorkProcessTaskDetail>> GetWorkProcessTaskDetailAsync(string id, CancellationToken ct = default)
        {
            var url = _workProcessTaskDetailEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<WorkProcessTaskDetail>>(stream, _json, ct);
            return data ?? new ApiResp<WorkProcessTaskDetail> { success = false, message = "empty response" };
        }

        public async Task<ApiResp<List<ShiftInfo>>> GetShiftOptionsAsync(
    string factoryCode,
    string workshopsCode,
    CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _shiftEndpoint);
            var query = BuildQuery(new Dictionary<string, string?>
            {
                ["factoryCode"] = factoryCode,
                ["workshopsCode"] = workshopsCode
            });

            var url = string.IsNullOrEmpty(query) ? full : $"{full}?{query}";
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<List<ShiftInfo>>>(stream, _json, ct);
            return data ?? new ApiResp<List<ShiftInfo>> { success = false, message = "empty response", result = new List<ShiftInfo>() };
        }

        public async Task<ApiResp<List<DevicesInfo>>> GetDeviceOptionsAsync(
            string factoryCode,
            string processCode,
            CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _deviceEndpoint);
            var query = BuildQuery(new Dictionary<string, string?>
            {
                ["factoryCode"] = factoryCode,
                ["processCode"] = processCode
            });

            var url = string.IsNullOrEmpty(query) ? full : $"{full}?{query}";
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<List<DevicesInfo>>>(stream, _json, ct);
            return data ?? new ApiResp<List<DevicesInfo>> { success = false, message = "empty response", result = new List<DevicesInfo>() };
        }

        private static string BuildQuery(Dictionary<string, string?> parameters)
        {
            return string.Join("&",
                parameters
                    .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                    .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
        }

        public async Task<SimpleOk> UpdateWorkProcessTaskAsync(
    WorkProcessTaskTeamUpdateReq req,
    CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _updateTeamEndpoint);
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var body = JsonSerializer.Serialize(req, options);
            using var msg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };

            using var res = await _http.SendAsync(msg, ct);
            var txt = await res.Content.ReadAsStringAsync(ct);

            var dto = JsonSerializer.Deserialize<ConfirmResp>(
                txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = dto?.success == true && dto.result == true;
            return new SimpleOk(ok, dto?.message ?? (ok ? "成功" : "失败"));
        }

        // 更新班次
        public Task<SimpleOk> UpdateWorkProcessTaskAsync(
            string id, string? productionMachine, string? productionMachineName, int? taskReportedQty, string? teamCode, string? teamName, int? workHours, string? startDate, string? endDate, CancellationToken ct = default)
        {
            var payload = 
        new WorkProcessTaskTeamUpdateReq { id = id, productionMachine = productionMachine, productionMachineName = productionMachineName,taskReportedQty = taskReportedQty,teamCode= teamCode,teamName=teamName,workHours = workHours,startDate= startDate,endDate = endDate };
            return UpdateWorkProcessTaskAsync(payload, ct);
        }

        public async Task<ApiResp<bool>> StartWorkAsync(string processCode, string workOrderNo, string? memo = null)
        {
            var body = new
            {
                memo = memo ?? "",
                processCode,
                workOrderNo
            };
            var full = BuildFullUrl(_http.BaseAddress, _startworkEndpoint);
            var resp = await _http.PostAsJsonAsync(full, body);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResp<bool>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ApiResp<bool> { success = false, message = "反序列化失败" };
        }

        public async Task<ApiResp<bool>> CompleteWorkAsync(string processCode, string workOrderNo, string? memo = null)
        {
            var body = new
            {
                memo = memo ?? "",
                processCode,
                workOrderNo
            };
            var full = BuildFullUrl(_http.BaseAddress, _completeworkEndpoint);
            var resp = await _http.PostAsJsonAsync(full, body);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResp<bool>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ApiResp<bool> { success = false, message = "反序列化失败" };
        }

        public async Task<ApiResp<bool>> PauseWorkAsync(string processCode, string workOrderNo, string? memo = null)
        {
            var body = new
            {
                memo = memo ?? "",
                processCode,
                workOrderNo
            };
            var full = BuildFullUrl(_http.BaseAddress, _pauseworkEndpoint);
            var resp = await _http.PostAsJsonAsync(full, body);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResp<bool>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ApiResp<bool> { success = false, message = "反序列化失败" };
        }

        public async Task<ApiResp<bool>> AddWorkProcessTaskMaterialInputAsync(AddWorkProcessTaskMaterialInputReq req)
        {
            var full = BuildFullUrl(_http.BaseAddress, _addMaterialEndpoint);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using var resp = await _http.PostAsJsonAsync(full, req, options);
            if (!resp.IsSuccessStatusCode)
            {
                return new ApiResp<bool>
                {
                    success = false,
                    message = $"HTTP错误 {resp.StatusCode}"
                };
            }

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResp<bool>>(json, options);
            return result ?? new ApiResp<bool> { success = false, message = "解析响应失败" };
        }

        public async Task<ApiResp<bool>> AddWorkProcessTaskProductOutputAsync(AddWorkProcessTaskProductOutputReq req)
        {
            var full = BuildFullUrl(_http.BaseAddress, _addOutputEndpoint);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using var resp = await _http.PostAsJsonAsync(full, req, options);
            if (!resp.IsSuccessStatusCode)
            {
                return new ApiResp<bool>
                {
                    success = false,
                    message = $"HTTP错误 {resp.StatusCode}"
                };
            }

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResp<bool>>(json, options);
            return result ?? new ApiResp<bool> { success = false, message = "解析响应失败" };
        }

        //实际投料列表
        public async Task<PageResp<MaterialAuRecord>?> PageWorkProcessTaskMaterialInputs(
            string factoryCode,          // 工厂编码（必填）
            string processCode,          // 工序编码（必填）
            string workOrderNo,          // 工单号（必填）
            int pageNo = 1,              // 当前页（必填）
            int pageSize = 50,           // 每页条数（必填）
            string? materialCode = null, // 物料编码（可选）
            bool? searchCount = null,    // 是否计算总记录数（可选）
            CancellationToken ct = default)
        {
            // 基础校验（避免发出无效请求）
            if (string.IsNullOrWhiteSpace(factoryCode))
                throw new ArgumentException("factoryCode 不能为空", nameof(factoryCode));
            if (string.IsNullOrWhiteSpace(processCode))
                throw new ArgumentException("processCode 不能为空", nameof(processCode));
            if (string.IsNullOrWhiteSpace(workOrderNo))
                throw new ArgumentException("workOrderNo 不能为空", nameof(workOrderNo));
            if (pageNo <= 0) pageNo = 1;
            if (pageSize <= 0) pageSize = 50;

            // 由于需要 application/x-www-form-urlencoded 的 query 形式，直接拼接查询串即可
            var pairs = new List<KeyValuePair<string, string>>
    {
        new("factoryCode", factoryCode.Trim()),
        new("pageNo",      pageNo.ToString()),
        new("pageSize",    pageSize.ToString()),
        new("processCode", processCode.Trim()),
        new("workOrderNo", workOrderNo.Trim())
    };

            if (!string.IsNullOrWhiteSpace(materialCode))
                pairs.Add(new("materialCode", materialCode.Trim()));
            if (searchCount.HasValue)
                pairs.Add(new("searchCount", searchCount.Value ? "true" : "false"));

            string BuildQueryMulti(IEnumerable<KeyValuePair<string, string>> kvs)
                => string.Join("&", kvs.Select(kv =>
                       $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            // 该 endpoint 应对应文档中的：/normalService/pda/pmsWorkOrder/pageWorkProcessTaskMaterialInputs
            var url = _autMaterialListEndpoint + "?" + BuildQueryMulti(pairs);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);

            if (!httpResp.IsSuccessStatusCode)
                return new PageResp<MaterialAuRecord> { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            // 大小写不敏感，兼容后端返回
            return JsonSerializer.Deserialize<PageResp<MaterialAuRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new PageResp<MaterialAuRecord>();
        }
        //实际产出列表
        public async Task<PageResp<OutputAuRecord>?> PageWorkProcessTaskOutputs(
            string factoryCode,          // 工厂编码（必填）
            string processCode,          // 工序编码（必填）
            string workOrderNo,          // 工单号（必填）
            int pageNo = 1,              // 当前页（必填）
            int pageSize = 50,           // 每页条数（必填）
            string? materialCode = null, // 物料编码（可选）
            bool? searchCount = null,    // 是否计算总记录数（可选）
            CancellationToken ct = default)
        {
            // 基础校验（避免发出无效请求）
            if (string.IsNullOrWhiteSpace(factoryCode))
                throw new ArgumentException("factoryCode 不能为空", nameof(factoryCode));
            if (string.IsNullOrWhiteSpace(processCode))
                throw new ArgumentException("processCode 不能为空", nameof(processCode));
            if (string.IsNullOrWhiteSpace(workOrderNo))
                throw new ArgumentException("workOrderNo 不能为空", nameof(workOrderNo));
            if (pageNo <= 0) pageNo = 1;
            if (pageSize <= 0) pageSize = 50;

            // 由于需要 application/x-www-form-urlencoded 的 query 形式，直接拼接查询串即可
            var pairs = new List<KeyValuePair<string, string>>
    {
        new("factoryCode", factoryCode.Trim()),
        new("pageNo",      pageNo.ToString()),
        new("pageSize",    pageSize.ToString()),
        new("processCode", processCode.Trim()),
        new("workOrderNo", workOrderNo.Trim())
    };

            if (!string.IsNullOrWhiteSpace(materialCode))
                pairs.Add(new("materialCode", materialCode.Trim()));
            if (searchCount.HasValue)
                pairs.Add(new("searchCount", searchCount.Value ? "true" : "false"));

            string BuildQueryMulti(IEnumerable<KeyValuePair<string, string>> kvs)
                => string.Join("&", kvs.Select(kv =>
                       $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            // 该 endpoint 应对应文档中的：/normalService/pda/pmsWorkOrder/pageWorkProcessTaskMaterialInputs
            var url = _autOutputListEndpoint + "?" + BuildQueryMulti(pairs);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var httpResp = await _http.SendAsync(req, ct);
            var json = await httpResp.Content.ReadAsStringAsync(ct);

            if (!httpResp.IsSuccessStatusCode)
                return new PageResp<OutputAuRecord> { success = false, message = $"HTTP {(int)httpResp.StatusCode}" };

            // 大小写不敏感，兼容后端返回
            return JsonSerializer.Deserialize<PageResp<OutputAuRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new PageResp<OutputAuRecord>();
        }
        //单条删除投料
        public async Task<ApiResp<bool>> DeleteWorkProcessTaskMaterialInputAsync(
    string id,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ApiResp<bool> { success = false, message = "id 不能为空" };
            
            var full = BuildFullUrl(_http.BaseAddress, _deleteWorkProcessTaskMaterialInputEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new DeleteWorkProcessTaskMaterialInputReq { id = id }, _json),
                    Encoding.UTF8,
                    "application/json")
            };

            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<bool>>(stream, _json, ct);

            return data ?? new ApiResp<bool> { success = false, message = "empty response" };
        }
        //单条删除产出
        public async Task<ApiResp<bool>> DeleteWorkProcessTaskOutputAsync(
   string id,
   CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ApiResp<bool> { success = false, message = "id 不能为空" };

            var full = BuildFullUrl(_http.BaseAddress, _deleteWorkProcessTaskOutputEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new DeleteWorkProcessTaskMaterialInputReq { id = id }, _json),
                    Encoding.UTF8,
                    "application/json")
            };

            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<bool>>(stream, _json, ct);

            return data ?? new ApiResp<bool> { success = false, message = "empty response" };
        }

        // 编辑投料记录
        public async Task<ApiResp<bool>> EditWorkProcessTaskMaterialInputAsync(
            string id,
            decimal? qty = null,
            string? memo = null,
            string? rawMaterialProductionDate = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ApiResp<bool> { success = false, message = "id 不能为空" };

            // 例如：_editWorkProcessTaskMaterialInputEndpoint = "/normalService/pda/pmsWorkOrder/editWorkProcessTaskMaterialInput"
            var full = BuildFullUrl(_http.BaseAddress, _editWorkProcessTaskMaterialInputEndpoint);

            var body = new EditWorkProcessTaskMaterialInputReq
            {
                id = id.Trim(),
                memo = string.IsNullOrWhiteSpace(memo) ? null : memo!.Trim(),
                qty = qty,
                rawMaterialProductionDate = string.IsNullOrWhiteSpace(rawMaterialProductionDate)
                    ? null
                    : rawMaterialProductionDate!.Trim()
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
            {
                // _json 建议包含：DefaultIgnoreCondition = WhenWritingNull，避免把 null 发给后端
                Content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json")
            };

            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<ApiResp<bool>>(stream, _json, ct);

            return data ?? new ApiResp<bool> { success = false, message = "empty response" };
        }

        
    }

}