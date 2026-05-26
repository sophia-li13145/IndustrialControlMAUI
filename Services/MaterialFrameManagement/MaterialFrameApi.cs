using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndustrialControlMAUI.Services;

public class MaterialFrameApi : IMaterialFrameApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _materialFrameInfoPageEndpoint;
    private readonly string _materialFrameOperationPageEndpoint;
    private readonly string _pageBasMaterialsEndpoint;
    private readonly string _getFrameStatusListEndpoint;
    private readonly string _getFrameStatusListByFrameNoEndpoint;
    private readonly string _getStatusDictListEndpoint;
    private readonly string _getMaterialFrameListEndpoint;
    private readonly string _addLoadingRecordEndpoint;
    private readonly string _getLoadingRecordDetailEndpoint;
    private readonly string _getUnloadRecordDetailEndpoint;
    private readonly string _getPouringRecordDetailEndpoint;
    private readonly string _getFrameMergingDetailEndpoint;
    private readonly string _getFrameReturnDetailEndpoint;
    private readonly string _getFrameStatusListForUnloadEndpoint;
    private readonly string _addUnloadingRecordEndpoint;
    private readonly string _addFrameMergingRecordEndpoint;
    private readonly string _addPouringRecordEndpoint;
    private readonly string _getMaterialFrameStatusListEndpoint;
    private readonly string _addFrameReturnRecordEndpoint;

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
        _getFrameStatusListEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getFrameStatusList", "/pda/dev/frameUseRecord/getFrameStatusList"),
            servicePath);
        _getFrameStatusListByFrameNoEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getFrameStatusListByFrameNo", "/pda/dev/frameUseRecord/getFrameStatusListByFrameNo"),
            servicePath);
        _getMaterialFrameListEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getMaterialFrameList", "/pda/dev/frameUseRecord/getMaterialFrameList"),
            servicePath);
        _getStatusDictListEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getStatusDictList", "/pda/dev/frameUseRecord/getStatusDictList"),
            servicePath);
        _addLoadingRecordEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.addLoadingRecord", "/pda/dev/frameUseRecord/addLoadingRecord"),
            servicePath);
        _getLoadingRecordDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getLoadingRecordDetail", "/pda/dev/frameUseRecord/getLoadingRecordDetail"),
            servicePath);
        _getUnloadRecordDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getUnloadRecordDetail", "/pda/dev/frameUseRecord/getUnloadDetail"),
            servicePath);
        _getPouringRecordDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getPouringRecordDetail", "/pda/dev/frameUseRecord/getPouringDetail"),
            servicePath);
        _getFrameMergingDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getFrameMergingDetail", "/pda/dev/frameUseRecord/getFrameMergingDetail"),
            servicePath);
        _getFrameStatusListForUnloadEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getFrameStatusListForUnload", "/pda/dev/frameUseRecord/getFrameStatusListForUnload"),
            servicePath);
        _addUnloadingRecordEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.addUnloadingRecord", "/pda/dev/frameUseRecord/addUnloadingRecord"),
            servicePath);
        _addFrameMergingRecordEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.addFrameMergingRecord", "/pda/dev/frameUseRecord/addFrameMergingRecord"),
            servicePath);
        _addPouringRecordEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.addPouringRecord", "/pda/dev/frameUseRecord/addPouringRecord"),
            servicePath);
        _getFrameReturnDetailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getFrameReturnDetail", "/pda/dev/frameUseRecord/getFrameReturnDetail"),
            servicePath);
        _getMaterialFrameStatusListEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.getMaterialFrameStatusList", "/pda/dev/frameUseRecord/getMaterialFrameStatusList"),
            servicePath);
        _addFrameReturnRecordEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("materialFrame.addFrameReturnRecord", "/pda/dev/frameUseRecord/addFrameReturnRecord"),
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

    public async Task<PageResp<MaterialFrameRecord>?> PageMaterialFrameOperationAsync(
        int pageNo = 1,
        int pageSize = 10,
        string operationType = "framing",
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

        var url = _materialFrameOperationPageEndpoint + "?" + query;
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

    public async Task<PageResp<FrameUseRecordOperation>?> PageFrameUseRecordPageAsync(
        int pageNo = 1,
        int pageSize = 10,
        string operationType = "framing",
        string? materialName = null,
        string? recordNo = null,
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
        if (!string.IsNullOrWhiteSpace(materialName))
            pairs.Add(new("materialName", materialName.Trim()));
        if (!string.IsNullOrWhiteSpace(recordNo))
            pairs.Add(new("recordNo", recordNo.Trim()));

        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var url = _materialFrameOperationPageEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new PageResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<PageResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new PageResp<FrameUseRecordOperation>();
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

    public async Task<ListResp<FrameStatusItem>?> GetFrameStatusListAsync(
        string materialCode,
        string materialName,
        CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("materialCode", materialCode.Trim()),
            new("materialName", materialName.Trim())
        };

        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var url = _getFrameStatusListEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ListResp<FrameStatusItem> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ListResp<FrameStatusItem>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ListResp<FrameStatusItem>();
    }
    public async Task<ListResp<FrameStatusItem>?> GetFrameStatusListByFrameNoAsync(
        string frameNo,
        string materialCode,
        CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("frameNo", frameNo.Trim()),
            new("materialCode", materialCode.Trim())
        };

        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var url = _getFrameStatusListByFrameNoEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ListResp<FrameStatusItem> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ListResp<FrameStatusItem>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ListResp<FrameStatusItem>();
    }

    public async Task<ObjResp<FrameUseRecordOperation>?> GetLoadingRecordDetailAsync(
        string id,
        CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("id", id.Trim())
        };
        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var url = _getLoadingRecordDetailEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ObjResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ObjResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ObjResp<FrameUseRecordOperation>();
    }


    public async Task<ObjResp<FrameUseRecordOperation>?> GetUnloadRecordDetailAsync(
        string recordId,
        CancellationToken ct = default)
    {
        var req = new
        {
            recordId = recordId.Trim()
        };

        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _getUnloadRecordDetailEndpoint);

        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(req)
        };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ObjResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ObjResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ObjResp<FrameUseRecordOperation>();
    }


    public async Task<ObjResp<FrameUseRecordOperation>?> GetPouringRecordDetailAsync(
        string recordId,
        CancellationToken ct = default)
    {
        var req = new
        {
            recordId = recordId.Trim()
        };

        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _getPouringRecordDetailEndpoint);

        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(req)
        };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ObjResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ObjResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ObjResp<FrameUseRecordOperation>();
    }


    public async Task<ObjResp<FrameUseRecordOperation>?> GetFrameMergingDetailAsync(
        string useRecordId,
        CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("useRecordId", useRecordId.Trim())
        };
        var query = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var url = _getFrameMergingDetailEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ObjResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ObjResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ObjResp<FrameUseRecordOperation>();
    }


    public async Task<ObjResp<FrameUseRecordOperation>?> GetFrameReturnDetailAsync(
        string useRecordId,
        CancellationToken ct = default)
    {
        var query = $"useRecordId={Uri.EscapeDataString(useRecordId.Trim())}";
        var url = _getFrameReturnDetailEndpoint + "?" + query;
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ObjResp<FrameUseRecordOperation> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ObjResp<FrameUseRecordOperation>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ObjResp<FrameUseRecordOperation>();
    }

    public async Task<ListResp<FrameStatusItem>?> GetMaterialFrameListAsync(string? frameNo = null, CancellationToken ct = default)
    {
        var url = _getMaterialFrameListEndpoint;
        if (!string.IsNullOrWhiteSpace(frameNo))
            url += "?frameNo=" + Uri.EscapeDataString(frameNo.Trim());
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);

        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ListResp<FrameStatusItem> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ListResp<FrameStatusItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ListResp<FrameStatusItem>();
    }

    public async Task<ListResp<FrameStatusItem>?> GetFrameStatusListForUnloadAsync(List<string> materialCodes, List<string> materialNames, string? frameNo = null, CancellationToken ct = default)
    {
        var reqBody = new
        {
            materialCodes = materialCodes ?? new List<string>(),
            materialNames = materialNames ?? new List<string>(),
            frameNo = string.IsNullOrWhiteSpace(frameNo) ? null : frameNo.Trim()
        };

        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _getFrameStatusListForUnloadEndpoint);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(reqBody)
        };
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ListResp<FrameStatusItem> { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ListResp<FrameStatusItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ListResp<FrameStatusItem>();
    }

    public async Task<BoolResp?> AddUnloadingRecordAsync(AddUnloadingRecordReq req, CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addUnloadingRecordEndpoint);

        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(req)
        };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new BoolResp { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<BoolResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new BoolResp();
    }

    public async Task<List<DictField>?> GetStatusDictListAsync(CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _getStatusDictListEndpoint);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new List<DictField>();

        var obj = JsonSerializer.Deserialize<ListResp<DictField>>(json,
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return obj?.result ?? new List<DictField>();
    }

    public async Task<BoolResp?> AddFrameMergingRecordAsync(AddFrameMergingRecordReq req, CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addFrameMergingRecordEndpoint);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute)) { Content = JsonContent.Create(req) };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
        if (!res.IsSuccessStatusCode) return new BoolResp { success = false, message = $"HTTP {(int)res.StatusCode}" };
        return JsonSerializer.Deserialize<BoolResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new BoolResp();
    }

    public async Task<BoolResp?> AddPouringRecordAsync(AddPouringRecordReq req, CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addPouringRecordEndpoint);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute)) { Content = JsonContent.Create(req) };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
        if (!res.IsSuccessStatusCode) return new BoolResp { success = false, message = $"HTTP {(int)res.StatusCode}" };
        return JsonSerializer.Deserialize<BoolResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new BoolResp();
    }

    public async Task<BoolResp?> AddLoadingRecordAsync(
        AddLoadingRecordReq req,
        CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addLoadingRecordEndpoint);

        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(req)
        };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new BoolResp { success = false, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<BoolResp>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new BoolResp();
    }


    public async Task<PageResp<FrameStatusItem>?> GetFrameReturnSelectableListAsync(int pageNo = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (pageNo <= 0) pageNo = 1;
        if (pageSize <= 0) pageSize = 10;
        var url = _getMaterialFrameStatusListEndpoint + $"?pageNo={pageNo}&pageSize={pageSize}";
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, url);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(full, UriKind.Absolute));
        using var res = await _http.SendAsync(req, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
        if (!res.IsSuccessStatusCode)
            return new PageResp<FrameStatusItem> { success = false, message = $"HTTP {(int)res.StatusCode}" };
        return JsonSerializer.Deserialize<PageResp<FrameStatusItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new PageResp<FrameStatusItem>();
    }

    public async Task<BoolResp?> AddFrameReturnRecordAsync(AddFrameReturnRecordReq req, CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addFrameReturnRecordEndpoint);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(full, UriKind.Absolute))
        {
            Content = JsonContent.Create(req)
        };
        using var res = await _http.SendAsync(reqMsg, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);
        if (!res.IsSuccessStatusCode)
            return new BoolResp { success = false, message = $"HTTP {(int)res.StatusCode}" };
        return JsonSerializer.Deserialize<BoolResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new BoolResp();
    }

}
