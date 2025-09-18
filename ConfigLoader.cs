using System.Text.Json;
using System.Text.Json.Nodes;

public interface IConfigLoader
{
    Task EnsureLatestAsync();
    JsonNode Load();
    void Save(JsonNode node);
}

public class ConfigLoader : IConfigLoader
{
    public Task EnsureLatestAsync() => ConfigLoaderStatic.EnsureConfigIsLatestAsync();
    public JsonNode Load() => ConfigLoaderStatic.Load();
    public void Save(JsonNode node) => ConfigLoaderStatic.Save(node);
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
}
