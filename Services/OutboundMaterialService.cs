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

public sealed class OutboundMaterialService : IOutboundMaterialService
{
    public readonly HttpClient _http;
    private readonly AuthState _auth;
    public readonly string _outboundListEndpoint;
    public readonly string _detailEndpoint;
    public readonly string _scanDetailEndpoint;
    public readonly string _scanByBarcodeEndpoint;
    public readonly string _scanConfirmEndpoint;
    public readonly string _cancelScanEndpoint;
    public readonly string _confirmOutstockEndpoint;
    public readonly string _judgeScanAllEndpoint;
    private readonly string _updateOutstockLocationEndpoint;
    private readonly string _updateQuantityEndpoint;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OutboundMaterialService(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;
        // 统一设置超时
        if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
            _http.Timeout = TimeSpan.FromSeconds(15);

        // === 新配置：scheme://ip:port + /{servicePath} ===
        var baseUrl = configLoader.GetBaseUrl(); // e.g. http://allysysindustrialsoft.aax6.cn:9128/normalService
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        // 取服务段做路径去重（如 /normalService）
        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";

        // 接受 JSON
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // === Endpoints：从配置读取相对路径（不含 /normalService），并做老配置兼容去重 ===
        _outboundListEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.list", "/pda/wmsMaterialOutstock/getOutStock"),
            servicePath);

        _detailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.detail", "/pda/wmsMaterialOutstock/getOutStockDetail"),
            servicePath);

        _scanDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.scanDetail", "/pda/wmsMaterialOutstock/getOutStockScanDetail"),
            servicePath);

        _scanByBarcodeEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.scanByBarcode", "/pda/wmsMaterialOutstock/getOutStockByBarcode"),
            servicePath);

        _scanConfirmEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.scanConfirm", "/pda/wmsMaterialOutstock/scanOutConfirm"),
            servicePath);

        _cancelScanEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.cancelScan", "/pda/wmsMaterialOutstock/cancelOutScan"),
            servicePath);

        _confirmOutstockEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.confirm", "/pda/wmsMaterialOutstock/confirm"),
            servicePath);

        _judgeScanAllEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.judgeScanAll", "/pda/wmsMaterialOutstock/judgeOutstockDetailScanAll"),
            servicePath);
        _updateOutstockLocationEndpoint = ServiceUrlHelper.NormalizeRelative(
    configLoader.GetApiPath("outbound.updateLocation", "/pda/wmsMaterialOutstock/updateLocation"),
    servicePath);

        _updateQuantityEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("outbound.updateQuantity", "/pda/wmsMaterialOutstock/updateOutQuantity"),
            servicePath);
    }

    // ---------------- 通用 HTTP 基础 ----------------

    private static bool IsIdempotent(HttpMethod m) => m == HttpMethod.Get || m == HttpMethod.Head;

    private async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage req, CancellationToken ct, int maxRetries = 0)
    {
        const HttpCompletionOption opt = HttpCompletionOption.ResponseHeadersRead;

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await _http.SendAsync(req, opt, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries && IsIdempotent(req.Method))
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt));
                Log.Warning(ex, "HTTP 失败，重试 {Attempt}/{Max} {Method} {Url}", attempt + 1, maxRetries + 1, req.Method, req.RequestUri);
                await Task.Delay(delay, ct);
                req = Clone(req);
            }
            catch (IOException ex) when (attempt < maxRetries && IsIdempotent(req.Method))
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt));
                Log.Warning(ex, "IO 失败，重试 {Attempt}/{Max} {Method} {Url}", attempt + 1, maxRetries + 1, req.Method, req.RequestUri);
                await Task.Delay(delay, ct);
                req = Clone(req);
            }
        }

        static HttpRequestMessage Clone(HttpRequestMessage src)
        {
            var clone = new HttpRequestMessage(src.Method, src.RequestUri);
            foreach (var h in src.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
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

    private static async Task<T?> ReadJsonAsync<T>(HttpContent content, CancellationToken ct)
    {
        await using var s = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(s, JsonOpt, ct).ConfigureAwait(false);
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
        return await ReadJsonAsync<T>(res.Content, ct).ConfigureAwait(false);
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
        return await ReadJsonAsync<TResp>(res.Content, ct).ConfigureAwait(false);
    }


    // ---------------- 业务接口（签名不变） ----------------

    public async Task<IEnumerable<OutboundOrderSummary>> ListOutboundOrdersAsync(
        string? orderNoOrBarcode,
        DateTime startDate,
        DateTime endDate,
        string[] outstockStatusList,
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
            pairs.Add(new("outstockNo", orderNoOrBarcode.Trim()));
        if (outstockStatusList is { Length: > 0 })
            pairs.Add(new("outstockStatusList", string.Join(",", outstockStatusList)));
        if (!string.IsNullOrWhiteSpace(orderType))
            pairs.Add(new("orderType", orderType));
        if (orderTypeList is { Length: > 0 })
            pairs.Add(new("orderTypeList", string.Join(",", orderTypeList)));

        using var res = new FormUrlEncodedContent(pairs);
        var qs = await PeekAsync(res, 2048, ct).ConfigureAwait(false);
        var url = _outboundListEndpoint + "?" + qs;

        var dto = await GetJsonAsync<GetOutStockPageResp>(url, ct).ConfigureAwait(false);
        var records = dto?.result?.records;
        if (dto?.success != true || records is null || records.Count == 0)
            return Enumerable.Empty<OutboundOrderSummary>();

        return records.Select(x => new OutboundOrderSummary(
            outstockId: x.id ?? "",
            outstockNo: x.outstockNo ?? "",
            orderType: x.orderType ?? "",
            orderTypeName: x.orderTypeName ?? "",
            workOrderNo: x.workOrderNo ?? "",
            returnNo: x.returnNo ?? "",
            deliveryNo: x.deliveryNo ?? "",
            requisitionMaterialNo: x.requisitionMaterialNo ?? "",
            customer: x.customer ?? "",
            deliveryMemo: x.deliveryMemo ?? "",
            expectedDeliveryTime: x.expectedDeliveryTime ?? "",
            memo: x.memo ?? "",
            saleNo: x.saleNo ?? "",
            createdTime: x.createdTime ?? ""
        ));
    }

    public async Task<IReadOnlyList<OutboundPendingRow>> GetOutStockDetailAsync(
    string outstockId, CancellationToken ct = default)
    {
        var url = $"{_detailEndpoint}?OutstockId={Uri.EscapeDataString(outstockId)}";

        // 直接调用 GetJsonAsync，失败时抛异常
        var dto = await GetJsonAsync<GetOutStockDetailResp>(url, ct).ConfigureAwait(false);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<OutboundPendingRow>();

        return dto.result.Select(x => new OutboundPendingRow(
            MaterialName: x.materialName ?? string.Empty,
            MaterialCode: x.materialCode ?? string.Empty,
            Spec: x.spec ?? string.Empty,
            Location: x.location ?? string.Empty,
            ProductionBatch: x.productionBatch ?? string.Empty,
            StockBatch: x.stockBatch ?? string.Empty,
            OutstockQty: ToInt(x.outstockQty),
            Qty: ToInt(x.qty)
        )).ToList();
    }


    public async Task<IReadOnlyList<OutboundScannedRow>> GetOutStockScanDetailAsync(
     string outstockId, CancellationToken ct = default)
    {
        var url = $"{_scanDetailEndpoint}?OutstockId={Uri.EscapeDataString(outstockId)}";

        // 直接调用 GetJsonAsync，失败会抛异常
        var dto = await GetJsonAsync<GetOutStockScanDetailResp>(url, ct).ConfigureAwait(false);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<OutboundScannedRow>();

        return dto.result.Select(x => new OutboundScannedRow(
            Barcode: (x.barcode ?? string.Empty).Trim(),
            DetailId: (x.id ?? string.Empty).Trim(),
            Location: (x.location ?? string.Empty).Trim(),
            MaterialName: (x.materialName ?? string.Empty).Trim(),
            Qty: ToInt(x.qty),
            OutstockQty: ToInt(x.outstockQty),
            Spec: (x.spec ?? string.Empty).Trim(),
            ScanStatus: x.scanStatus ?? false,
            WarehouseCode: x.warehouseCode?.Trim()
        )).ToList();
    }


    public async Task<SimpleOk> OutStockByBarcodeAsync(string outstockId, string barcode, CancellationToken ct = default)
    {
        var dto = await PostJsonAsync<object, ScanByBarcodeResp>(_scanByBarcodeEndpoint,
                  new { barcode, id = outstockId }, ct).ConfigureAwait(false);
        var ok = dto?.success == true || dto?.result?.ToString() == "true";
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

    public async Task<SimpleOk> ConfirmOutstockAsync(string outstockId, CancellationToken ct = default)
    {
        var dto = await PostJsonAsync<object, ConfirmResp>(_confirmOutstockEndpoint, new { id = outstockId }, ct)
                  .ConfigureAwait(false);
        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<bool> JudgeOutstockDetailScanAllAsync(string outstockId, CancellationToken ct = default)
    {
        var url = $"{_judgeScanAllEndpoint}?id={Uri.EscapeDataString(outstockId)}";
        var dto = await GetJsonAsync<JudgeScanAllResp>(url, ct).ConfigureAwait(false);
        return dto?.result == true;
    }

    public async Task<SimpleOk> UpdateOutstockLocationAsync(
    string detailId, string id, string outstockWarehouse, string outstockWarehouseCode, string location, CancellationToken ct = default)
    {
        var payload = new { detailId, id, outstockWarehouse, outstockWarehouseCode, location };

        var dto = await PostJsonAsync<object, UpdateLocationResp>(
            _updateOutstockLocationEndpoint,
            payload, ct).ConfigureAwait(false);

        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> UpdateQuantityAsync(
        string barcode, string detailId, string id, int quantity, CancellationToken ct = default)
    {
        var payload = new { barcode, detailId, id, quantity };

        var dto = await PostJsonAsync<object, ConfirmResp>(
            _updateQuantityEndpoint,
            payload, ct).ConfigureAwait(false);

        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
    }

    // -------- 工具 --------
    private static int ToInt(object? v)
    {
        if (v is null) return 0;
        return v switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)Math.Round(d, MidpointRounding.AwayFromZero),
            double db => (int)Math.Round(db, MidpointRounding.AwayFromZero),
            string s when int.TryParse(s.Trim(), out var i2) => i2,
            string s2 when decimal.TryParse(s2.Trim(), out var d2) => (int)Math.Round(d2, MidpointRounding.AwayFromZero),
            _ => 0
        };

    }
   

    private sealed class UpdateLocationResp
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool? result { get; set; }
        public bool? success { get; set; }
    }
}


public class GetOutStockItem
    {
        public string? arrivalNo { get; set; }
        public string? createdTime { get; set; }
        public string? outstockId { get; set; }
        public string? outstockNo { get; set; }
        public string? orderType { get; set; }
        public string? purchaseNo { get; set; }
        public string? supplierName { get; set; }
    }
    public  class GetOutStockDetailResp
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int? code { get; set; }
        public List<GetOutStockDetailItem>? result { get; set; }
        public int? costTime { get; set; }
    }
    public  class GetOutStockDetailItem
    {
        public string? id { get; set; }                     // 入库单明细主键id
        public string? outstockNo { get; set; }              // 入库单号
        public string? materialName { get; set; }
        public string? outstockWarehouseCode { get; set; }   // 入库仓库编码
        public string? materialCode { get; set; } //产品编码
        public string? spec { get; set; } //规格
        public string? location { get; set; } //出库库位
        public string? productionBatch { get; set; } //生产批号

        public string? stockBatch { get; set; } //批次号
        public decimal? outstockQty { get; set; } //出库数量
        public decimal? qty { get; set; } //已扫描数
    }

    public class GetOutStockPageResp
    {
        public int code { get; set; }
        public long costTime { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public GetOutStockPageData? result { get; set; }
    }

    public class GetOutStockPageData
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<GetOutStockRecord> records { get; set; } = new();
    }

    public class GetOutStockRecord
    {
        public string? id { get; set; }
        public string? outstockNo { get; set; }
        public string? orderType { get; set; }
        public string? orderTypeName { get; set; }
        public string? workOrderNo { get; set; }
        public string? materialName { get; set; }
        public string? requisitionMaterialNo { get; set; }
        public string? returnNo { get; set; }
        public string? deliveryNo { get; set; }
        public string? customer { get; set; }
        public string? deliveryMemo { get; set; }
        public string? expectedDeliveryTime { get; set; }
        public string? memo { get; set; }
        public string? saleNo { get; set; }
        public string? createdTime { get; set; }
    }
    public  class GetOutStockScanDetailResp
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int? code { get; set; }
        public List<GetOutStockScanDetailItem>? result { get; set; }
        public int? costTime { get; set; }
    }

    public class GetOutStockScanDetailItem
    {
        public string? id { get; set; }              // 入库单明细主键 id
        public string? barcode { get; set; }
        public string? materialName { get; set; }
        public string? spec { get; set; }
        public decimal? qty { get; set; }             // 可能是 null 或 “数字字符串”
        public decimal? outstockQty { get; set; }
        public string? warehouseCode { get; set; }
        public string? location { get; set; }
        public bool? scanStatus { get; set; }        // 可能为 null，按 false 处理
    }

