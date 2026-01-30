using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Tools
{
    public static class ResponseGuard
    {
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        private static readonly HashSet<string> _expiredCodes = new(StringComparer.OrdinalIgnoreCase)
    { "401", "40101", "40301", "TOKEN_EXPIRED", "NO_AUTH" };

        public static async Task<string> ReadAsStringAndCheckAsync(HttpResponseMessage res, AuthState auth, CancellationToken ct)
        {
            var text = await ReadAsStringSafeAsync(res.Content, ct).ConfigureAwait(false);

            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _ = auth.LogoutAsync($"登录状态失效（HTTP {(int)res.StatusCode}）");
                return text;
            }

            try
            {
                var api = JsonSerializer.Deserialize<ApiBase>(text, _json);
                if (api?.success == false)
                {
                    var code = api.code?.ToString() ?? "";
                    if (_expiredCodes.Contains(code) || code.StartsWith("401", StringComparison.OrdinalIgnoreCase)
                        || code.Contains("EXPIRE", StringComparison.OrdinalIgnoreCase))
                    {
                        _ = auth.LogoutAsync(api?.message ?? "登录状态失效");
                    }
                }
            }
            catch { /* ignore */ }

            return text;
        }

        public static async Task<string> ReadAsStringSafeAsync(HttpContent content, CancellationToken ct)
        {
            try
            {
                return await content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCopyStreamError(ex))
            {
                return await ReadAsStringViaStreamAsync(content, ct).ConfigureAwait(false);
            }
        }

        private static bool IsCopyStreamError(Exception ex)
        {
            while (true)
            {
                if (ex is IOException or HttpRequestException)
                {
                    if (ex.Message.IndexOf("copying content to a stream", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                if (ex.InnerException is null)
                    return false;

                ex = ex.InnerException;
            }
        }

        private static async Task<string> ReadAsStringViaStreamAsync(HttpContent content, CancellationToken ct)
        {
            await using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        private sealed class ApiBase
        {
            public bool? success { get; set; }
            public object? code { get; set; }
            public string? message { get; set; }
        }
    }
}
