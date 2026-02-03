using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IConfigLoader _cfg;
    private bool _servicePathCustomized;
    private bool _isSyncingServicePath;

    [ObservableProperty] private int schemaVersion;
    [ObservableProperty] private string ipAddress = "";          // 可包含端口
    [ObservableProperty] private string scheme = "http";         // http / https
    [ObservableProperty] private string serviceName = "normalService";  // services.current
    [ObservableProperty] private string servicePath = "/normalService"; // services[serviceName] 的路径
    [ObservableProperty] private string baseUrl = "";            // 预览：scheme://ipAddress + servicePath

    /// <summary>执行 AdminViewModel 初始化逻辑。</summary>
    public AdminViewModel(IConfigLoader cfg)
    {
        _cfg = cfg;
        LoadFromConfig();
    }

    /// <summary>执行 LoadFromConfig 逻辑。</summary>
    private void LoadFromConfig()
    {
        JsonNode node = _cfg.Load();

        SchemaVersion = node?["schemaVersion"]?.GetValue<int?>() ?? 0;

        // server
        var server = node?["server"] as JsonObject ?? new JsonObject();
        Scheme = server["scheme"]?.GetValue<string>()?.Trim().ToLowerInvariant() ?? "http";
        IpAddress = server["ipAddress"]?.GetValue<string>()?.Trim() ?? "";

        // services
        var services = node?["services"] as JsonObject ?? new JsonObject();
        var current = services["current"]?.GetValue<string>()?.Trim() ?? "normalService";
        var path = services[current]?.GetValue<string>()?.Trim()
                       ?? services["normalService"]?.GetValue<string>()?.Trim()
                       ?? "/normalService";

        ServiceName = current;
        _servicePathCustomized = false;
        ServicePath = NormalizePath(path);

        BaseUrl = BuildBaseUrl(Scheme, IpAddress, ServicePath);
    }

    /// <summary>执行 SaveAsync 逻辑。</summary>
    [RelayCommand]
    public Task SaveAsync()
    {
        var node = _cfg.Load();

        // --- server ---
        var server = node["server"] as JsonObject ?? new JsonObject();
        server["scheme"] = (Scheme?.Trim().ToLowerInvariant()) switch { "https" => "https", _ => "http" };
        server["ipAddress"] = (IpAddress ?? "").Trim();   // 已可包含端口
        node["server"] = server;

        // --- services ---
        var services = node["services"] as JsonObject ?? new JsonObject();

        var svcName = string.IsNullOrWhiteSpace(ServiceName) ? "normalService" : ServiceName.Trim();
        var svcPath = NormalizePath(string.IsNullOrWhiteSpace(ServicePath) ? "/normalService" : ServicePath.Trim());

        // 当前服务名
        services["current"] = svcName;

        // 确保该服务名下有路径（允许用户修改路径）
        services[svcName] = svcPath;

        // 若没有默认 normalService，给个兜底，避免其他代码取不到
        services["normalService"] ??= "/normalService";

        node["services"] = services;

        // 持久化
        _cfg.Save(node);

        // 刷新 BaseUrl 预览
        BaseUrl = BuildBaseUrl(Scheme, IpAddress, svcPath);

        return Shell.Current.DisplayAlert("已保存", "配置已保存，可立即生效。", "确定");
    }

    /// <summary>执行 ResetToPackageAsync 逻辑。</summary>
    [RelayCommand]
    public async Task ResetToPackageAsync()
    {
        await _cfg.EnsureLatestAsync(); // 包内 schemaVersion 更高会覆盖
        LoadFromConfig();
        await Shell.Current.DisplayAlert("已重载", "已从包内默认配置重载。", "确定");
    }

    // ========== Helper ==========
    /// <summary>执行 BuildBaseUrl 逻辑。</summary>
    private static string BuildBaseUrl(string scheme, string ip, string path)
    {
        // 去掉用户误填的 http(s):// 前缀，避免重复
        ip = StripScheme(ip?.Trim() ?? "");
        return $"{(scheme?.ToLowerInvariant() == "https" ? "https" : "http")}://{ip}{NormalizePath(path)}";
    }

    /// <summary>执行 NormalizePath 逻辑。</summary>
    private static string NormalizePath(string? p)
    {
        if (string.IsNullOrWhiteSpace(p)) return "/normalService";
        var s = p.Trim();
        if (!s.StartsWith("/")) s = "/" + s;
        return s.TrimEnd('/') is "/" ? "/" : s; // 允许 "/" 但通常应为 "/normalService"
    }

    /// <summary>执行 StripScheme 逻辑。</summary>
    private static string StripScheme(string s)
    {
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return s.Substring("http://".Length);
        if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return s.Substring("https://".Length);
        return s;
    }

    /// <summary>执行 OnSchemeChanged 逻辑。</summary>
    partial void OnSchemeChanged(string value) => RefreshBaseUrl();
    /// <summary>执行 OnIpAddressChanged 逻辑。</summary>
    partial void OnIpAddressChanged(string value) => RefreshBaseUrl();

    /// <summary>执行 OnServiceNameChanged 逻辑。</summary>
    partial void OnServiceNameChanged(string value)
    {
        if (_servicePathCustomized)
        {
            RefreshBaseUrl();
            return;
        }

        _isSyncingServicePath = true;
        ServicePath = NormalizePath(value);
        _isSyncingServicePath = false;
        RefreshBaseUrl();
    }

    /// <summary>执行 OnServicePathChanged 逻辑。</summary>
    partial void OnServicePathChanged(string value)
    {
        if (!_isSyncingServicePath)
            _servicePathCustomized = true;
        RefreshBaseUrl();
    }

    /// <summary>执行 RefreshBaseUrl 逻辑。</summary>
    private void RefreshBaseUrl()
    {
        BaseUrl = BuildBaseUrl(Scheme, IpAddress, ServicePath);
    }
}
