using System.Text.Json;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;

namespace IndustrialControlMAUI.Services;

public sealed class TodoTaskApi : ITodoTaskApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly string _pageEndpoint;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TodoTaskApi(HttpClient http, IConfigLoader configLoader, AuthState auth)
    {
        _http = http;
        _auth = auth;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(configLoader.GetBaseUrl(), UriKind.Absolute);

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
        _pageEndpoint = ServiceUrlHelper.NormalizeRelative(
            configLoader.GetApiPath("todoTask.page", "/pda/todoTask/pageTodoTasks"), servicePath);
    }

    public async Task<PageResponeResult<TodoTaskItem>> PageTodoTasksAsync(
        int pageNo, int pageSize, string? taskNo = null, string? taskType = null,
        DateTime? createdTimeStart = null, DateTime? createdTimeEnd = null,
        bool searchCount = true, CancellationToken ct = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("pageNo", Math.Max(1, pageNo).ToString()),
            new("pageSize", Math.Max(1, pageSize).ToString()),
            new("searchCount", searchCount ? "true" : "false")
        };

        AddIfNotEmpty("taskNo", taskNo);
        AddIfNotEmpty("taskType", taskType);
        if (createdTimeStart.HasValue)
            parameters.Add(new("createdTimeStart", createdTimeStart.Value.ToString("yyyy-MM-dd HH:mm:ss")));
        if (createdTimeEnd.HasValue)
            parameters.Add(new("createdTimeEnd", createdTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss")));

        var query = string.Join("&", parameters.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var full = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, $"{_pageEndpoint}?{query}");
        using var response = await _http.GetAsync(full, ct);
        var json = await ResponseGuard.ReadAsStringAndCheckAsync(response, _auth, ct);

        if (!response.IsSuccessStatusCode)
            return new PageResponeResult<TodoTaskItem>
            {
                success = false,
                code = (int)response.StatusCode,
                message = $"HTTP {(int)response.StatusCode}"
            };

        return JsonSerializer.Deserialize<PageResponeResult<TodoTaskItem>>(json, JsonOptions)
               ?? new PageResponeResult<TodoTaskItem> { success = false, message = "响应为空" };

        void AddIfNotEmpty(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parameters.Add(new(name, value.Trim()));
        }
    }
}
