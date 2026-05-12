using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.Tools;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IndustrialControlMAUI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IConfigLoader _cfg;
    private readonly IAppVersionService _appVersionService;

    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool showPassword; // false=默认隐藏
    [ObservableProperty] private bool rememberPassword; // 新增：记住密码

    /// <summary>执行 new 逻辑。</summary>
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>执行 LoginViewModel 初始化逻辑。</summary>
    public LoginViewModel(IConfigLoader cfg, IAppVersionService appVersionService)
    {
        _cfg = cfg;
        _appVersionService = appVersionService;

        // 启动时加载保存的账号
        UserName = Preferences.Get("UserName", string.Empty);
        Password = Preferences.Get("Password", string.Empty);
        RememberPassword = Preferences.Get("RememberPassword", false);
    }

    /// <summary>执行 LoginAsync 逻辑。</summary>
    [RelayCommand]
    public async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // 1) 确保有本地配置
            await _cfg.EnsureLatestAsync();

            // 2) 登录验证前：根据用户名里的 @xxx 写入当前服务并保存
            _cfg.SetCurrentServiceByUser(UserName);

            // 3) 用最新配置拼 URL（端口已在 ipAddress 中）
            var baseUrl = _cfg.GetBaseUrl();                // scheme://ip:port + /{servicePath}
            var loginRel = _cfg.GetApiPath("login", "/pda/auth/login");
            var fullUrl = new Uri(baseUrl + loginRel);

            // 3) 表单校验后执行登录
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请输入用户名和密码", "确定");
                return;
            }

            var payload = new { username = UserName, password = Password };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var resp = await ApiClient.Instance.PostAsJsonAsync(fullUrl, payload, cts.Token);
            var raw = await ResponseGuard.ReadAsStringSafeAsync(resp.Content, cts.Token);

            if (!resp.IsSuccessStatusCode)
            {
                await Application.Current.MainPage.DisplayAlert("登录失败", GetLoginHttpErrorMessage(resp.StatusCode), "确定");
                return;
            }

            var result = JsonSerializer.Deserialize<ApiResponse<LoginResult>>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = (result?.success == true) || (result?.code is 0 or 200);
            var token = result?.result?.token;

            if (!ok || string.IsNullOrWhiteSpace(token))
            {
                await Application.Current.MainPage.DisplayAlert("登录失败", GetLoginResultErrorMessage(result?.message), "确定");
                return;
            }

            var canLogin = await _appVersionService.ShowCompareResultMessageIfNeededAsync();
            if (!canLogin)
                return;

            await TokenStorage.SaveAsync(token!);
            Preferences.Set("UserName", UserName ?? "");
            if (RememberPassword)
            {
                Preferences.Set("Password", Password ?? "");
                Preferences.Set("RememberPassword", true);
            }
            else
            {
                Preferences.Remove("Password");
                Preferences.Set("RememberPassword", false);
            }

            App.SwitchToLoggedInShell();
        }
        catch (OperationCanceledException)
        {
            await Application.Current.MainPage.DisplayAlert("超时", "登录请求超时，请检查网络", "确定");
        }
        catch (HttpRequestException)
        {
            await Application.Current.MainPage.DisplayAlert("登录失败", "网络连接异常，请检查网络后重试", "确定");
        }
        catch (JsonException)
        {
            await Application.Current.MainPage.DisplayAlert("登录失败", "登录服务返回异常，请稍后再试", "确定");
        }
        catch (Exception)
        {
            await Application.Current.MainPage.DisplayAlert("登录失败", "登录失败，请稍后再试", "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetLoginHttpErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadGateway => "请输入正确的用户名和密码",
        HttpStatusCode.BadRequest => "登录请求有误，请检查输入后重试",
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "账号验证未通过，请确认后重试",
        HttpStatusCode.NotFound => "登录服务暂不可用，请稍后再试",
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => "登录请求超时，请检查网络后重试",
        HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable => "登录服务异常，请稍后再试",
        _ => "登录失败，请稍后再试"
    };

    private static string GetLoginResultErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "登录失败，请确认账号信息后重试";

        if (message.Contains("密码") || message.Contains("账号") || message.Contains("用户") ||
            message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("user", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("credential", StringComparison.OrdinalIgnoreCase))
        {
            return "账号验证未通过，请确认后重试";
        }

        return "登录失败，请稍后再试";
    }

    /// <summary>执行 TogglePassword 逻辑。</summary>
    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;

    /// <summary>执行 ClearHistory 逻辑。</summary>
    [RelayCommand]
    private void ClearHistory()
    {
        UserName = string.Empty;
        Password = string.Empty;
        RememberPassword = false;

        Preferences.Remove("UserName");
        Preferences.Remove("Password");
        Preferences.Set("RememberPassword", false);
    }


    private sealed class ApiResponse<T>
    {
        public int code { get; set; }
        public bool success { get; set; }
        public string? message { get; set; }
        public T? result { get; set; }
    }

    private sealed class LoginResult
    {
        public string? token { get; set; }
        public object? userInfo { get; set; }
    }
}
