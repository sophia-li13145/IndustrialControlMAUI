using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;
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
        Task<ApiResp<InspectDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteInspectAttachmentAsync(string id, CancellationToken ct = default);
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
        Task<ApiResp<bool>> DeleteMainAttachmentAsync(string id, CancellationToken ct = default);
        Task<DictRepair> GetRepairDictsAsync(CancellationToken ct = default);
        Task<ApiResp<RepairDetailDto>?> GetRepairDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<RepairWorkflowNode>>> GetRepairWorkflowAsync(string id, CancellationToken ct = default);

        Task<ApiResp<bool?>> ExecuteSaveAsync(InspectDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(InspectDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteMainSaveAsync(MaintenanceDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteMainCompleteAsync(MaintenanceDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteRepairSaveAsync(RepairDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteRepairCompleteAsync(RepairDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteRepairAttachmentAsync(string id, CancellationToken ct = default);
        Task<PageResponeResult<MaintenanceReportDto>> ESPageQueryAsync(
            int pageNo,
         int pageSize,
         string? maintainNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? auditStatus,
         bool searchCount,
         CancellationToken ct = default);
        Task<DictExcept> GetExceptDictsAsync(CancellationToken ct = default);
        Task<ApiResp<MaintenanceReportDto>?> GetExceptDetailAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<ExceptWorkflowNode>>> GetExceptWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteExceptSaveAsync(MaintenanceReportDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> SubmitExceptAsync(MaintenanceReportDto payload, CancellationToken ct = default);

    }


    // ===================== 实现 =====================
    public class EquipmentApi : IEquipmentApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;
        private readonly IAttachmentApi _attachmentApi;
        private readonly string _pageEndpoint;
        private readonly string _dictEndpoint;
        private readonly string _detailsEndpoint;
        private readonly string _workflowPath;
        private readonly string _executeSavePath;
        private readonly string _executeCompletePath;
        private readonly string _deleteAttPath;

        private readonly string _mainPageEndpoint;
        private readonly string _dictMainEndpoint;
        private readonly string _mainDetailsEndpoint;
        private readonly string _mainWorkflowPath;
        private readonly string _mainexecuteSavePath;
        private readonly string _mainexecuteCompletePath;
        private readonly string _maindeleteAttPath;

        private readonly string _repPageEndpoint;
        private readonly string _dictRepEndpoint;
        private readonly string _repDetailsEndpoint;
        private readonly string _repWorkflowPath;
        private readonly string _repexecuteSavePath;
        private readonly string _repexecuteCompletePath;
        private readonly string _repdeleteAttPath;

        private readonly string _exceptPageEndpoint;
        private readonly string _dictESEndpoint;
        private readonly string _exceptDetailsEndpoint;
        private readonly string _exceptWorkflowPath;



        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public EquipmentApi(HttpClient http, IConfigLoader configLoader, AuthState auth,IAttachmentApi attachmentApi)
        {
            _http = http;
            _auth = auth;
            _attachmentApi = attachmentApi;
            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // === 巡检 ===
            _pageEndpoint = NormalizeRelative(
                configLoader.GetApiPath("equipment.page", "/pda/dev/inspectTask/pageQuery"), servicePath);
            _dictEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.dictList", "/pda/dev/inspectTask/getDictList"), servicePath);
            _detailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.detailList", "/pda/dev/inspectTask/detail"), servicePath);
            _workflowPath = NormalizeRelative(
                configLoader.GetApiPath("equipment.workflow", "/pda/dev/inspectTask/getWorkflow"),
                servicePath);
            _executeSavePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.inspectexecuteSave", "/pda/dev/inspectTask/executeSave"), servicePath);
            _executeCompletePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.inspectexecuteComplete", "/pda/dev/inspectTask/executeCompleteInspection"), servicePath);
            _deleteAttPath = NormalizeRelative(
   configLoader.GetApiPath("equipment.deleteAtt", "/pda/dev/inspectTask/deleteAttachment"),
   servicePath);
            // === 保养 ===
            _mainPageEndpoint = NormalizeRelative(
                configLoader.GetApiPath("equipment.mainpage", "/pda/dev/upkeepTask/pageQuery"), servicePath);
            _dictMainEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.maindictList", "/pda/dev/upkeepTask/getDictList"), servicePath);
            _mainDetailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.maindetailList", "/pda/dev/upkeepTask/detail"), servicePath);
            _mainWorkflowPath = NormalizeRelative(
                configLoader.GetApiPath("equipment.mainworkflow", "/pda/dev/upkeepTask/getWorkflow"),
                servicePath);
            _mainexecuteSavePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.mainexecuteSave", "/pda/dev/upkeepTask/executeSave"), servicePath);
            _mainexecuteCompletePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.mainexecuteComplete", "/pda/dev/upkeepTask/executeCompleteUpkeep"), servicePath);
            _maindeleteAttPath = NormalizeRelative(
   configLoader.GetApiPath("equipment.maindeleteAtt", "/pda/dev/upkeepTask/deleteAttachment"),
   servicePath);
            // === 维修 ===
            _repPageEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.reppage", "/pda/dev/maintainWorkOrder/pageQuery"), servicePath);
            _dictRepEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.repdictList", "/pda/dev/maintainWorkOrder/getDictList"), servicePath);
            _repDetailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("equipment.repdetailList", "/pda/dev/maintainWorkOrder/detail"), servicePath);
            _repWorkflowPath = NormalizeRelative(
                configLoader.GetApiPath("equipment.repworkflow", "/pda/dev/maintainWorkOrder/getWorkflow"),
                servicePath);
            _repexecuteSavePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.repexecuteSave", "/pda/dev/maintainWorkOrder/executeSave"), servicePath);
            _repexecuteCompletePath = NormalizeRelative(
                configLoader.GetApiPath("equipment.repexecuteComplete", "/pda/dev/maintainWorkOrder/executeComplete"), servicePath);
            _repdeleteAttPath = NormalizeRelative(
        configLoader.GetApiPath("equipment.repdeleteAtt", "/pda/dev/maintainWorkOrder/deleteAttachment"),
        servicePath);
            //异常提报
            _exceptPageEndpoint = NormalizeRelative(
        configLoader.GetApiPath("equipment.exceptPage", "/pda/dev/maintainReport/pageQuery"),
        servicePath);
            _dictESEndpoint = NormalizeRelative(
        configLoader.GetApiPath("equipment.exceptDic", "/pda/dev/maintainReport/getDictList"),
        servicePath);
            _exceptDetailsEndpoint = NormalizeRelative(
              configLoader.GetApiPath("equipment.exceptDetails", "/pda/dev/maintainReport/detail"), servicePath);
            _exceptWorkflowPath = NormalizeRelative(
              configLoader.GetApiPath("equipment.exceptWorkflow", "/pda/dev/maintainReport/getWorkflow"), servicePath);
        }

        // ===================== 公共工具 =====================
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

        // ========== 抽取的通用方法：分页查询 ==========
        private async Task<PageResponeResult<T>> GetPageAsync<T>(
            string endpoint,
            IDictionary<string, string> queryParams,
            CancellationToken ct)
        {
            var url = endpoint + "?" + BuildQuery(queryParams);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<T>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<PageResponeResult<T>>(json, _json)
                   ?? new PageResponeResult<T> { success = false, message = "Empty body" };
        }

        // ========== 抽取的通用方法：字典查询 ==========
        private async Task<List<DictField>> GetDictFieldsAsync(string endpoint, CancellationToken ct)
        {
            var full = BuildFullUrl(_http.BaseAddress, endpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            var dto = JsonSerializer.Deserialize<DictResponse>(json, _json);
            return dto?.result ?? new List<DictField>();
        }

        // ========== 抽取的通用方法：GET ?id=xxx ==========
        private async Task<ApiResp<T>?> GetApiRespByIdAsync<T>(
            string endpoint,
            string id,
            CancellationToken ct)
        {
            var full = BuildFullUrl(_http.BaseAddress, endpoint) +
                       "?id=" + Uri.EscapeDataString(id ?? string.Empty);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<T>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<T>>(json, _json)
                   ?? new ApiResp<T> { success = false, message = "Empty body" };
        }

        // ========== 抽取的通用方法：POST JSON ==========
        private async Task<ApiResp<TResp>?> PostJsonAsync<TPayload, TResp>(
            string endpoint,
            TPayload payload,
            CancellationToken ct)
        {
            var url = BuildFullUrl(_http.BaseAddress, endpoint);
            var json = JsonSerializer.Serialize(payload, _json);

            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute))
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<TResp>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<TResp>>(body, _json)
                   ?? new ApiResp<TResp> { success = false, message = "Empty body" };
        }

        // ===================== 具体业务方法实现 =====================

        // ---------- 巡检列表 ----------
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
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(inspectNo)) p["inspectNo"] = inspectNo.Trim();
            if (!string.IsNullOrWhiteSpace(createdTimeBegin)) p["createdTimeStart"] = createdTimeBegin;
            if (!string.IsNullOrWhiteSpace(createdTimeEnd)) p["createdTimeEnd"] = createdTimeEnd;
            if (!string.IsNullOrWhiteSpace(inspectStatus)) p["inspectStatus"] = inspectStatus;

            return await GetPageAsync<InspectionRecordDto>(_pageEndpoint, p, ct);
        }

        // ---------- 巡检字典 ----------
        public async Task<DictInspection> GetInspectionDictsAsync(CancellationToken ct = default)
        {
            var all = await GetDictFieldsAsync(_dictEndpoint, ct);

            var inspectStatus = all.FirstOrDefault(f =>
                string.Equals(f.field, "inspectStatus", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();
            var inspectResult = all.FirstOrDefault(f =>
                string.Equals(f.field, "inspectResult", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            return new DictInspection
            {
                InspectStatus = inspectStatus
            };
        }

        // ---------- 巡检详情 ----------
        public Task<ApiResp<InspectDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<InspectDetailDto>(_detailsEndpoint, id, ct);

        // ---------- 巡检流程 ----------
        public Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<List<InspectWorkflowNode>>(_workflowPath, id, ct)!;

        // ---------- 保养列表 ----------
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
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(upkeepNo)) p["upkeepNo"] = upkeepNo.Trim();
            if (!string.IsNullOrWhiteSpace(planUpkeepTimeBegin)) p["planUpkeepTimeBegin"] = planUpkeepTimeBegin;
            if (!string.IsNullOrWhiteSpace(planUpkeepTimeEnd)) p["planUpkeepTimeEnd"] = planUpkeepTimeEnd;
            if (!string.IsNullOrWhiteSpace(upkeepStatus)) p["upkeepStatus"] = upkeepStatus;

            return await GetPageAsync<MaintenanceRecordDto>(_mainPageEndpoint, p, ct);
        }

        // ---------- 保养字典 ----------
        public async Task<DictMaintenance> GetMainDictsAsync(CancellationToken ct = default)
        {
            var all = await GetDictFieldsAsync(_dictMainEndpoint, ct);

            var maintenanceStatus = all.FirstOrDefault(f =>
                string.Equals(f.field, "upkeepStatus", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            var maintenanceResult = all.FirstOrDefault(f =>
                string.Equals(f.field, "upkeepResult", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            return new DictMaintenance
            {
                MaintenanceStatus = maintenanceStatus,
                MaintenanceResult = maintenanceResult
            };
        }

        // ---------- 保养详情 ----------
        public Task<ApiResp<MaintenanceDetailDto>?> GetMainDetailAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<MaintenanceDetailDto>(_mainDetailsEndpoint, id, ct);

        // ---------- 保养流程 ----------
        public Task<ApiResp<List<MaintenanceWorkflowNode>>> GetMainWorkflowAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<List<MaintenanceWorkflowNode>>(_mainWorkflowPath, id, ct)!;

        // ---------- 维修列表 ----------
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
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(workOrderNo)) p["workOrderNo"] = workOrderNo.Trim();
            if (!string.IsNullOrWhiteSpace(submitTimeBegin)) p["submitTimeBegin"] = submitTimeBegin;
            if (!string.IsNullOrWhiteSpace(submitTimeEnd)) p["submitTimeEnd"] = submitTimeEnd;
            if (!string.IsNullOrWhiteSpace(auditStatus)) p["auditStatus"] = auditStatus;

            return await GetPageAsync<RepairRecordDto>(_repPageEndpoint, p, ct);
        }

        // ---------- 维修字典 ----------
        public async Task<DictRepair> GetRepairDictsAsync(CancellationToken ct = default)
        {
            var all = await GetDictFieldsAsync(_dictRepEndpoint, ct);

            var auditStatus = all.FirstOrDefault(f =>
                string.Equals(f.field, "auditStatus", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            var urgent = all.FirstOrDefault(f =>
                string.Equals(f.field, "urgent", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            var maintainType = all.FirstOrDefault(f =>
                string.Equals(f.field, "maintainType", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            return new DictRepair
            {
                AuditStatus = auditStatus,
                Urgent = urgent,
                MaintainType = maintainType
            };
        }

        // ---------- 维修详情 ----------
        public Task<ApiResp<RepairDetailDto>?> GetRepairDetailAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<RepairDetailDto>(_repDetailsEndpoint, id, ct);

        // ---------- 维修流程 ----------
        public Task<ApiResp<List<RepairWorkflowNode>>> GetRepairWorkflowAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<List<RepairWorkflowNode>>(_repWorkflowPath, id, ct)!;

        // ---------- 巡检执行保存 ----------
        public Task<ApiResp<bool?>> ExecuteSaveAsync(InspectDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<InspectDetailDto, bool?>(_executeSavePath, payload, ct)!;

        // ---------- 巡检执行完成 ----------
        public Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(InspectDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<InspectDetailDto, bool?>(_executeCompletePath, payload, ct)!;

        // ---------- 维护执行保存 ----------
        public Task<ApiResp<bool?>> ExecuteMainSaveAsync(MaintenanceDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<MaintenanceDetailDto, bool?>(_mainexecuteSavePath, payload, ct)!;

        // ---------- 维护执行完成 ----------
        public Task<ApiResp<bool?>> ExecuteMainCompleteAsync(MaintenanceDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<MaintenanceDetailDto, bool?>(_mainexecuteCompletePath, payload, ct)!;

        // ---------- 维修执行保存 ----------
        public Task<ApiResp<bool?>> ExecuteRepairSaveAsync(RepairDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<RepairDetailDto, bool?>(_repexecuteSavePath, payload, ct)!;

        // ---------- 维修执行完成 ----------
        public Task<ApiResp<bool?>> ExecuteRepairCompleteAsync(RepairDetailDto payload, CancellationToken ct = default)
            => PostJsonAsync<RepairDetailDto, bool?>(_repexecuteCompletePath, payload, ct)!;

        public async Task<ApiResp<bool>> DeleteInspectAttachmentAsync(string id, CancellationToken ct = default)
        {
            return await _attachmentApi.DeleteAttachmentAsync(id, _deleteAttPath, ct);
        }
        public async Task<ApiResp<bool>> DeleteMainAttachmentAsync(string id, CancellationToken ct = default)
        {
            return await _attachmentApi.DeleteAttachmentAsync(id, _maindeleteAttPath, ct);
        }
        public async Task<ApiResp<bool>> DeleteRepairAttachmentAsync(string id, CancellationToken ct = default)
        {
            return await _attachmentApi.DeleteAttachmentAsync(id, _repdeleteAttPath, ct);
        }
        //异常提报
        public async Task<PageResponeResult<MaintenanceReportDto>> ESPageQueryAsync(
            int pageNo,
         int pageSize,
         string? maintainNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? auditStatus,
         bool searchCount,
         CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["searchCount"] = searchCount ? "true" : "false"
            };
            if (!string.IsNullOrWhiteSpace(maintainNo)) p["maintainNo"] = maintainNo.Trim();
            if (!string.IsNullOrWhiteSpace(createdTimeBegin)) p["createdTimeStart"] = createdTimeBegin;
            if (!string.IsNullOrWhiteSpace(createdTimeEnd)) p["createdTimeEnd"] = createdTimeEnd;
            if (!string.IsNullOrWhiteSpace(auditStatus)) p["auditStatus"] = auditStatus;

            return await GetPageAsync<MaintenanceReportDto>(_exceptPageEndpoint, p, ct);
        }

        public async Task<DictExcept> GetExceptDictsAsync(CancellationToken ct = default)
        {
            var all = await GetDictFieldsAsync(_dictESEndpoint, ct);

            var auditStatus = all.FirstOrDefault(f =>
                string.Equals(f.field, "auditStatus", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            var urgent = all.FirstOrDefault(f =>
                string.Equals(f.field, "urgent", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            var devStatus = all.FirstOrDefault(f =>
                string.Equals(f.field, "devStatus", StringComparison.OrdinalIgnoreCase))
                ?.dictItems ?? new List<DictItem>();

            return new DictExcept
            {
                AuditStatus = auditStatus,
                Urgent = urgent,
                DevStatus = devStatus
            };
        }

        public Task<ApiResp<MaintenanceReportDto>?> GetExceptDetailAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<MaintenanceReportDto>(_exceptDetailsEndpoint, id, ct);
        public Task<ApiResp<List<ExceptWorkflowNode>>> GetExceptWorkflowAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<List<ExceptWorkflowNode>>(_exceptWorkflowPath, id, ct)!;

        // ---------- 异常保存 ----------
        public Task<ApiResp<bool?>> ExecuteExceptSaveAsync(MaintenanceReportDto payload, CancellationToken ct = default)
            => PostJsonAsync<MaintenanceReportDto, bool?>(_repexecuteSavePath, payload, ct)!;

        // ---------- 异常保修 ----------
        public Task<ApiResp<bool?>> SubmitExceptAsync(MaintenanceReportDto payload, CancellationToken ct = default)
            => PostJsonAsync<MaintenanceReportDto, bool?>(_repexecuteCompletePath, payload, ct)!;
    }

}

