using IndustrialControlMAUI.Models;
using System.Text.Json;

namespace IndustrialControlMAUI.Services;

public class ConfigLoader
{
    public const string FileName = "appconfig.json";
    private readonly string _configPath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public event Action? ConfigChanged;

    public AppConfig Current { get; private set; } = new AppConfig();

    public string BaseUrl => $"http://{Current.Server.IpAddress}";

    public ConfigLoader()
    {
        Task.Run(EnsureConfigIsLatestAsync).Wait();
    }

    public async Task EnsureConfigIsLatestAsync()
    {
        using var pkgStream = await FileSystem.OpenAppPackageFileAsync(FileName).ConfigureAwait(false);
        using var pkgReader = new StreamReader(pkgStream);
        var pkgJson = await pkgReader.ReadToEndAsync().ConfigureAwait(false);
        var pkgCfg = JsonSerializer.Deserialize<AppConfig>(pkgJson, _jsonOptions) ?? new AppConfig();

        if (!File.Exists(_configPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            await File.WriteAllTextAsync(_configPath, pkgJson).ConfigureAwait(false);
            Current = pkgCfg;
            return;
        }

        var currJson = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
        var currCfg = JsonSerializer.Deserialize<AppConfig>(currJson, _jsonOptions) ?? new AppConfig();

        if (currCfg.SchemaVersion < pkgCfg.SchemaVersion)
        {
            pkgCfg.Server = currCfg.Server;
            Current = pkgCfg;
            await SaveAsync(Current, fireChanged: false).ConfigureAwait(false);
        }
        else
        {
            currCfg.ApiEndpoints ??= pkgCfg.ApiEndpoints;
            currCfg.Logging ??= pkgCfg.Logging;
            currCfg.Server ??= currCfg.Server ?? new ServerSettings();
            Current = currCfg;
        }
    }

    public async Task SaveAsync(AppConfig cfg, bool fireChanged = true)
    {
        var json = JsonSerializer.Serialize(cfg, _jsonOptions);
        await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
        Current = cfg;
        if (fireChanged) ConfigChanged?.Invoke();
    }

    public async Task<AppConfig> ReloadAsync()
    {
        var json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
        Current = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
        ConfigChanged?.Invoke();
        return Current;
    }

    public string GetConfigPath() => _configPath;
}
