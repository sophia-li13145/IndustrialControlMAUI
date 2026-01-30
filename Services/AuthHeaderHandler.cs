// Services/AuthHeaderHandler.cs
using System.Net.Http.Headers;

namespace IndustrialControlMAUI.Services
{
    /// 统一为经 DI 创建的 HttpClient 附加鉴权头：token: <jwt>
    public sealed class AuthHeaderHandler : DelegatingHandler
    {


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // 1) 取 token（按你项目实际来：SecureStorage/Preferences/ITokenProvider）
                var token = await TokenStorage.LoadAsync().ConfigureAwait(false); // 示例：请替换为你的实现
                token = token?.Trim();

                // 2) 写入头（只移除自己要覆盖的键）
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Remove("token");
                    request.Headers.Remove("satoken");
                    request.Headers.Remove("Authorization");

                    // 自定义头
                    request.Headers.TryAddWithoutValidation("token", token);

                    // Sa-Token 常用
                    request.Headers.TryAddWithoutValidation("satoken", token);

                    // JWT 常用
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // 3) 日志：放在写入之后再打印，便于确认真的带上了
                System.Diagnostics.Debug.WriteLine($"[AuthHeaderHandler] {request.RequestUri}");
                System.Diagnostics.Debug.WriteLine(
                    "[AuthHeaderHandler] headers: " +
                    string.Join(" | ", request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthHeaderHandler] error: {ex}");
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }


    }
}
