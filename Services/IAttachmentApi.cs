using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace IndustrialControlMAUI.Services
{
    // ===================== 接口定义 =====================
    public interface IAttachmentApi
    {
        Task<ApiResp<UploadAttachmentResult>> UploadAttachmentAsync(
                string attachmentFolder,
                string attachmentLocation,
                Stream fileStream,                // ← 新增：文件流
                string fileName,                  // ← 新增：文件名（需含后缀）
                string? contentType = null,       // ← 可选：MIME 类型
                string? attachmentName = null,
                string? attachmentExt = null,
                long? attachmentSize = null,
                CancellationToken ct = default);

        Task<ApiResp<string>> GetPreviewUrlAsync(string attachmentUrl, long? expires = null, CancellationToken ct = default);
        Task<ApiResp<bool>> DeleteAttachmentAsync(string id, string atturl, CancellationToken ct = default);
    }

    // ===================== 实现 =====================
    public class AttachmentApi : IAttachmentApi
    {
        private readonly HttpClient _http;
        private readonly AuthState _auth;
       private readonly string _uploadAttachmentPath;
        private readonly string _previewImagePath;

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public AttachmentApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
        {
            _http = http;
            _auth = auth;
            if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                _http.Timeout = TimeSpan.FromSeconds(15);

            var baseUrl = configLoader.GetBaseUrl();
            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _uploadAttachmentPath = ServiceUrlHelper.NormalizeRelative(
               configLoader.GetApiPath("quality.uploadAttachment", "/pda/attachment/uploadAttachment"), servicePath);
            _previewImagePath = ServiceUrlHelper.NormalizeRelative(
    configLoader.GetApiPath("quality.previewImage", "/pda/attachment/previewAttachment"),
    servicePath);
           
        }
      
        public async Task<ApiResp<UploadAttachmentResult>> UploadAttachmentAsync(
            string attachmentFolder,
            string attachmentLocation,
            Stream fileStream,                // ← 新增：文件流
            string fileName,                  // ← 新增：文件名（需含后缀）
            string? contentType = null,       // ← 可选：MIME 类型
            string? attachmentName = null,
            string? attachmentExt = null,
            long? attachmentSize = null,
            CancellationToken ct = default)
        {
            var url = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _uploadAttachmentPath);

            // 准备 multipart/form-data
            using var form = new MultipartFormDataContent();

            // 1) 文件部分（字段名要与后端匹配，常见是 "file" 或文档指定的名）
            var fileContent = new StreamContent(fileStream);
            if (!string.IsNullOrWhiteSpace(contentType))
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            // 关键：name 要与后端参数名一致（例如 "file"），并且一定要提供 filename
            form.Add(fileContent, "file", fileName);

            // 2) 其他普通字段（与后端参数名一一对应）
            form.Add(new StringContent(attachmentFolder), "attachmentFolder");
            form.Add(new StringContent(attachmentLocation), "attachmentLocation");

            if (!string.IsNullOrWhiteSpace(attachmentName))
                form.Add(new StringContent(attachmentName), "attachmentName");

            if (!string.IsNullOrWhiteSpace(attachmentExt))
                form.Add(new StringContent(attachmentExt), "attachmentExt");

            if (attachmentSize.HasValue)
                form.Add(new StringContent(attachmentSize.Value.ToString()), "attachmentSize");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            using var res = await _http.SendAsync(req, ct);

            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
            // 如果服务端会返回 4xx/5xx + JSON 错误体，先别急着 EnsureSuccessStatusCode，以便保留服务端错误信息
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Upload failed: {(int)res.StatusCode} {json}");

            return JsonSerializer.Deserialize<ApiResp<UploadAttachmentResult>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new ApiResp<UploadAttachmentResult>();
        }


        public async Task<ApiResp<string>> GetPreviewUrlAsync(string attachmentUrl, long? expires = null, CancellationToken ct = default)
        {
            var baseUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _previewImagePath);

            // 组装 query
            var qb = HttpUtility.ParseQueryString(string.Empty);
            qb["attachmentUrl"] = attachmentUrl;                    // 必填
            if (expires.HasValue) qb["expires"] = expires.Value.ToString(); // 可选（秒）

            var url = $"{baseUrl}?{qb}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var res = await _http.SendAsync(req, ct);
            var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            return JsonSerializer.Deserialize<ApiResp<string>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            }) ?? new ApiResp<string>();
        }

        public async Task<ApiResp<bool>> DeleteAttachmentAsync(string id,string atturl, CancellationToken ct = default)
        {
            var url = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, atturl);
            var reqObj = new DeleteAttachmentReq { id = id };
            var json = JsonSerializer.Serialize(reqObj, _json);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute))
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(req, ct);
            var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

            if (!res.IsSuccessStatusCode)
                return new ApiResp<bool> { success = false, code = (int)res.StatusCode, message = $"HTTP {(int)res.StatusCode}" };

            return JsonSerializer.Deserialize<ApiResp<bool>>(body, _json)
                   ?? new ApiResp<bool> { success = false, message = "Empty body" };
        }
    }



}
