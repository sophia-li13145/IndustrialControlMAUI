using IndustrialControlMAUI.Services.Common;

using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using Serilog;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseItem>> QueryAllWarehouseAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LocationItem>> QueryLocationsByWarehouseCodeAsync(string warehouseCode, CancellationToken ct = default);
    Task<IReadOnlyList<LocationSegment>> QueryLocationSegmentsByWarehouseCodeAsync(string warehouseCode, CancellationToken ct = default);
}

public sealed class WarehouseService : IWarehouseService
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _queryAllEndpoint;
    private readonly string _queryByCodeEndpoint;
    private static readonly SemaphoreSlim _httpGate = new(1, 1);


    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        PropertyNameCaseInsensitive = true,
        // 兼容 "123" 这种字符串数字写成数字类型
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public WarehouseService(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        JsonNode cfg = configLoader.Load();

        var baseUrl = configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

        _queryAllEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("warehouse.queryAll", "/pda/wmsWarehouse/queryAllWarehouse"), servicePath);
        _queryByCodeEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("warehouse.queryByCode", "/pda/wmsWarehouse/queryLocationByWarehouseCode"), servicePath);
        _auth = auth;
    }

    public async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        string requestUrl;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            requestUrl = url;
        else
        {
            var baseUrl = _http.BaseAddress?.AbsoluteUri
                          ?? throw new InvalidOperationException("BaseAddress 未配置");
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            requestUrl = baseUrl + url.TrimStart('/');
        }

        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUrl, UriKind.Absolute))
            {
                Version = System.Net.HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
            if (!req.Headers.Contains("Accept-Encoding"))
                req.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");

            try
            {
                using var res = await SendAsyncCore(req, ct).ConfigureAwait(false); // ResponseHeadersRead
                
                if (!res.IsSuccessStatusCode)
                {
                    var head = await PeekAsync(res.Content, 2048, ct).ConfigureAwait(false);
                    Log.Warning("GET {Url} 非2xx：{Status} 预览：{Head}", requestUrl, (int)res.StatusCode, head);
                    res.EnsureSuccessStatusCode();
                }

                EnsureJson(res);

                // —— 关键：在 using 内把内容一次性读完并反序列化，同时抓原文与路径信息 ——
                var json = await ReadBodyAsStringAsync(res, ct).ConfigureAwait(false);

                try
                {
                    return JsonSerializer.Deserialize<T>(json, JsonOpt);
                }
                catch (JsonException jx)
                {
                    // 把 JSON 路径、片段都打出来，定位最直观
                    var preview = json.Length > 2048 ? json.Substring(0, 2048) : json;
                    Log.Error(jx, "反序列化失败：{Url} Path={Path} Line={Line} BytePos={Byte} 预览={Preview}",
                              requestUrl, jx.Path, jx.LineNumber, jx.BytePositionInLine, preview);
                    throw; // 直接抛 JsonException，避免被包装成 TargetInvocationException
                }
            }
            catch (IOException ioex) when (
                ioex.Message?.IndexOf("copying content to a stream", StringComparison.OrdinalIgnoreCase) >= 0
                && attempt == 0)
            {
                // 网络流拷贝偶发错误 → 幂等 GET 重试一次
                continue;
            }
            catch (Exception ex) when (attempt == 0)
            {
                var root = Unwrap(ex);
                if (root is HttpRequestException || root is IOException)
                {
                    Log.Warning(root, "GET {Url} 网络/管道异常，重试一次", requestUrl);
                    continue;
                }
                // 其余直接抛出根因，避免 TargetInvocationException 迷惑视线
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(root).Throw();
            }
        }

        return default; // 两次都失败
    }

    private static Exception Unwrap(Exception ex)
    {
        while (true)
        {
            if (ex is TargetInvocationException tie && tie.InnerException != null) { ex = tie.InnerException; continue; }
            if (ex is AggregateException ae && ae.InnerException != null) { ex = ae.InnerException; continue; }
            return ex;
        }
    }

    public async Task<IReadOnlyList<WarehouseItem>> QueryAllWarehouseAsync(CancellationToken ct = default)
    {
        await _httpGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var dto = await GetJsonAsync<WarehouseResp>(_queryAllEndpoint, ct);
        return dto?.result?.Select(x => new WarehouseItem
        {
            WarehouseCode = x.warehouseCode ?? "",
            WarehouseName = string.IsNullOrWhiteSpace(x.warehouseName) ? x.warehouseCode ?? "" : x.warehouseName
        }).Where(x => !string.IsNullOrEmpty(x.WarehouseCode)).ToList() ?? new List<WarehouseItem>();
        }
        finally { _httpGate.Release(); }
    }

    public async Task<IReadOnlyList<LocationItem>> QueryLocationsByWarehouseCodeAsync(
    string warehouseCode, CancellationToken ct = default)
    {
        await _httpGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var url = $"{_queryByCodeEndpoint}?warehouseCode={Uri.EscapeDataString(warehouseCode)}";

        // 直接按你的封装拿 DTO
        var dto = await GetJsonAsync<LocationResp>(url, ct);
        if (dto?.result is null || dto.result.Count == 0)
            return Array.Empty<LocationItem>();

        // 拍平：result[] -> layerData[] -> List<LocationLiteDto> -> LocationLiteDto
        var items =
            (from g in dto.result
             let groupZone = g.zone ?? ""
             from seg in (g.layerData ?? Array.Empty<List<LocationLiteDto>>())
             where seg != null
             from l in seg
             select new LocationItem
             {
                 WarehouseCode = string.IsNullOrWhiteSpace(l.warehouseCode) ? warehouseCode : l.warehouseCode!,
                 WarehouseName = l.warehouseName ?? "",
                 Zone = string.IsNullOrWhiteSpace(l.zone) ? groupZone : l.zone!,
                 Rack = l.rack ?? "",
                 Layer = l.layer ?? "",
                 Location = l.location ?? "",
                 InventoryStatus = l.inventoryStatus ?? ""
             })
            .ToList();

        return items;
        }
        finally { _httpGate.Release(); }
    }

    public async Task<IReadOnlyList<LocationSegment>> QueryLocationSegmentsByWarehouseCodeAsync(
        string warehouseCode, CancellationToken ct = default)
    {
        await _httpGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var url = $"{_queryByCodeEndpoint}?warehouseCode={Uri.EscapeDataString(warehouseCode)}";
            var dto = await GetJsonAsync<LocationResp>(url, ct).ConfigureAwait(false);

            if (dto?.result is null || dto.result.Count == 0)
                return Array.Empty<LocationSegment>();

            // 将 result[] 下的每个 layerData(List<List<LocationLiteDto>>) 的每个 list 映射成一个 Segment
            // 注意：保留原有 zone（g.zone），list 内部元素缺失的字段用 group 的值或空串兜底
            var segments = new List<LocationSegment>();

            foreach (var g in dto.result)
            {
                var groupZone = g.zone ?? "";
                var layerLists = g.layerData ?? Array.Empty<List<LocationLiteDto>>();

                foreach (var seg in layerLists)
                {
                    if (seg == null || seg.Count == 0)
                    {
                        // 空段也可选择跳过；如需“空段也显示分隔”可保留
                        continue;
                    }

                    var items = new List<LocationItem>(seg.Count);
                    foreach (var l in seg)
                    {
                        items.Add(new LocationItem
                        {
                            WarehouseCode = string.IsNullOrWhiteSpace(l.warehouseCode) ? warehouseCode : l.warehouseCode!,
                            WarehouseName = l.warehouseName ?? "",
                            Zone = string.IsNullOrWhiteSpace(l.zone) ? groupZone : l.zone!,
                            Rack = l.rack ?? "",
                            Layer = l.layer ?? "",
                            Location = l.location ?? "",
                            InventoryStatus = l.inventoryStatus ?? ""
                        });
                    }

                    segments.Add(new LocationSegment
                    {
                        Zone = groupZone,
                        Items = items
                    });
                }
            }

            return segments;
        }
        finally { _httpGate.Release(); }
    }
    private static bool IsIdempotent(HttpMethod m) => m == HttpMethod.Get || m == HttpMethod.Head;

    private async Task<HttpResponseMessage> SendAsyncCore(
        HttpRequestMessage req, CancellationToken ct, int maxRetries = 0)
    {
        // 使用流式读取，避免大响应或损坏压缩导致 “Error while copying content to a stream”
        const HttpCompletionOption opt = HttpCompletionOption.ResponseHeadersRead;

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                var res = await _http.SendAsync(req, opt, ct).ConfigureAwait(false);
                return res;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries && IsIdempotent(req.Method))
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)); // 0.5s,1s,2s
                Log.Warning(ex, "HTTP 请求失败，准备重试 {Attempt}/{Max} {Method} {Url}", attempt + 1, maxRetries + 1, req.Method, req.RequestUri);
                await Task.Delay(delay, ct);
                // 需要重建请求消息（已被发送过）
                req = Clone(req);
            }
            catch (IOException ex) when (attempt < maxRetries && IsIdempotent(req.Method))
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt));
                Log.Warning(ex, "网络 IO 失败，准备重试 {Attempt}/{Max} {Method} {Url}", attempt + 1, maxRetries + 1, req.Method, req.RequestUri);
                await Task.Delay(delay, ct);
                req = Clone(req);
            }
        }
        static HttpRequestMessage Clone(HttpRequestMessage src)
        {
            var clone = new HttpRequestMessage(src.Method, src.RequestUri);
            // headers
            foreach (var h in src.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            // content（仅支持 StringContent/ByteArrayContent 这类一次性可复制的；本项目已满足）
            if (src.Content is HttpContent c)
            {
                var ms = new MemoryStream();
                c.CopyTo(ms, null, default);
                ms.Position = 0;
                var sc = new StreamContent(ms);
                foreach (var h in c.Headers) sc.Headers.TryAddWithoutValidation(h.Key, h.Value);
                clone.Content = sc;
            }
            return clone;
        }
    }
    private static void EnsureJson(HttpResponseMessage res)
    {
        var ct = res.Content.Headers.ContentType?.MediaType;
        // 某些服务会返回 "application/json;charset=UTF-8"
        if (ct is null) return;
        if (!ct.Contains("json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Content-Type 不是 JSON：{ct}");
    }

    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage res, AuthState auth, CancellationToken ct)
    {
        res.EnsureSuccessStatusCode();
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, auth, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json, JsonOpt);
    }


    private static async Task<string> PeekAsync(HttpContent content, int limit, CancellationToken ct)
    {
        await using var s = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var buf = new byte[limit];
        var n = await s.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false);
        return Encoding.UTF8.GetString(buf, 0, n);
    }


    private static async Task<string> ReadBodyAsStringAsync(HttpResponseMessage res, CancellationToken ct)
    {
        // 尝试读取两次（仅限读取阶段错误）
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var s = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, 81920, ct).ConfigureAwait(false);
                ms.Position = 0;

                // 文本编码：优先按 ContentType 的 charset
                var charset = res.Content.Headers.ContentType?.CharSet;
                Encoding enc;
                try { enc = !string.IsNullOrWhiteSpace(charset) ? Encoding.GetEncoding(charset!) : Encoding.UTF8; }
                catch { enc = Encoding.UTF8; }

                return enc.GetString(ms.ToArray());
            }
            catch (IOException) when (attempt == 0)
            {
                // 读取阶段偶发断流 → 重试一次
                await Task.Delay(200, ct).ConfigureAwait(false);
                continue;
            }
            catch (HttpRequestException ex) when (attempt == 0 &&
                ex.Message.IndexOf("copying content to a stream", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await Task.Delay(200, ct).ConfigureAwait(false);
                continue;
            }
        }
        // 第二次仍失败会落到上层调用处的 try/catch
        throw new HttpRequestException("Failed to read response body after retry.");
    }


    // DTO
    private sealed class WarehouseResp { public List<WarehouseDto>? result { get; set; } public bool success { get; set; } }
    private sealed class WarehouseDto { public string? warehouseCode { get; set; } public string? warehouseName { get; set; } }

}

// 应用模型
public class WarehouseItem
{
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
}
public class LocationItem
{
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Rack { get; set; } = "";
    public string Layer { get; set; } = "";
    public string Location { get; set; } = "";
    public string InventoryStatus { get; set; } = "";
}

