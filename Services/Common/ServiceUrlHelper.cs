using System;

namespace IndustrialControlMAUI.Services.Common;

internal static class ServiceUrlHelper
{
    public static string NormalizeRelative(string? endpoint, string servicePath)
    {
        var ep = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(ep)) return "/";

        if (string.IsNullOrWhiteSpace(servicePath)) servicePath = "/";
        if (!servicePath.StartsWith("/")) servicePath = "/" + servicePath;
        servicePath = servicePath.TrimEnd('/');

        if (ep.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            ep.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return ep;
        }

        if (!string.IsNullOrEmpty(servicePath) &&
            servicePath != "/" &&
            ep.StartsWith(servicePath + "/", StringComparison.OrdinalIgnoreCase))
        {
            ep = ep[servicePath.Length..];
        }

        if (!ep.StartsWith("/")) ep = "/" + ep;
        return ep;
    }

    public static string BuildFullUrl(Uri? baseAddress, string url)
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
}
