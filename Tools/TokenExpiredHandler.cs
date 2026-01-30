using System.Net;
using System.Text.Json;

namespace IndustrialControlMAUI.Tools
{
    /// <summary>
    /// 统一拦截 HTTP 401/403 或 业务 JSON 返回的 token 失效码，触发 Logout。
    /// </summary>
    public sealed class TokenExpiredHandler : DelegatingHandler
    {
        private readonly AuthState _auth;

        public TokenExpiredHandler(AuthState auth) => _auth = auth;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // 登录/刷新等接口排除，避免登录失败时也触发 Logout
            var path = request.RequestUri?.AbsolutePath?.ToLowerInvariant() ?? "";
            if (path.Contains("/auth/login") || path.Contains("/auth/refresh"))
                return await base.SendAsync(request, ct);

            var resp = await base.SendAsync(request, ct);

            // ① HTTP 层判断
            if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
            {
                _ = _auth.LogoutAsync($"登录状态失效（HTTP {(int)resp.StatusCode}）");
                return resp;
            }

            // ② 业务层 JSON 判断（与你项目通用响应结构对齐）
            try
            {
                var media = resp.Content?.Headers?.ContentType?.MediaType?.ToLowerInvariant();
                if (media is not null && media.Contains("json"))
                {
                    var text = await ResponseGuard.ReadAsStringSafeAsync(resp.Content, ct);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var api = JsonSerializer.Deserialize<ApiBase>(text, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (api?.success == false)
                        {
                            var code = api.code?.ToString()?.ToUpperInvariant();
                            if (code is "401" or "40101" or "40301" or "TOKEN_EXPIRED" or "NO_AUTH"
                                || (code?.StartsWith("401") ?? false) || (code?.Contains("EXPIRE") ?? false))
                            {
                                _ = _auth.LogoutAsync(api?.message ?? "登录状态已过期");
                            }
                        }

                        // 还原内容，避免上层读取不了
                        resp.Content = new StringContent(text, System.Text.Encoding.UTF8,
                            resp.Content.Headers.ContentType?.MediaType ?? "application/json");
                    }
                }
            }
            catch { /* 忽略解析异常 */ }

            return resp;
        }

        private sealed class ApiBase
        {
            public bool? success { get; set; }
            public object? code { get; set; }
            public string? message { get; set; }
        }
    }
}
