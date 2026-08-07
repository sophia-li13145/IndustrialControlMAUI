using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndustrialControlMAUI.Services;

public class ShiftHandoverApi : IShiftHandoverApi
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _pendingEndpoint;
    private readonly string _pageEndpoint;
    private readonly string _dictEndpoint;
    private readonly string _handoverInfoEndpoint;
    private readonly string _receiverTeamsEndpoint;
    private readonly string _addEndpoint;
    private readonly string _detailEndpoint;
    private readonly string _confirmEndpoint;

    public ShiftHandoverApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;
        var baseUrl = configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _pendingEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.pending", "/pda/shiftHandover/getPendingHandover"), servicePath);
        _pageEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.page", "/pda/shiftHandover/page"), servicePath);
        _dictEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.dictList", "/pda/shiftHandover/getDictList"), servicePath);
        _handoverInfoEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.info", "/pda/shiftHandover/getHandoverInfo"), servicePath);
        _receiverTeamsEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.receiverTeams", "/pda/shiftHandover/getReceiverTeamOptions"), servicePath);
        _addEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.add", "/pda/shiftHandover/add"), servicePath);
        _detailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.detail", "/pda/shiftHandover/detail"), servicePath);
        _confirmEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("shiftHandover.confirm", "/pda/shiftHandover/confirmHandover"), servicePath);
    }

    public Task<ApiResp<PendingShiftHandover>> GetPendingHandoverAsync(CancellationToken ct = default)
        => GetAsync<PendingShiftHandover>(_pendingEndpoint, ct);

    public Task<ApiResp<ShiftHandoverPageResult>> GetRecordsAsync(int pageNo, int pageSize, CancellationToken ct = default)
        => GetAsync<ShiftHandoverPageResult>($"{_pageEndpoint}?pageNo={pageNo}&pageSize={pageSize}&searchCount=true", ct);

    public Task<ApiResp<List<ShiftHandoverDictionary>>> GetDictListAsync(CancellationToken ct = default)
        => GetAsync<List<ShiftHandoverDictionary>>(_dictEndpoint, ct);

    public Task<ApiResp<ShiftHandoverInfo>> GetHandoverInfoAsync(CancellationToken ct = default)
        => GetAsync<ShiftHandoverInfo>(_handoverInfoEndpoint, ct);

    public Task<ApiResp<List<ReceiverTeamOption>>> GetReceiverTeamOptionsAsync(CancellationToken ct = default)
        => GetAsync<List<ReceiverTeamOption>>(_receiverTeamsEndpoint, ct);

    public async Task<ApiResp<bool>> AddAsync(AddShiftHandoverRequest request, CancellationToken ct = default)
    {
        var fullUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _addEndpoint);
        using var response = await _http.PostAsJsonAsync(new Uri(fullUrl, UriKind.Absolute), request, JsonOptions, ct);
        return await ReadResponseAsync<bool>(response, ct);
    }

    public Task<ApiResp<ShiftHandoverDetail>> GetDetailAsync(string id, CancellationToken ct = default)
        => GetAsync<ShiftHandoverDetail>($"{_detailEndpoint}?id={Uri.EscapeDataString(id)}", ct);

    public async Task<ApiResp<bool>> ConfirmHandoverAsync(ConfirmShiftHandoverRequest request, CancellationToken ct = default)
    {
        var fullUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _confirmEndpoint);
        using var response = await _http.PostAsJsonAsync(new Uri(fullUrl, UriKind.Absolute), request, JsonOptions, ct);
        return await ReadResponseAsync<bool>(response, ct);
    }

    private async Task<ApiResp<T>> GetAsync<T>(string endpoint, CancellationToken ct)
    {
        var fullUrl = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, endpoint);
        using var response = await _http.GetAsync(new Uri(fullUrl, UriKind.Absolute), ct);
        return await ReadResponseAsync<T>(response, ct);
    }

    private async Task<ApiResp<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await ResponseGuard.ReadAsStringAndCheckAsync(response, _auth, ct);
        if (!response.IsSuccessStatusCode)
            return new ApiResp<T> { success = false, code = (int)response.StatusCode, message = $"HTTP {(int)response.StatusCode}" };

        return JsonSerializer.Deserialize<ApiResp<T>>(body, JsonOptions)
               ?? new ApiResp<T> { success = false, message = "empty response" };
    }
}
