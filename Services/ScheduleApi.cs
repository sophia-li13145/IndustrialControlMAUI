using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndustrialControlMAUI.Services;

public class ScheduleApi : IScheduleApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _detailEndpoint;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ScheduleApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;

        if (_http.Timeout == Timeout.InfiniteTimeSpan)
            _http.Timeout = TimeSpan.FromSeconds(15);

        var baseUrl = configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _detailEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("schedule.planDetail", "/pda/pmsSchedulePlan/querySchedulePlanDetail"),
            servicePath);
    }

    public async Task<ApiResp<SchedulePlanDetailResult>> QuerySchedulePlanDetailAsync(DateTime selectedDate, CancellationToken ct = default)
    {
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _detailEndpoint);
        var payload = new { selectedDate = selectedDate.Date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz") };

        using var res = await _http.PostAsJsonAsync(new Uri(full, UriKind.Absolute), payload, JsonOptions, ct);
        var body = await ResponseGuard.ReadAsStringAndCheckAsync(res, _auth, ct);

        if (!res.IsSuccessStatusCode)
            return new ApiResp<SchedulePlanDetailResult> { success = false, code = (int)res.StatusCode, message = $"HTTP {(int)res.StatusCode}" };

        return JsonSerializer.Deserialize<ApiResp<SchedulePlanDetailResult>>(body, JsonOptions)
               ?? new ApiResp<SchedulePlanDetailResult> { success = false, message = "empty response" };
    }
}
