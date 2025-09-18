using Microsoft.Maui.Controls;
using Serilog;

namespace IndustrialControlMAUI;

public partial class App : Application
{
    private readonly IConfigLoader _configLoader;
    public static IServiceProvider? Services { get; set; }
    public App(IServiceProvider sp, IConfigLoader configLoader)
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()  // 设置最低日志级别为 Information
        .WriteTo.File("app_log.txt", rollingInterval: RollingInterval.Day)  // 输出到文件
        .CreateLogger();
        _configLoader = configLoader;
        // 1) 立刻给 MainPage 一个默认值，避免 null
        MainPage = new AppShell(authed: false); // 显示：登录｜日志｜管理员
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
        if (!string.IsNullOrWhiteSpace(token))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Current.MainPage = new AppShell(authed: true); // 显示：主页｜日志｜管理员
            });
        }
    }

    public static void SwitchToLoggedInShell() => Current.MainPage = new AppShell(true);
    public static void SwitchToLoggedOutShell() => Current.MainPage = new AppShell(false);

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
