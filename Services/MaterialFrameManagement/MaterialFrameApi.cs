using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;
using System.Text.Json;

namespace IndustrialControlMAUI.Services;

public class MaterialFrameApi : IMaterialFrameApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _materialFrameInfoPageEndpoint;
    private readonly string _materialFrameOperationPageEndpoint;
    private readonly string _pageBasMaterialsEndpoint;

    public MaterialFrameApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;

        var baseUrl = configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
        _materialFrameInfoPageEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.pageMaterialFrameInfo", "/pda/dev/frameUseRecord/pageMaterialFrameInfo"),
            servicePath);
        _materialFrameOperationPageEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.page", "/pda/dev/frameUseRecord/page"),
            servicePath);
        _pageBasMaterialsEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.pageBasMaterials", "/pda/dev/frameUseRecord/pageBasMaterials"),
            servicePath);
    }

    public async Task<PageResp<MaterialFrameRecord>?> PageMaterialFrameInfoAsync(
        int pageNo = 1,
        int pageSize = 10,
        string? operationType = null,
        string? frameNo = null,
        CancellationToken ct = default)
    {
        if (pageNo <= 0) pageNo = 1;
        if (pageSize <= 0) pageSize = 10;

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("pageNo", pageNo.ToString()),
            new("pageSize", pageSize.ToString())
        };
        if (!string.IsNullOrWhiteSpace(operationType))
            pairs.Add(new("operationType", operationType.Trim()));

        if (!string.IsNullOrWhiteSpace(frameNo))
            pairs.Add(new("frameNo", frameNo.Trim()));

        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var endpoint = string.IsNullOrWhiteSpace(operationType)
            ? _materialFrameInfoPageEndpoint
            : _materialFrameOperationPageEndpoint;
        var url = endpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new PageResp<MaterialFrameRecord> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<PageResp<MaterialFrameRecord>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new PageResp<MaterialFrameRecord>();
    }

    public async Task<PageResp<BasMaterialRecord>?> PageBasMaterialsAsync(
        int pageNo = 1,
        int pageSize = 20,
        string? materialName = null,
        string? materialCode = null,
        CancellationToken ct = default)
    {
        if (pageNo <= 0) pageNo = 1;
        if (pageSize <= 0) pageSize = 20;

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("pageNo", pageNo.ToString()),
            new("pageSize", pageSize.ToString())
        };
        if (!string.IsNullOrWhiteSpace(materialName))
            pairs.Add(new("materialName", materialName.Trim()));
        if (!string.IsNullOrWhiteSpace(materialCode))
            pairs.Add(new("materialCode", materialCode.Trim()));

        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var url = _pageBasMaterialsEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new PageResp<BasMaterialRecord> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<PageResp<BasMaterialRecord>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new PageResp<BasMaterialRecord>();
    }
}
