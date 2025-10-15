using Microsoft.Maui.Controls;
using Serilog;

namespace IndustrialControlMAUI;

public partial class App : Application
{
    private readonly IConfigLoader _configLoader;
    public static IServiceProvider? Services { get; set; }
    private readonly AppShell _shell;
    public App(IConfigLoader configLoader, AppShell shell)
    {
        InitializeComponent();
        _configLoader = configLoader;
        _shell = shell;                 // ★ 由 DI 提供
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()  // 设置最低日志级别为 Information
        .WriteTo.File("app_log.txt", rollingInterval: RollingInterval.Day)  // 输出到文件
        .CreateLogger();
       
        // 1) 立刻给 MainPage 一个默认值，避免 null
        MainPage = _shell;
        // 启动根据是否已登录选择壳
        _ = InitAsync();

    }

    protected override async void OnStart()
    {
        base.OnStart();

        // 1) 启动时确保配置已覆盖（这里才能 await）
        await _configLoader.EnsureLatestAsync();

        // 2) 判断是否已登录
        var token = await TokenStorage.LoadAsync();
        var isLoggedIn = !string.IsNullOrWhiteSpace(token);
    }

    private async Task InitAsync()
    {
        var token = await TokenStorage.LoadAsync();
        bool authed = !string.IsNullOrWhiteSpace(token);

        // 给 shell 一个“应用登录状态”的方法，而不是 new
        _shell.ApplyAuth(authed);
    }

    public static void SwitchToLoggedInShell()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var sp = Current?.Handler?.MauiContext?.Services;
            var shell = sp?.GetRequiredService<AppShell>();
            if (shell == null) return;

            Current!.MainPage = shell;   // 先挂壳
            shell.ApplyAuth(true);       // 再切到 //Home（已登录）
        });
    }
    public static void SwitchToLoggedOutShell()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var sp = Current?.Handler?.MauiContext?.Services;
            var shell = sp?.GetRequiredService<AppShell>();
            if (shell == null) return;

            Current!.MainPage = shell;   // 先挂上新壳
            shell.ApplyAuth(false);      // 再跳到 //Login
        });
    }

    // 处理未处理的异常
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (ex != null)
        {
            Log.Error(ex, "捕获到未处理的全局异常");
        }
    }

    // 处理未观察的任务异常
    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "捕获到未观察的任务异常");
        e.SetObserved();  // 防止应用崩溃
    }
}
