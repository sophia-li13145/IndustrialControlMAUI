using IndustrialControlMAUI.Models.Permissions;
using IndustrialControlMAUI.Services.Common;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndustrialControlMAUI.Services.Permissions;

public sealed class PdaPermissionApi : IPdaPermissionApi
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public PdaPermissionApi(HttpClient httpClient, IConfigLoader configLoader)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        var servicePath = _httpClient.BaseAddress?.AbsolutePath.TrimEnd('/') ?? "/normalService";
        _endpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("common.getUserMenuPermission", "/pda/common/getUserMenuPermission"),
            servicePath);
    }

    public async Task<IReadOnlyList<PdaMenuPermissionNode>> GetCurrentUserPermissionsAsync(
        string systemType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(systemType))
            throw new ArgumentException("systemType 不能为空。", nameof(systemType));

        var url = $"{ServiceUrlHelper.BuildFullUrl(_httpClient.BaseAddress, _endpoint)}?systemType={Uri.EscapeDataString(systemType)}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PdaPermissionResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("权限接口返回为空。");
        if (!(result.Success || result.Code is 0 or 200))
            throw new InvalidOperationException(result.Message ?? "获取菜单权限失败。");
        return result.Result;
    }
}
