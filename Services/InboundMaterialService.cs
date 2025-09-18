using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.Services;

/// <summary>
/// 真实接口实现，风格对齐 WorkOrderApi
/// </summary>
public sealed class InboundMaterialService : IInboundMaterialService
{
    public readonly HttpClient _http;
    public readonly string _inboundListEndpoint;
    public readonly string _detailEndpoint;
    public readonly string _scanDetailEndpoint;
    // 新增：扫码入库端点
    public readonly string _scanByBarcodeEndpoint;
    public readonly string _scanConfirmEndpoint;
    public readonly string _cancelScanEndpoint;
    public readonly string _confirmInstockEndpoint;
    public readonly string _judgeScanAllEndpoint;
    public readonly string _pageLocationQuery;
    private readonly JsonSerializerOptions _opt;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public InboundMaterialService(HttpClient http, IConfigLoader configLoader)
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
        _inboundListEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["list"] ??
            (string?)cfg?["apiEndpoints"]?["getInStock"] ??
            "/normalService/pda/wmsMaterialInstock/getInStock";

        _detailEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["detail"] ??
            "/normalService/pda/wmsMaterialInstock/getInStockDetail";

        _scanDetailEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["scanDetail"] ??
            "/normalService/pda/wmsMaterialInstock/getInStockScanDetail";

        _scanByBarcodeEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["scanByBarcode"] ??
            "/normalService/pda/wmsMaterialInstock/getInStockByBarcode";

        _scanConfirmEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["scanConfirm"] ??
            "/normalService/pda/wmsMaterialInstock/scanConfirm";

        _cancelScanEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["cancelScan"] ??
            "/normalService/pda/wmsMaterialInstock/cancelScan";

        _confirmInstockEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["confirm"] ??
            "/normalService/pda/wmsMaterialInstock/confirm";

        _judgeScanAllEndpoint =
            (string?)cfg?["apiEndpoints"]?["inbound"]?["judgeScanAll"] ??
            "/normalService/pda/wmsMaterialInstock/judgeInstockDetailScanAll";
        _pageLocationQuery = "/normalService/pda/wmsMaterialInstock/pageLocationQuery";
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


    public async Task<IEnumerable<InboundOrderSummary>> ListInboundOrdersAsync(
    string? orderNoOrBarcode,
    DateTime startDate,
    DateTime endDate,
    string[] instockStatusList,
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
            pairs.Add(new("instockNo", orderNoOrBarcode.Trim()));

        if (instockStatusList is { Length: > 0 })
            pairs.Add(new("instockStatusList", string.Join(",", instockStatusList)));

        if (!string.IsNullOrWhiteSpace(orderType))
            pairs.Add(new("orderType", orderType));

        if (orderTypeList is { Length: > 0 })
            pairs.Add(new("orderTypeList", string.Join(",", orderTypeList)));

        // 交给 BCL 编码（比手写 Escape 安全）
        using var form = new FormUrlEncodedContent(pairs);
        var qs = await form.ReadAsStringAsync(ct);
        var url = _inboundListEndpoint + "?" + qs;

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return Enumerable.Empty<InboundOrderSummary>();

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetInStockPageResp>(json, opt);

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
        // ✅ 文档为 GET + x-www-form-urlencoded，参数名是小写 instockId
        var url = $"{_detailEndpoint}?instockId={Uri.EscapeDataString(instockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetInStockDetailResp>(json, opt);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<InboundPendingRow>();

        // ⚠️ 接口没有 barcode，这里先用空串；如需展示可以改成 x.materialCode 或 x.stockBatch
        var list = dto.result.Select(x => new InboundPendingRow(
            Barcode: string.Empty,                 // 或 $"{x.materialCode}" / $"{x.stockBatch}"
            DetailId: x.id ?? string.Empty,        // ← 改为接口的 id
            Location: x.location ?? string.Empty,
            MaterialName: x.materialName ?? string.Empty,
            PendingQty: ToInt(x.instockQty),         // 此处再转 int
            ScannedQty: ToInt(x.qty),      // ← 已扫描量
            Spec: x.spec ?? string.Empty
        )).ToList();

        return list;
    }
    static int ToInt(decimal? v) => v.HasValue ? (int)Math.Round(v.Value, MidpointRounding.AwayFromZero) : 0;
    public async Task<IReadOnlyList<InboundScannedRow>> GetInStockScanDetailAsync(
     string instockId,
     CancellationToken ct = default)
    {
        // 文档为 GET + x-www-form-urlencoded，这里用 query 传递（关键在大小写常为 InstockId）
        var url = $"{_scanDetailEndpoint}?InstockId={Uri.EscapeDataString(instockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<GetInStockScanDetailResp>(json, opt);

        if (dto?.success != true || dto.result is null || dto.result.Count == 0)
            return Array.Empty<InboundScannedRow>();

        // 映射：InstockId <- id（截图注释“入库单明细主键id”）
        var list = dto.result.Select(x => new InboundScannedRow(
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
    public async Task<SimpleOk> InStockByBarcodeAsync(string instockId, string barcode, CancellationToken ct = default)
    {
        // 注意：接口要的是 id 不是 instockId
        var body = JsonSerializer.Serialize(new { barcode, id = instockId });

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


    public async Task<SimpleOk> ConfirmInstockAsync(string instockId, CancellationToken ct = default)
    {
        var bodyJson = JsonSerializer.Serialize(new { id = instockId });
        using var req = new HttpRequestMessage(HttpMethod.Post, _confirmInstockEndpoint)
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
    /// <param name="instockId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> JudgeInstockDetailScanAllAsync(string instockId, CancellationToken ct = default)
    {
        var url = $"{_judgeScanAllEndpoint}?id={Uri.EscapeDataString(instockId)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<JudgeScanAllResp>(json, opt);

        // 按文档：看 result（true/false）；若接口异常或无 result，则返回 false 让前端提示/二次确认
        return dto?.result == true;
    }


    // 修改点①：把路径统一为 Instock（与你提供的接口一致）
    public async Task<List<LocationNodeDto>> GetLocationTreeAsync(CancellationToken ct = default)
    {
        var res = await _http.GetAsync("/normalService/pda/wmsMaterialInstock/getInStockLocation", ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);

        var root = JsonSerializer.Deserialize<InStockLocationResp>(json, _opt);

        // 返回根节点的 children
        return root?.result?.children ?? new List<LocationNodeDto>();
    }

    // 修改点②：按层查询库位（保持不变，如需带 searchCount 等可再加参数）
    public async Task<List<BinInfo>> GetBinsByLayerAsync(
    string warehouseCode, string layer, int pageNo = 1, int pageSize = 50, int status = 1, CancellationToken ct = default)
    {
        var url = $"{_pageLocationQuery}?warehouseCode={Uri.EscapeDataString(warehouseCode)}" +
                  $"&layer={Uri.EscapeDataString(layer)}&pageNo={pageNo}&pageSize={pageSize}&status={status}";

        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<PageLocationResp>(json, _json);

        if (dto?.success != true || dto.result?.records is null || dto.result.records.Count == 0)
            return new();

        static bool ToInStock(string? s) =>
            string.Equals(s, "instock", StringComparison.OrdinalIgnoreCase);

        var list = new List<BinInfo>(dto.result.records.Count);
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
        var url = "/normalService/pda/wmsMaterialInstock/updateLocation";
        var payload = new
        {
            detailId,
            id,
            instockWarehouse,
            instockWarehouseCode,
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
        var url = "/normalService/pda/wmsMaterialInstock/updateQuantity";
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




