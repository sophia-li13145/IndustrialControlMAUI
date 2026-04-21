using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Serilog;

namespace IndustrialControlMAUI.Services
{
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
            try
            {
                var baseUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _previewImagePath);

                var qb = HttpUtility.ParseQueryString(string.Empty);
                qb["attachmentUrl"] = attachmentUrl;
                if (expires.HasValue) qb["expires"] = expires.Value.ToString();

                var url = $"{baseUrl}?{qb}";

                Log.Information("=== GetPreviewUrlAsync Start ===");
                Log.Information("BaseAddress: {BaseAddress}", _http.BaseAddress?.ToString());
                Log.Information("PreviewPath: {PreviewPath}", _previewImagePath);
                Log.Information("AttachmentUrl param: {AttachmentUrl}", attachmentUrl);
                Log.Information("Final request url: {Url}", url);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);

                // 看看认证头是否真的加上了
                if (req.Headers.Authorization != null)
                {
                    Log.Information("Authorization header exists: {Scheme}", req.Headers.Authorization.Scheme);
                }
                else
                {
                    Log.Information("Authorization header is null on request object before send");
                }

                using var res = await _http.SendAsync(req, ct);

                Log.Information("HTTP Status: {StatusCode}", (int)res.StatusCode);
                Log.Information("Response headers: {Headers}", res.Headers.ToString());

                var raw = await res.Content.ReadAsStringAsync(ct);
                Log.Information("Raw response: {RawResponse}", raw);

                // 先不用 ResponseGuard，先看真实返回
                var result = JsonSerializer.Deserialize<ApiResp<string>>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                }) ?? new ApiResp<string>();

                Log.Information("Deserialize result: code={Code}, msg={Msg}, data={Data}",
                    result.code, result.message, result.result);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetPreviewUrlAsync failed. attachmentUrl={AttachmentUrl}", attachmentUrl);
                throw;
            }
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
