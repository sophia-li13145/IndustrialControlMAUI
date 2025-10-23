using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public interface IConfigLoader
{
    Task EnsureLatestAsync();
    JsonNode Load();
    void Save(JsonNode node);

    // === 新增 ===
    /// <summary>根据用户名中的 @service 写入 services.current，并在缺失时给该服务名补默认路径</summary>
    void SetCurrentServiceByUser(string? userName, string defaultServiceName = "normalService", string defaultServicePath = "/normalService");

    /// <summary>获取基地址：scheme://ipAddress + services[current]</summary>
    string GetBaseUrl();

    /// <summary>获取 API 相对路径（点路径），如 "login" 或 "workOrder.page"</summary>
    string GetApiPath(string dottedPath, string fallback = "/pda/auth/login");
}

public class ConfigLoader : IConfigLoader
{
    public Task EnsureLatestAsync() => ConfigLoaderStatic.EnsureConfigIsLatestAsync();
    public JsonNode Load() => ConfigLoaderStatic.Load();
    public void Save(JsonNode node) => ConfigLoaderStatic.Save(node);

    // === 新增 ===
    public void SetCurrentServiceByUser(string? userName, string defaultServiceName = "normalService", string defaultServicePath = "/normalService")
        => ConfigLoaderStatic.SetCurrentServiceByUser(userName, defaultServiceName, defaultServicePath);

    public string GetBaseUrl() => ConfigLoaderStatic.GetBaseUrl();

    public string GetApiPath(string dottedPath, string fallback = "/pda/auth/login")
        => ConfigLoaderStatic.GetApiPath(dottedPath, fallback);
}

public static class ConfigLoaderStatic
{
    private const string FileName = "appconfig.json";
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 用包内更高版本覆盖本地（不做任何字段保留/合并）。
    /// 首次运行：直接复制包内文件到 AppData。
    /// </summary>
    public static async Task EnsureConfigIsLatestAsync()
    {
        var appDataPath = Path.Combine(FileSystem.AppDataDirectory, FileName);

        // 读包内
        JsonNode pkgNode;
        using (var s = await FileSystem.OpenAppPackageFileAsync(FileName))
        using (var reader = new StreamReader(s))
        {
            pkgNode = JsonNode.Parse(await reader.ReadToEndAsync())!;
        }

        // 本地不存在 → 直接落地
        if (!File.Exists(appDataPath))
        {
            await File.WriteAllTextAsync(appDataPath, pkgNode.ToJsonString(JsonOpts));
            return;
        }

        // 读本地
        JsonNode localNode;
        using (var fs = new FileStream(appDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            localNode = JsonNode.Parse(fs)!;
        }

        int pkgVer = pkgNode?["schemaVersion"]?.GetValue<int?>() ?? 0;
        int localVer = localNode?["schemaVersion"]?.GetValue<int?>() ?? 0;

        // 包内版本更高 → 直接覆盖（整文件替换）
        if (pkgVer > localVer)
        {
            await File.WriteAllTextAsync(appDataPath, pkgNode.ToJsonString(JsonOpts));
        }
        // 否则保持本地不动
    }

    /// <summary>读取生效配置（AppData）</summary>
    public static JsonNode Load() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, FileName)))!;

    /// <summary>保存（如果你在设置页手动修改本地配置）</summary>
    public static void Save(JsonNode node) =>
        File.WriteAllText(Path.Combine(FileSystem.AppDataDirectory, FileName), node.ToJsonString(JsonOpts));

    // ==================== 新增能力 ====================

    /// <summary>从用户名里提取 @service；不合法或不存在则返回默认</summary>
    private static string ExtractServiceName(string? userName, string @default = "normalService")
    {
        if (string.IsNullOrWhiteSpace(userName)) return @default;
        var i = userName.LastIndexOf('@');
        if (i < 0 || i == userName.Length - 1) return @default;

        var raw = userName[(i + 1)..].Trim().ToLowerInvariant();
        // 允许 a-z 0-9 _ - .
        return Regex.IsMatch(raw, @"^[a-z0-9_\-\.]+$") ? raw : @default;
    }

    /// <summary>
    /// 根据用户名设置 services.current；
    /// 若该服务在 services 下不存在，则补一个默认路径（默认 "/normalService"），并保存。
    /// </summary>
    public static void SetCurrentServiceByUser(string? userName, string defaultServiceName = "normalService", string defaultServicePath = "/normalService")
    {
        var node = Load() ?? new JsonObject();

        var services = node["services"] as JsonObject;
        if (services is null)
        {
            services = new JsonObject();
            node["services"] = services;
        }

        var svcName = ExtractServiceName(userName, defaultServiceName);

        // 如果该服务名没有路径，则补默认路径
        if (services[svcName] is null)
        {
            services[svcName] = defaultServicePath;
        }

        // 记录当前生效服务名
        services["current"] = svcName.Replace("Service","") + "Service";

        // 持久化
        Save(node);
    }

    /// <summary>获取基地址：scheme://ipAddress + services[current]（ipAddress 已可含端口）</summary>
    public static string GetBaseUrl()
    {
        var cfg = Load();
        var scheme = cfg?["server"]?["scheme"]?.GetValue<string>() ?? "http";
        var host = cfg?["server"]?["ipAddress"]?.GetValue<string>() ?? "127.0.0.1";
        var services = cfg?["services"] as JsonObject;

        var current = services?["current"]?.GetValue<string>() ?? "normalService";
        var servicePath = services?["current"]?.GetValue<string>()
                          ?? services?["normalService"]?.GetValue<string>()
                          ?? "/normalService";

        if (!servicePath.StartsWith("/")) servicePath = "/" + servicePath;

        return $"{scheme}://{host}{servicePath}";
    }

    /// <summary>
    /// 从 apiEndpoints 里按“点路径”取相对路径（支持一层或二层），如 "login"、"workOrder.page"
    /// </summary>
    public static string GetApiPath(string dottedPath, string fallback = "/pda/auth/login")
    {
        var cfg = Load();
        var api = cfg?["apiEndpoints"];
        if (api is null) return NormalizePath(fallback);

        string? val = null;
        var parts = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            val = api[parts[0]]?.GetValue<string>();
        }
        else if (parts.Length == 2)
        {
            val = api[parts[0]]?[parts[1]]?.GetValue<string>();
        }

        val ??= fallback;
        return NormalizePath(val);

        static string NormalizePath(string p) => p.StartsWith("/") ? p : "/" + p;
    }
}