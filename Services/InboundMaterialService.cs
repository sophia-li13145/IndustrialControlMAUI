using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Tools;
using IndustrialControlMAUI.ViewModels;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services;

public sealed class InboundMaterialService : IInboundMaterialService
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    // endpoints（与你原始文件一致）
    public readonly string _inboundListEndpoint;
    public readonly string _detailEndpoint;
    public readonly string _scanDetailEndpoint;
    public readonly string _scanByBarcodeEndpoint;
    public readonly string _scanConfirmEndpoint;
    public readonly string _cancelScanEndpoint;
    public readonly string _confirmInstockEndpoint;
    public readonly string _judgeScanAllEndpoint;
    public readonly string _pageLocationQuery;
    private readonly string _getInStockLocationEndpoint;
    private readonly string _updateLocationEndpoint;
    private readonly string _updateQuantityEndpoint;

    // 统一 JSON 选项
    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InboundMaterialService(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;
        // 读取一次配置（如需其它字段）
        JsonNode cfg = configLoader.Load();

        // ① 基地址：scheme://ip:port + /{servicePath}
        var baseUrl = configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        // 拿到服务路径用于兼容处理（如 /normalService）
        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

        // ② Header：接受 JSON
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // ③ Endpoints：统一从配置取“相对服务路径”的地址（新配置已去掉 normalService 前缀）
        //    若老配置仍带 /normalService/ 前缀，会被 NormalizeRelative 去重。
        _inboundListEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.list", "/pda/wmsMaterialInstock/getInStock"), servicePath);
        _detailEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.detail", "/pda/wmsMaterialInstock/getInStockDetail"), servicePath);
        _scanDetailEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.scanDetail", "/pda/wmsMaterialInstock/getInStockScanDetail"), servicePath);
        _scanByBarcodeEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.scanByBarcode", "/pda/wmsMaterialInstock/getInStockByBarcode"), servicePath);
        _scanConfirmEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.scanConfirm", "/pda/wmsMaterialInstock/scanConfirm"), servicePath);
        _cancelScanEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.cancelScan", "/pda/wmsMaterialInstock/cancelScan"), servicePath);
        _confirmInstockEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.confirm", "/pda/wmsMaterialInstock/confirm"), servicePath);
        _judgeScanAllEndpoint = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.judgeScanAll", "/pda/wmsMaterialInstock/judgeInstockDetailScanAll"), servicePath);

        // 如果你有此接口，最好也放进配置：apiEndpoints.inbound.pageLocationQuery
        _pageLocationQuery = ServiceUrlHelper.NormalizeRelative(configLoader.GetApiPath("inbound.pageLocationQuery", "/pda/wmsMaterialInstock/pageLocationQuery"), servicePath);
        _getInStockLocationEndpoint = ServiceUrlHelper.NormalizeRelative(
         configLoader.GetApiPath("inbound.getInStockLocation", "/pda/wmsMaterialInstock/getInStockLocation"),
         servicePath);

        _updateLocationEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("inbound.updateLocation", "/pda/wmsMaterialInstock/updateLocation"),
            servicePath);

        _updateQuantityEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("inbound.updateQuantity", "/pda/wmsMaterialInstock/updateQuantity"),
            servicePath);
      
    }

    // ======================
    // 通用 HTTP 基础方法
    // ======================

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
        var mt = res.Content.Headers.ContentType?.MediaType ?? "";
        if (!mt.Contains("json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"期望 JSON，实际返回 Content-Type: {mt}");
    }

    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage res, AuthState auth, CancellationToken ct)
    {
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

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        string requestUrl;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // 已经是绝对地址
            requestUrl = url;
        }
        else
        {
            // 手动拼接
            var baseUrl = _http.BaseAddress?.AbsoluteUri ?? throw new InvalidOperationException("BaseAddress 未配置");

            // 确保 baseUrl 以 "/" 结尾
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            // 去掉 url 前导 "/"
            var relative = url.TrimStart('/');

            requestUrl = baseUrl + relative;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUrl, UriKind.Absolute));
        using var res = await SendAsyncCore(req, ct).ConfigureAwait(false);

        if (!res.IsSuccessStatusCode)
        {
            var head = await PeekAsync(res.Content, 2048, ct);
            Log.Warning("GET {Url} 非 2xx：{Status} 预览：{Head}", requestUrl, (int)res.StatusCode, head);
            res.EnsureSuccessStatusCode();
        }

        EnsureJson(res);
        return await ReadJsonAsync<T>(res,_auth, ct).ConfigureAwait(false);
    }

    private async Task<TResp?> PostJsonAsync<TReq, TResp>(string url, TReq body, CancellationToken ct)
    {
        var requestUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        var json = JsonSerializer.Serialize(body, JsonOpt);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(requestUrl, UriKind.Absolute))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var res = await SendAsyncCore(req, ct).ConfigureAwait(false);

        if (!res.IsSuccessStatusCode)
        {
            var head = await PeekAsync(res.Content, 2048, ct);
            Log.Warning("POST {Url} 非 2xx：{Status} 预览：{Head}", requestUrl, (int)res.StatusCode, head);
            res.EnsureSuccessStatusCode();
        }

        EnsureJson(res);
        return await ReadJsonAsync<TResp>(res,_auth, ct).ConfigureAwait(false);
    }


    // ======================
    // 业务方法（与你原签名一致）
    // ======================

    public async Task<IEnumerable<InboundOrderSummary>> ListInboundOrdersAsync(
        string? orderNoOrBarcode,
        DateTime startDate,
        DateTime endDate,
        string[] instockStatusList,
        string orderType,
        string[] orderTypeList,
        CancellationToken ct = default)
    {
        var begin = startDate.ToString("yyyy-MM-dd 00:00:00");
        var end = endDate.ToString("yyyy-MM-dd 23:59:59");

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("createdTimeBegin", begin),
            new("createdTimeEnd", end),
            new("pageNo", "1"),
            new("pageSize", "50")
        };
        if (!string.IsNullOrWhiteSpace(orderNoOrBarcode))
            pairs.Add(new("instockNo", orderNoOrBarcode.Trim()));
        if (instockStatusList is { Length: > 0 })
            pairs.Add(new("instockStatusList", string.Join(",", instockStatusList)));
        if (!string.IsNullOrWhiteSpace(orderType))
            pairs.Add(new("orderType", orderType));
        if (orderTypeList is { Length: > 0 })
            pairs.Add(new("orderTypeList", string.Join(",", orderTypeList)));

        using var form = new FormUrlEncodedContent(pairs);
        var qs = await form.ReadAsStringAsync(ct).ConfigureAwait(false);
        var url = _inboundListEndpoint + "?" + qs;

        var dto = await GetJsonAsync<GetInStockPageResp>(url, ct).ConfigureAwait(false);
        var records = dto?.result?.records;
        if (dto?.success != true || records is null || records.Count == 0)
            return Enumerable.Empty<InboundOrderSummary>();

        return records.Select(x => new InboundOrderSummary(
            instockId: x.id ?? "",
            instockNo: x.instockNo ?? "",
            orderType: x.orderType ?? "",
            orderTypeName: x.orderTypeName ?? "",
            purchaseNo: x.purchaseNo ?? "",
            supplierName: x.supplierName ?? "",
            arrivalNo: x.arrivalNo ?? "",
            workOrderNo: x.workOrderNo ?? "",
            materialName: x.materialName ?? "",
            instockQty: ToInt(x.instockQty),
            createdTime: x.createdTime ?? ""
        ));
    }

    public async Task<IReadOnlyList<InboundPendingRow>> GetInStockDetailAsync(
        string instockId, CancellationToken ct = default)
    {
        var url = $"{_detailEndpoint}?instockId={Uri.EscapeDataString(instockId)}";
        var dto = await GetJsonAsync<GetInStockDetailResp>(url, ct).ConfigureAwait(false);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<InboundPendingRow>();

        return dto.result.Select(x => new InboundPendingRow(
            Barcode: string.Empty,
            DetailId: x.id ?? string.Empty,
            Location: x.location ?? string.Empty,
            MaterialName: x.materialName ?? string.Empty,
            PendingQty: ToInt(x.instockQty),
            ScannedQty: ToInt(x.qty),
            Spec: x.spec ?? string.Empty
        )).ToList();
    }

    public async Task<IReadOnlyList<InboundScannedRow>> GetInStockScanDetailAsync(
        string instockId, CancellationToken ct = default)
    {
        var url = $"{_scanDetailEndpoint}?InstockId={Uri.EscapeDataString(instockId)}";
        var dto = await GetJsonAsync<GetInStockScanDetailResp>(url, ct).ConfigureAwait(false);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<InboundScannedRow>();

        return dto.result.Select(x => new InboundScannedRow(
            Barcode: (x.barcode ?? string.Empty).Trim(),
            DetailId: (x.id ?? string.Empty).Trim(),
            Location: (x.location ?? string.Empty).Trim(),
            MaterialName: (x.materialName ?? string.Empty).Trim(),
            Qty: ToInt(x.qty),
            Spec: (x.spec ?? string.Empty).Trim(),
            ScanStatus: x.scanStatus ?? false,
            WarehouseCode: x.warehouseCode?.Trim()
        )).ToList();
    }

    public async Task<SimpleOk> InStockByBarcodeAsync(string instockId, string barcode, CancellationToken ct = default)
    {
        var dto = await PostJsonAsync<object, ScanByBarcodeResp>(
            _scanByBarcodeEndpoint, new { barcode, id = instockId }, ct).ConfigureAwait(false);

        var ok = dto?.success == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> ScanConfirmAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default)
    {
        var payload = items.Select(x => new { barcode = x.barcode, id = x.id }).ToArray();
        var dto = await PostJsonAsync<object, ScanConfirmResp>(_scanConfirmEndpoint, payload, ct).ConfigureAwait(false);
        var ok = dto?.success == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> CancelScanAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default)
    {
        var payload = items.Select(x => new { barcode = x.barcode, id = x.id }).ToArray();
        var dto = await PostJsonAsync<object, CancelScanResp>(_cancelScanEndpoint, payload, ct).ConfigureAwait(false);
        var ok = dto?.success == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> ConfirmInstockAsync(string instockId, CancellationToken ct = default)
    {
        var dto = await PostJsonAsync<object, ConfirmResp>(_confirmInstockEndpoint, new { id = instockId }, ct).ConfigureAwait(false);
        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<bool> JudgeInstockDetailScanAllAsync(string instockId, CancellationToken ct = default)
    {
        var url = $"{_judgeScanAllEndpoint}?id={Uri.EscapeDataString(instockId)}";
        var dto = await GetJsonAsync<JudgeScanAllResp>(url, ct).ConfigureAwait(false);
        return dto?.result == true;
    }

    public async Task<List<LocationNodeDto>> GetLocationTreeAsync(CancellationToken ct = default)
    {
        var dto = await GetJsonAsync<InStockLocationResp>(_getInStockLocationEndpoint, ct)
                      .ConfigureAwait(false);
        return dto?.result?.children ?? new();
    }

    public async Task<List<BinInfo>> GetBinsByLayerAsync(
        string warehouseCode, string layer, int pageNo = 1, int pageSize = 50, int status = 1, CancellationToken ct = default)
    {
        var url = $"{_pageLocationQuery}?warehouseCode={Uri.EscapeDataString(warehouseCode)}&layer={Uri.EscapeDataString(layer)}&pageNo={pageNo}&pageSize={pageSize}&status={status}";
        var dto = await GetJsonAsync<PageLocationResp>(url, ct).ConfigureAwait(false);

        if (dto?.success != true || dto.result?.records is null || dto.result.records.Count == 0)
            return new();

        return dto.result.records.Select(r => new BinInfo
        {
            Id = r.id ?? string.Empty,
            FactoryCode = r.factoryCode ?? string.Empty,
            FactoryName = r.factoryName ?? string.Empty,
            WarehouseCode = r.warehouseCode ?? string.Empty,
            WarehouseName = r.warehouseName ?? string.Empty,
            ZoneCode = r.zone ?? string.Empty,
            ZoneName = r.zoneName ?? string.Empty,
            RackCode = r.rack ?? string.Empty,
            RackName = r.rackName ?? string.Empty,
            LayerCode = r.layer ?? string.Empty,
            LayerName = r.layerName ?? string.Empty,
            Location = r.location ?? string.Empty,
            InventoryStatus = r.inventoryStatus ?? string.Empty,
            InStock = string.Equals(r.inventoryStatus, "instock", StringComparison.OrdinalIgnoreCase),
            Status = int.TryParse(r.status, out var st) ? st : 0,
            Memo = r.memo ?? string.Empty,
            DelStatus = r.delStatus ?? false,
            Creator = r.creator ?? string.Empty,
            CreatedTime = DateTime.TryParse(r.createdTime, out var cdt) ? cdt : null,
            Modifier = r.modifier ?? string.Empty,
            ModifiedTime = DateTime.TryParse(r.modifiedTime, out var mdt) ? mdt : null,
        }).ToList();
    }

    public async Task<SimpleOk> UpdateInstockLocationAsync(
    string detailId, string id, string instockWarehouse, string instockWarehouseCode, string location, CancellationToken ct = default)
    {
        var payload = new { detailId, id, instockWarehouse, instockWarehouseCode, location };
        var dto = await PostJsonAsync<object, UpdateLocationResp>(_updateLocationEndpoint, payload, ct)
                      .ConfigureAwait(false);

        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> UpdateQuantityAsync(
        string barcode, string detailId, string id, int quantity, CancellationToken ct = default)
    {
        var payload = new { barcode, detailId, id, quantity };
        var dto = await PostJsonAsync<object, ConfirmResp>(_updateQuantityEndpoint, payload, ct)
                      .ConfigureAwait(false);

        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    // ===== 工具：多形态转 int =====
    private static int ToInt(object? v)
    {
        if (v is null) return 0;
        switch (v)
        {
            case int i: return i;
            case long l: return (int)l;
            case decimal d: return (int)Math.Round(d, MidpointRounding.AwayFromZero);
            case double db: return (int)Math.Round(db, MidpointRounding.AwayFromZero);
            case string s when int.TryParse(s.Trim(), out var i2): return i2;
            case string s2 when decimal.TryParse(s2.Trim(), out var d2): return (int)Math.Round(d2, MidpointRounding.AwayFromZero);
            default: return 0;
        }
    }

    // 内部响应 DTO（仅用于 updateLocation）
    private sealed class UpdateLocationResp
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool? result { get; set; }
        public bool? success { get; set; }
    }
}


// ====== DTO（按接口示例字段） ======
public class GetInStockReq
{
    public string? createdTime { get; set; }
    public string? endTime { get; set; }
    public string? instockNo { get; set; }
    public string? orderType { get; set; }
    public string? startTime { get; set; }
}

public class GetInStockResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public List<GetInStockItem>? result { get; set; }
}

public class GetInStockItem
{
    public string? arrivalNo { get; set; }
    public string? createdTime { get; set; }
    public string? instockId { get; set; }
    public string? instockNo { get; set; }
    public string? orderType { get; set; }
    public string? purchaseNo { get; set; }
    public string? supplierName { get; set; }
}
public sealed class GetInStockDetailResp
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public List<GetInStockDetailItem>? result { get; set; }
    public int? costTime { get; set; }
}
public sealed class GetInStockDetailItem
{
    public string? id { get; set; }                     // 入库单明细主键id
    public string? instockNo { get; set; }              // 入库单号
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? spec { get; set; }
    public string? stockBatch { get; set; }

    public decimal? instockQty { get; set; }           // 预计数量(字符串/可能为空)
    public string? instockWarehouseCode { get; set; }   // 入库仓库编码
    public string? location { get; set; }               // 内点库位
    public decimal? qty { get; set; }                    // 已扫描量(字符串/可能为空)
}


public class ScanRow
{
    public string? barcode { get; set; }
    public string? instockId { get; set; }
    public string? location { get; set; }
    public string? materialName { get; set; }
    public string? qty { get; set; }
    public string? spec { get; set; }
}

public class ScanByBarcodeResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public object? result { get; set; }   // 文档里 result 只是 bool/无结构，这里占位
    public bool success { get; set; }
}
public class ScanConfirmResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public object? result { get; set; }
    public bool success { get; set; }
}
public class CancelScanResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public object? result { get; set; }
    public bool success { get; set; }
}
public class ConfirmResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool? result { get; set; }
    public bool? success { get; set; }
}
public class JudgeScanAllResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public bool? result { get; set; } // 文档中为布尔
}
public class GetInStockPageResp
{
    public int code { get; set; }
    public long costTime { get; set; }
    public string? message { get; set; }
    public bool success { get; set; }
    public GetInStockPageData? result { get; set; }
}

public class GetInStockPageData
{
    public int pageNo { get; set; }
    public int pageSize { get; set; }
    public long total { get; set; }
    public List<GetInStockRecord> records { get; set; } = new();
}

public class GetInStockRecord
{
    public string? id { get; set; }
    public string? instockNo { get; set; }
    public string? orderType { get; set; }
    public string? orderTypeName { get; set; }
    public string? supplierName { get; set; }
    public string? arrivalNo { get; set; }
    public string? purchaseNo { get; set; }
    public string? workOrderNo { get; set; }
    public string? materialName { get; set; }
    public decimal? instockQty { get; set; }
    public string? createdTime { get; set; }
}
public sealed class GetInStockScanDetailResp
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int? code { get; set; }
    public List<GetInStockScanDetailItem>? result { get; set; }
    public int? costTime { get; set; }
}

public class GetInStockScanDetailItem
{
    public string? id { get; set; }              // 入库单明细主键 id
    public string? barcode { get; set; }
    public string? materialName { get; set; }
    public string? spec { get; set; }
    public decimal? qty { get; set; }             // 可能是 null 或 “数字字符串”
    public string? warehouseCode { get; set; }
    public string? location { get; set; }
    public bool? scanStatus { get; set; }        // 可能为 null，按 false 处理
}



