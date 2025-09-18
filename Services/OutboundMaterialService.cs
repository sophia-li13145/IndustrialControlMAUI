using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services;

/// <summary>
/// 真实接口实现，风格对齐 WorkOrderApi
/// </summary>
public sealed class OutboundMaterialService : IOutboundMaterialService
{
    public readonly HttpClient _http;
    public readonly string _outboundListEndpoint;
    public readonly string _detailEndpoint;
    public readonly string _scanDetailEndpoint;
    // 新增：扫码入库端点
    public readonly string _scanByBarcodeEndpoint;
    public readonly string _scanConfirmEndpoint;
    public readonly string _cancelScanEndpoint;
    public readonly string _confirmOutstockEndpoint;
    public readonly string _judgeScanAllEndpoint;
    private readonly JsonSerializerOptions _opt;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public OutboundMaterialService(HttpClient http, IConfigLoader configLoader)
    {
        _http = http;
        _opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        JsonNode cfg = configLoader.Load();

        // ⭐ 新增：读取 baseUrl 或 ip+port
        var baseUrl =
            (string?)cfg?["server"]?["baseUrl"]
            ?? BuildBaseUrl(cfg?["server"]?["ipAddress"], cfg?["server"]?["port"]);

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("后端基础地址未配置：请在 appconfig.json 配置 server.baseUrl 或 server.ipAddress + server.port");

        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        // 下面保持原来的相对路径读取（不变）
        _outboundListEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["list"] ??
            (string?)cfg?["apiEndpoints"]?["getOutStock"] ??
            "/normalService/pda/wmsMaterialOutstock/getOutStock";

        _detailEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["detail"] ??
            "/normalService/pda/wmsMaterialOutstock/getOutStockDetail";

        _scanDetailEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["scanDetail"] ??
            "/normalService/pda/wmsMaterialOutstock/getOutStockScanDetail";

        _scanByBarcodeEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["scanByBarcode"] ??
            "/normalService/pda/wmsMaterialOutstock/getOutStockByBarcode";

        _scanConfirmEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["scanConfirm"] ??
            "/normalService/pda/wmsMaterialOutstock/scanOutConfirm";

        _cancelScanEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["cancelScan"] ??
            "/normalService/pda/wmsMaterialOutstock/cancelOutScan";

        _confirmOutstockEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["confirm"] ??
            "/normalService/pda/wmsMaterialOutstock/confirm";

        _judgeScanAllEndpoint =
            (string?)cfg?["apiEndpoints"]?["outbound"]?["judgeScanAll"] ??
            "/normalService/pda/wmsMaterialOutstock/judgeOutstockDetailScanAll";
    }

    // ⭐ 新增：拼接 ip + port → baseUrl
    private static string? BuildBaseUrl(JsonNode? ipNode, JsonNode? portNode)
    {
        string? ip = ipNode?.ToString().Trim();
        string? port = portNode?.ToString().Trim();

        if (string.IsNullOrWhiteSpace(ip)) return null;

        // 如果没带 http:// 或 https://，默认 http://
        var hasScheme = ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var host = hasScheme ? ip : $"http://{ip}";

        return string.IsNullOrEmpty(port) ? host : $"{host}:{port}";
    }


    public async Task<IEnumerable<OutboundOrderSummary>> ListOutboundOrdersAsync(
    string? orderNoOrBarcode,
    DateTime startDate,
    DateTime endDate,
    string[] outstockStatusList,
    string orderType,
    string[] orderTypeList,
    CancellationToken ct = default)
    {
        // 结束时间扩到当天 23:59:59，避免把当日数据排除
        var begin = startDate.ToString("yyyy-MM-dd 00:00:00");
        var end = endDate.ToString("yyyy-MM-dd 23:59:59");

        // 用 KVP 列表（不要 Dictionary）→ 规避 WinRT generic + AOT 警告
        var pairs = new List<KeyValuePair<string, string>>
    {
        new("createdTimeBegin", begin),
        new("createdTimeEnd",   end),
        new("pageNo",  "1"),
        new("pageSize","50")
        // 如需统计总数：new("searchCount", "true")
    };

        if (!string.IsNullOrWhiteSpace(orderNoOrBarcode))
            pairs.Add(new("outstockNo", orderNoOrBarcode.Trim()));

        if (outstockStatusList is { Length: > 0 })
            pairs.Add(new("outstockStatusList", string.Join(",", outstockStatusList)));

        if (!string.IsNullOrWhiteSpace(orderType))
            pairs.Add(new("orderType", orderType));

        if (orderTypeList is { Length: > 0 })
            pairs.Add(new("orderTypeList", string.Join(",", orderTypeList)));

        // 交给 BCL 编码（比手写 Escape 安全）
        using var form = new FormUrlEncodedContent(pairs);
        var qs = await form.ReadAsStringAsync(ct);
        var url = _outboundListEndpoint + "?" + qs;

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return Enumerable.Empty<OutboundOrderSummary>();

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetOutStockPageResp>(json, opt);

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
        // ✅ 文档为 GET + x-www-form-urlencoded，参数名是小写 outstockId
        var url = $"{_detailEndpoint}?outstockId={Uri.EscapeDataString(outstockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetOutStockDetailResp>(json, opt);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<OutboundPendingRow>();

        // ⚠️ 接口没有 barcode，这里先用空串；如需展示可以改成 x.materialCode 或 x.stockBatch
        var list = dto.result.Select(x => new OutboundPendingRow(
            MaterialName: x.materialName ?? string.Empty,
            MaterialCode: x.materialCode ?? string.Empty,
            Spec: x.spec ?? string.Empty,
            Location: x.location ?? string.Empty,
            ProductionBatch: x.productionBatch ?? string.Empty,
            StockBatch: x.stockBatch ?? string.Empty,
            OutstockQty: ToInt(x.outstockQty),         // 此处再转 int
            Qty: ToInt(x.qty)      // ← 已扫描量
        )).ToList();

        return list;
    }

    static int ToInt(decimal? v) => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : 0;
    public async Task<IReadOnlyList<OutboundScannedRow>> GetOutStockScanDetailAsync(
     string outstockId,
     CancellationToken ct = default)
    {
        // 文档为 GET + x-www-form-urlencoded，这里用 query 传递（关键在大小写常为 OutstockId）
        var url = $"{_scanDetailEndpoint}?OutstockId={Uri.EscapeDataString(outstockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetOutStockScanDetailResp>(json, opt);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<OutboundScannedRow>();

        // 映射：OutstockId <- id（截图注释“入库单明细主键id”）
        var list = dto.result.Select(x => new OutboundScannedRow(
            Barcode: (x.barcode ?? string.Empty).Trim(),
            DetailId: (x.id ?? string.Empty).Trim(),
            Location: (x.location ?? string.Empty).Trim(),
            MaterialName: (x.materialName ?? string.Empty).Trim(),
            Qty: ToInt(x.qty),
            Spec: (x.spec ?? string.Empty).Trim(),
            ScanStatus: x.scanStatus ?? false,
            WarehouseCode: x.warehouseCode?.Trim()
        )).ToList();

        return list;
    }

    // ========= 扫码入库实现 =========
    public async Task<SimpleOk> OutStockByBarcodeAsync(string outstockId, string barcode, CancellationToken ct = default)
    {
        // 注意：接口要的是 id 不是 outstockId
        var body = JsonSerializer.Serialize(new { barcode, id = outstockId });

        using var req = new HttpRequestMessage(HttpMethod.Post, _scanByBarcodeEndpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        var dto = JsonSerializer.Deserialize<ScanByBarcodeResp>(json, _opt);
        var ok = dto?.success == true || dto?.result?.ToString() == "true";
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> ScanConfirmAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default)
    {
        var payload = items.Select(x => new { barcode = x.barcode, id = x.id });
        var bodyJson = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, _scanConfirmEndpoint)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<ScanConfirmResp>(json, _opt);

        var ok = dto?.success == true;       // 你的接口：success=true 且 result=true
        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> CancelScanAsync(IEnumerable<(string barcode, string id)> items, CancellationToken ct = default)
    {
        var payload = items.Select(x => new { barcode = x.barcode, id = x.id });
        var bodyJson = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, _cancelScanEndpoint)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<CancelScanResp>(json, _opt);

        var ok = dto?.success == true;
        return new SimpleOk(ok, dto?.message);
    }


    public async Task<SimpleOk> ConfirmOutstockAsync(string outstockId, CancellationToken ct = default)
    {
        var bodyJson = JsonSerializer.Serialize(new { id = outstockId });
        using var req = new HttpRequestMessage(HttpMethod.Post, _confirmOutstockEndpoint)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<ConfirmResp>(json, opt);

        var ok = dto?.success == true;
        return new SimpleOk(ok, dto?.message);
    }
    /// <summary>
    /// 判断入库单明细是否已全部扫描确认
    /// </summary>
    /// <param name="outstockId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> JudgeOutstockDetailScanAllAsync(string outstockId, CancellationToken ct = default)
    {
        var url = $"{_judgeScanAllEndpoint}?id={Uri.EscapeDataString(outstockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<JudgeScanAllResp>(json, opt);

        // 按文档：看 result（true/false）；若接口异常或无 result，则返回 false 让前端提示/二次确认
        return dto?.result == true;
    }


    public async Task<SimpleOk> UpdateOutstockLocationAsync(
    string detailId, string id, string outstockWarehouse, string outstockWarehouseCode, string location, CancellationToken ct = default)
    {
        var url = "/normalService/pda/wmsMaterialOutstock/updateLocation";
        var payload = new
        {
            detailId,
            id,
            outstockWarehouse,
            outstockWarehouseCode,
            location
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        // 假设响应：{ code, message, result: true/false, success: true/false }
        var dto = JsonSerializer.Deserialize<UpdateLocationResp>(body, _json);
        var ok = dto?.success == true || dto?.result == true;

        return new SimpleOk(ok, dto?.message);
    }

    public async Task<SimpleOk> UpdateQuantityAsync(
    string barcode, string detailId, string id, int quantity, CancellationToken ct = default)
    {
        var url = "/normalService/pda/wmsMaterialOutstock/updateQuantity";
        var payload = new { barcode, detailId, id, quantity };
        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        // 响应格式：{ success, message, code, result, ... }（与截图一致）
        var dto = JsonSerializer.Deserialize<ConfirmResp>(body, _json);
        var ok = dto?.success == true || dto?.result == true;
        return new SimpleOk(ok, dto?.message);
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
    public sealed class GetOutStockDetailResp
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int? code { get; set; }
        public List<GetOutStockDetailItem>? result { get; set; }
        public int? costTime { get; set; }
    }
    public sealed class GetOutStockDetailItem
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
        public int outstockQty { get; set; } //出库数量
        public int qty { get; set; } //已扫描数
    }

    private sealed class UpdateLocationResp
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool? result { get; set; }
        public bool? success { get; set; }
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
    public sealed class GetOutStockScanDetailResp
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
        public string? warehouseCode { get; set; }
        public string? location { get; set; }
        public bool? scanStatus { get; set; }        // 可能为 null，按 false 处理
    }

}
