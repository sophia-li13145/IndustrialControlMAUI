using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndustrialControlMAUI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IConfigLoader _cfg;

    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool showPassword; // false=默认隐藏
    [ObservableProperty] private bool rememberPassword; // 新增：记住密码

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public LoginViewModel(IConfigLoader cfg)
    {
        _cfg = cfg;

        // 启动时加载保存的账号
        UserName = Preferences.Get("UserName", string.Empty);
        Password = Preferences.Get("Password", string.Empty);
        RememberPassword = Preferences.Get("RememberPassword", false);
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var cfg = _cfg.Load();
            var scheme = cfg["server"]?["scheme"]?.GetValue<string>() ?? "http";
            var host = cfg["server"]?["ipAddress"]?.GetValue<string>() ?? "127.0.0.1";
            var port = cfg["server"]?["port"]?.GetValue<int?>();

            var path = cfg["apiEndpoints"]?["login"]?.GetValue<string>() ?? "/normalService/pda/auth/login";
            if (!path.StartsWith("/")) path = "/" + path;

            var baseUrl = port is > 0 and < 65536 ? $"{scheme}://{host}:{port}" : $"{scheme}://{host}";
            var fullUrl = new Uri(baseUrl + path);

            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请输入用户名和密码", "确定");
                return;
            }

            var payload = new { username = UserName, password = Password };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var resp = await ApiClient.Instance.PostAsJsonAsync(fullUrl, payload, cts.Token);
            var raw = await resp.Content.ReadAsStringAsync(cts.Token);

            if (!resp.IsSuccessStatusCode)
            {
                await Application.Current.MainPage.DisplayAlert("登录失败", $"HTTP {(int)resp.StatusCode}: {raw}", "确定");
                return;
            }

            var result = JsonSerializer.Deserialize<ApiResponse<LoginResult>>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ok = (result?.success == true) || (result?.code is 0 or 200);
            var token = result?.result?.token;

            if (!ok || string.IsNullOrWhiteSpace(token))
            {
                await Application.Current.MainPage.DisplayAlert("登录失败", result?.message ?? "登录返回无效", "确定");
                return;
            }

            // 保存 token
            await TokenStorage.SaveAsync(token!);

            // 如果勾选了记住密码，就保存账号信息
            if (RememberPassword)
            {
                Preferences.Set("UserName", UserName ?? "");
                Preferences.Set("Password", Password ?? "");
                Preferences.Set("RememberPassword", true);
            }
            else
            {
                Preferences.Remove("UserName");
                Preferences.Remove("Password");
                Preferences.Set("RememberPassword", false);
            }

            // 进入主壳
            App.SwitchToLoggedInShell();
        }
        catch (OperationCanceledException)
        {
            await Application.Current.MainPage.DisplayAlert("超时", "登录请求超时，请检查网络", "确定");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;

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
