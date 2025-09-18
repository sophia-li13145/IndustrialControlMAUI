using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json.Nodes;

namespace IndustrialControlMAUI.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IConfigLoader _cfg;

    [ObservableProperty] private int schemaVersion;
    [ObservableProperty] private string ipAddress = "";
    [ObservableProperty] private int port;
    [ObservableProperty] private string baseUrl = "";

    public AdminViewModel(IConfigLoader cfg)
    {
        _cfg = cfg;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        JsonNode node = _cfg.Load();

        SchemaVersion = node["schemaVersion"]?.GetValue<int?>() ?? 0;

        var server = node["server"] as JsonObject ?? new JsonObject();
        IpAddress = server["ipAddress"]?.GetValue<string>() ?? "";
        Port = server["port"]?.GetValue<int?>() ?? 80;

        BaseUrl = $"http://{IpAddress}:{Port}";
    }

    [RelayCommand]
    public Task SaveAsync()
    {
        var node = _cfg.Load();

        var server = node["server"] as JsonObject ?? new JsonObject();
        server["ipAddress"] = IpAddress.Trim();
        server["port"] = Port;
        node["server"] = server;

        _cfg.Save(node);

        BaseUrl = $"http://{IpAddress}:{Port}";
        return Shell.Current.DisplayAlert("已保存", "配置已保存，可立即生效。", "确定");
    }

    [RelayCommand]
    public async Task ResetToPackageAsync()
    {
        await _cfg.EnsureLatestAsync(); // 包内 schemaVersion 高则触发合并覆盖
        LoadFromConfig();
        await Shell.Current.DisplayAlert("已重载", "已从包内默认配置重载/合并。", "确定");
    }
}
