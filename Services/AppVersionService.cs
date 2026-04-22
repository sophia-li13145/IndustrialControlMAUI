using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services.Common;
using IndustrialControlMAUI.Tools;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;

namespace IndustrialControlMAUI.Services;

public interface IAppVersionService
{
    Task HandleStartupUpdateAsync(CancellationToken ct = default);
    Task<bool> ShowCompareResultMessageIfNeededAsync(CancellationToken ct = default);
}

public sealed class AppVersionService : IAppVersionService
{
    private const string FallbackVersionName = "0";

    private readonly HttpClient _http;
    private readonly IConfigLoader _configLoader;
    private readonly IAttachmentApi _attachmentApi;

    private readonly string _checkUpdatePath;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AppVersionService(HttpClient http, IConfigLoader configLoader, IAttachmentApi attachmentApi)
    {
        _http = http;
        _configLoader = configLoader;
        _attachmentApi = attachmentApi;

        if (_http.Timeout == Timeout.InfiniteTimeSpan)
            _http.Timeout = TimeSpan.FromSeconds(15);

        var baseUrl = _configLoader.GetBaseUrl();
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        var servicePath = _http.BaseAddress.AbsolutePath?.TrimEnd('/') ?? "/normalService";
        _checkUpdatePath = ServiceUrlHelper.NormalizeRelative(
            _configLoader.GetApiPath("appVersion.checkUpdate", "/pda/pdaAppVersion/checkUpdate"),
            servicePath);
    }

    public async Task HandleStartupUpdateAsync(CancellationToken ct = default)
    {
        var data = await CheckUpdateAsync(ct);
        if (data is null || !data.needUpdate)
            return;

        var alertMessage = new StringBuilder();

        alertMessage.AppendLine(string.IsNullOrWhiteSpace(data.message)
            ? "检测到新版本，请及时更新。"
            : data.message);

        // 企业可用版本号
        if (!string.IsNullOrWhiteSpace(data.enterpriseVersionName))
        {
            alertMessage.AppendLine();
            alertMessage.AppendLine($"企业可用版本号：{data.enterpriseVersionName}");
        }

        // 更新内容
        if (!string.IsNullOrWhiteSpace(data.updateNote))
        {
            alertMessage.AppendLine();
            alertMessage.AppendLine("更新内容：");
            alertMessage.AppendLine(data.updateNote);
        }

        var confirm = await MainThread.InvokeOnMainThreadAsync(() =>
            Application.Current?.MainPage?.DisplayAlert(
                "版本更新",
                alertMessage.ToString(),
                "确定",
                "取消") ?? Task.FromResult(false));

        if (confirm)
        {
            await DownloadAndOpenAsync(data.fileInfo?.attachmentUrl, ct);
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current?.MainPage?.DisplayAlert(
                    "提示",
                    "不更新可能会存在问题。",
                    "确定") ?? Task.CompletedTask);
        }
    }

    public async Task<bool> ShowCompareResultMessageIfNeededAsync(CancellationToken ct = default)
    {
        var data = await CheckUpdateAsync(ct);
        if (data is null)
            return true;

        if (!string.Equals(data.compareResult, "equal", StringComparison.OrdinalIgnoreCase))
        {
            var isHigher = string.Equals(data.compareResult, "higher", StringComparison.OrdinalIgnoreCase);
            var msg = string.IsNullOrWhiteSpace(data.message)
                ? (isHigher
                    ? "当前版本高于企业可用版本，请切换到企业可用版本后再登录。"
                    : $"当前版本比较结果：{data.compareResult ?? "unknown"}")
                : data.message;

            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current?.MainPage?.DisplayAlert("版本提示", msg, "确定") ?? Task.CompletedTask);

            if (isHigher)
                return false;
        }

        return true;
    }

    private async Task<PdaAppVersionCheckResult?> CheckUpdateAsync(CancellationToken ct)
    {
        await _configLoader.EnsureLatestAsync();

        var url = ServiceUrlHelper.BuildFullUrl(_http.BaseAddress, _checkUpdatePath);
        var payload = new { versionName = GetCurrentVersionName() };

        using var resp = await _http.PostAsJsonAsync(url, payload, ct);
        var raw = await ResponseGuard.ReadAsStringSafeAsync(resp.Content, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var result = JsonSerializer.Deserialize<ApiResp<PdaAppVersionCheckResult>>(raw, _json);
        return result?.result;
    }

    private string GetCurrentVersionName() => GetLocalVersionName();

    private string GetLocalVersionName()
    {
        try
        {
            var cfg = _configLoader.Load();
            return cfg?["schemaVersion"]?.GetValue<string>()?.Trim() is { Length: > 0 } version
                ? version
                : FallbackVersionName;
        }
        catch
        {
            return FallbackVersionName;
        }
    }

    private async Task DownloadAndOpenAsync(string? attachmentUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl))
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current?.MainPage?.DisplayAlert("下载失败", "更新文件地址为空。", "确定") ?? Task.CompletedTask);
            return;
        }

        try
        {

            var resp = await _attachmentApi.GetPreviewUrlAsync(attachmentUrl, 600, ct);
            var downloadUrl = resp.result;
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("下载地址为空:resp.success" + resp.success);

            await Launcher.Default.OpenAsync(new Uri(downloadUrl));
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current?.MainPage?.DisplayAlert("下载失败", ex.Message, "确定") ?? Task.CompletedTask);
        }
    }
}

public sealed class PdaAppVersionCheckResult
{
    public bool needUpdate { get; set; }
    public string? compareResult { get; set; }
    public string? message { get; set; }
    public string? enterpriseVersionName { get; set; }

    public string? updateNote { get; set; }
    public FileInfo? fileInfo { get; set; }


}

public sealed class FileInfo
{
    public string? attachmentUrl { get; set; }


}
