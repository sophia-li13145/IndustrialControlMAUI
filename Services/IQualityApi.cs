using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IQualityApi
    {
        Task<PageResponeResult<QualityRecordDto>> PageQueryAsync(
         int pageNo,
         int pageSize,
         string? qualityNo,
         string? createdTimeBegin,
         string? createdTimeEnd,
         string? inspectStatus,
         string? qualityType,
         bool searchCount,
         CancellationToken ct = default);
        Task<DictQuality> GetQualityDictsAsync(CancellationToken ct = default);
        Task<ApiResp<QualityDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default);
        
        Task<ApiResp<bool?>> ExecuteSaveAsync(QualityDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(QualityDetailDto payload, CancellationToken ct = default);
        Task<ApiResp<DefectPage>> GetDefectPageAsync(
    int pageNo, int pageSize,
    string? defectCode = null,
    string? defectName = null,
    string? levelCode = null,
    string? status = null,
    bool? searchCount = null,
    string? createdTimeBegin = null,
    string? createdTimeEnd = null,
    CancellationToken ct = default);

        Task<ApiResp<bool>> DeleteAttachmentAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default);
        Task<ApiResp<List<InspectDeviceOption>>?> GetInspectDevicesAsync(CancellationToken ct = default);
        Task<ApiResp<List<InspectParamOption>>?> GetInspectParamsAsync(string deviceCode, CancellationToken ct = default);
        Task<ApiResp<bool?>> CheckQcItemLimitAsync(
            string deviceCode,
            string paramCode,
            string qsOrderItemId,
            string? collectTimeBegin,
            string? collectTimeEnd,
            decimal? actualValue,
            CancellationToken ct = default);
        Task<PageResponeResult<InspectionDetailRecord>?> GetInspectionDetailPageAsync(
            string deviceCode,
            string paramCode,
            string? collectTimeBegin,
            string? collectTimeEnd,
            int pageNo,
            int pageSize,
            bool? searchCount,
            CancellationToken ct = default);
    }

    // ===================== 实现 =====================
    public class QualityApi : IQualityApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;
        private readonly string _pageEndpoint;
        private readonly string _dictEndpoint;
        private readonly string _detailsEndpoint;
        private readonly string _executeSavePath;
        private readonly string _executeCompletePath;
        private readonly string _defectPagePath;
        private readonly string _deleteAttPath;
        private readonly IAttachmentApi _attachmentApi;
        private readonly string _workflowPath;
        private readonly string _inspectDevicePath;
        private readonly string _inspectParamPath;
        private readonly string _autoInspectPath;
        private readonly string _inspectDetailPagePath;

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public QualityApi(HttpClient http, IConfigLoader configLoader, AuthState auth, IAttachmentApi attachmentApi)
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

            _pageEndpoint = NormalizeRelative(
                configLoader.GetApiPath("quality.page", "/pda/qsOrderQuality/pageQuery"), servicePath);
            _dictEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.dictList", "/pda/qsOrderQuality/getDictList"), servicePath);
            _detailsEndpoint = NormalizeRelative(
               configLoader.GetApiPath("quality.detailList", "/pda/qsOrderQuality/detail"), servicePath);
            _executeSavePath = NormalizeRelative(
             configLoader.GetApiPath("quality.executeSave", "/pda/qsOrderQuality/executeSave"), servicePath);
            _executeCompletePath = NormalizeRelative(
                configLoader.GetApiPath("quality.executeComplete", "/pda/qsOrderQuality/executeCompleteInspection"), servicePath);
            _defectPagePath = NormalizeRelative(
            configLoader.GetApiPath("quality.defect.page", "/pda/qsOrderQuality/defectPageQuery"),
            servicePath);
            _deleteAttPath = NormalizeRelative(
    configLoader.GetApiPath("quality.previewImage", "/pda/qsOrderQuality/deleteAttachment"),
    servicePath);
            _workflowPath = NormalizeRelative(
               configLoader.GetApiPath("quality.workflow", "/pda/qsOrderQuality/getQsOrderWorkflow"),
               servicePath);
            _inspectDevicePath = NormalizeRelative(
                configLoader.GetApiPath("quality.inspectDevices", "/pda/common/queryDevList"),
                servicePath);
            _inspectParamPath = NormalizeRelative(
                configLoader.GetApiPath("quality.inspectParams", "/pda/qsOrderQuality/queryPmsEqptPointParams"),
                servicePath);
            _autoInspectPath = NormalizeRelative(
                configLoader.GetApiPath("quality.autoInspect", "/pda/qsOrderQuality/checkQsItemLimit"),
                servicePath);
            _inspectDetailPagePath = NormalizeRelative(
                configLoader.GetApiPath("quality.inspectDetailPage", "/pda/qsOrderQuality/pageQueryInspectionDetail"),
                servicePath);
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
        public async Task<PageResponeResult<QualityRecordDto>> PageQueryAsync(
     int pageNo,
     int pageSize,
     string? qualityNo,
     string? createdTimeBegin,
     string? createdTimeEnd,
     string? inspectStatus,
     string? qualityType,
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
            if (!string.IsNullOrWhiteSpace(qualityNo)) p["qualityNo"] = qualityNo!.Trim();
            if (!string.IsNullOrWhiteSpace(createdTimeBegin)) p["createdTimeBegin"] = createdTimeBegin!;
            if (!string.IsNullOrWhiteSpace(createdTimeEnd)) p["createdTimeEnd"] = createdTimeEnd!;
            if (!string.IsNullOrWhiteSpace(inspectStatus)) p["inspectStatus"] = inspectStatus!;
            if (!string.IsNullOrWhiteSpace(qualityType)) p["qualityType"] = qualityType!;

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
                return new PageResponeResult<QualityRecordDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            // 5) 反序列化（大小写不敏感）
            return JsonSerializer.Deserialize<PageResponeResult<QualityRecordDto>>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                   ) ?? new PageResponeResult<QualityRecordDto> { success = false, message = "Empty body" };
        }


        public async Task<DictQuality> GetQualityDictsAsync(CancellationToken ct = default)
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
            var qualityTypes = all.FirstOrDefault(f =>
       string.Equals(f.field, "qualityType", StringComparison.OrdinalIgnoreCase))
       ?.dictItems ?? new List<DictItem>();
            

            return new DictQuality { InspectStatus = inspectStatus, QualityTypes = qualityTypes };
        }

        public async Task<ApiResp<List<InspectDeviceOption>>?> GetInspectDevicesAsync(CancellationToken ct = default)
        {
            var full = BuildFullUrl(_http.BaseAddress, _inspectDevicePath);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<List<InspectDeviceOption>>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<List<InspectDeviceOption>>>(json, _json);
        }

        public async Task<ApiResp<List<InspectParamOption>>?> GetInspectParamsAsync(string deviceCode, CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["deviceCode"] = deviceCode
            };
            var url = _inspectParamPath + "?" + BuildQuery(p);
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<List<InspectParamOption>>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<List<InspectParamOption>>>(json, _json);
        }

        public async Task<ApiResp<bool?>> CheckQcItemLimitAsync(
            string deviceCode,
            string paramCode,
            string qsOrderItemId,
            string? collectTimeBegin,
            string? collectTimeEnd,
            decimal? actualValue,
            CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["deviceCode"] = deviceCode,
                ["paramCode"] = paramCode,
                ["qsOrderItemId"] = qsOrderItemId
            };
            if (!string.IsNullOrWhiteSpace(collectTimeBegin)) p["collectTimeBegin"] = collectTimeBegin!;
            if (!string.IsNullOrWhiteSpace(collectTimeEnd)) p["collectTimeEnd"] = collectTimeEnd!;
            if (actualValue is not null) p["actualValue"] = actualValue.Value.ToString("G29", CultureInfo.InvariantCulture);

            var url = _autoInspectPath + "?" + BuildQuery(p);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<bool?>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<bool?>>(json, _json);
        }

        public async Task<PageResponeResult<InspectionDetailRecord>?> GetInspectionDetailPageAsync(
            string deviceCode,
            string paramCode,
            string? collectTimeBegin,
            string? collectTimeEnd,
            int pageNo,
            int pageSize,
            bool? searchCount,
            CancellationToken ct = default)
        {
            var p = new Dictionary<string, string>
            {
                ["deviceCode"] = deviceCode,
                ["paramCode"] = paramCode,
                ["pageNo"] = pageNo.ToString(),
                ["pageSize"] = pageSize.ToString()
            };
            if (!string.IsNullOrWhiteSpace(collectTimeBegin)) p["collectTimeBegin"] = collectTimeBegin!;
            if (!string.IsNullOrWhiteSpace(collectTimeEnd)) p["collectTimeEnd"] = collectTimeEnd!;
            if (searchCount.HasValue) p["searchCount"] = searchCount.Value ? "true" : "false";

            var url = _inspectDetailPagePath + "?" + BuildQuery(p);
            var full = BuildFullUrl(_http.BaseAddress, url);

            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            if (!res.IsSuccessStatusCode)
            {
                return new PageResponeResult<InspectionDetailRecord>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<PageResponeResult<InspectionDetailRecord>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        // Services/QualityApi.cs 追加方法（沿用你 BuildQuery/BuildFullUrl 风格）
        public async Task<ApiResp<QualityDetailDto>?> GetDetailAsync(string id, CancellationToken ct = default)
        {
            var url = _detailsEndpoint + "?id=" + Uri.EscapeDataString(id ?? "");
            var full = BuildFullUrl(_http.BaseAddress, url);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
            {
                return new ApiResp<QualityDetailDto>
                {
                    success = false,
                    code = (int)res.StatusCode,
                    message = $"HTTP {(int)res.StatusCode}"
                };
            }

            return JsonSerializer.Deserialize<ApiResp<QualityDetailDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new ApiResp<QualityDetailDto> { success = false, message = "Empty body" };
        }
       



        public async Task<ApiResp<bool?>> ExecuteSaveAsync(QualityDetailDto payload, CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _executeSavePath);
            var json = JsonSerializer.Serialize(payload, _json);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute))
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<bool?> { success = false, code = (int)res.StatusCode, message = $"HTTP {(int)res.StatusCode}" };

            return JsonSerializer.Deserialize<ApiResp<bool?>>(body, _json)
                   ?? new ApiResp<bool?> { success = false, message = "Empty body" };
        }

        public async Task<ApiResp<bool?>> ExecuteCompleteInspectionAsync(QualityDetailDto payload, CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _executeCompletePath);
            var json = JsonSerializer.Serialize(payload, _json);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute))
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<bool?> { success = false, code = (int)res.StatusCode, message = $"HTTP {(int)res.StatusCode}" };

            return JsonSerializer.Deserialize<ApiResp<bool?>>(body, _json)
                   ?? new ApiResp<bool?> { success = false, message = "Empty body" };
        }
        public async Task<ApiResp<DefectPage>> GetDefectPageAsync(
                 int pageNo, int pageSize,
                 string? defectCode = null,
                 string? defectName = null,
                 string? levelCode = null,
                 string? status = null,
                 bool? searchCount = null,
                 string? createdTimeBegin = null,
                 string? createdTimeEnd = null,
                 CancellationToken ct = default)
        {
            var url = BuildFullUrl(_http.BaseAddress, _defectPagePath);

            // 该接口是 GET + query（表单编码），必填 pageNo/pageSize
            var q = new List<string>
    {
        $"pageNo={pageNo}",
        $"pageSize={pageSize}"
    };
            if (!string.IsNullOrWhiteSpace(defectCode)) q.Add($"defectCode={Uri.EscapeDataString(defectCode)}");
            if (!string.IsNullOrWhiteSpace(defectName)) q.Add($"defectName={Uri.EscapeDataString(defectName)}");
            if (!string.IsNullOrWhiteSpace(levelCode)) q.Add($"levelCode={Uri.EscapeDataString(levelCode)}");
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (searchCount.HasValue) q.Add($"searchCount={(searchCount.Value ? "true" : "false")}");
            if (!string.IsNullOrWhiteSpace(createdTimeBegin)) q.Add($"createdTimeBegin={Uri.EscapeDataString(createdTimeBegin)}");
            if (!string.IsNullOrWhiteSpace(createdTimeEnd)) q.Add($"createdTimeEnd={Uri.EscapeDataString(createdTimeEnd)}");

            var full = url + "?" + string.Join("&", q);
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<DefectPage> { success = false, code = (int)res.StatusCode, message = $"HTTP {(int)res.StatusCode}" };

            return System.Text.Json.JsonSerializer.Deserialize<ApiResp<DefectPage>>(body, _json)
                   ?? new ApiResp<DefectPage> { success = false, message = "Empty body" };
        }

        public async Task<ApiResp<bool>> DeleteAttachmentAsync(string id, CancellationToken ct = default)
        { 
        return await _attachmentApi.DeleteAttachmentAsync(id, _deleteAttPath, ct);
         }

        public Task<ApiResp<List<InspectWorkflowNode>>> GetWorkflowAsync(string id, CancellationToken ct = default)
            => GetApiRespByIdAsync<List<InspectWorkflowNode>>(_workflowPath, id, ct)!;

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

    }



}
