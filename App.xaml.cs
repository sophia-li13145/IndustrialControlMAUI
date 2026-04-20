using IndustrialControlMAUI.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Serilog;
using System.IO;

namespace IndustrialControlMAUI;

public partial class App : Application
{
    private readonly IConfigLoader _configLoader;
    private readonly AppShell _shell;
    private readonly IAppVersionService _appVersionService;

    public static IServiceProvider? Services { get; set; }

    public App(IConfigLoader configLoader, AppShell shell, IAppVersionService appVersionService)
    {
        InitializeComponent();

        _configLoader = configLoader;
        _shell = shell;
        _appVersionService = appVersionService;

        // ===== ʼ Serilogƽ̨ȫ·=====
        InitSerilog();

        // ===== ȫ쳣 =====
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // ȸ MainPage
        MainPage = _shell;

        // 첽ʼ¼״̬
        _ = InitAsync();
    }

    /// <summary>
    /// Serilog ʼAppDataDirectory/logs
    /// </summary>
    private static void InitSerilog()
    {
        var baseDir = FileSystem.Current.AppDataDirectory;

        // ˶ףϲᷢԴ
        if (string.IsNullOrWhiteSpace(baseDir) || baseDir == "/")
        {
            baseDir = FileSystem.Current.CacheDirectory;
        }

        var logsDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logsDir);

        var logPath = Path.Combine(logsDir, "app_log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Debug()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                shared: true)
            .CreateLogger();

        Log.Information("Serilog initialized. LogPath = {LogPath}", logPath);
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // ʱȷ
        await _configLoader.EnsureLatestAsync();

        var token = await TokenStorage.LoadAsync();
        var isLoggedIn = !string.IsNullOrWhiteSpace(token);

        Log.Information("App started. IsLoggedIn = {IsLoggedIn}", isLoggedIn);
    }

    private async Task InitAsync()
    {
        var token = await TokenStorage.LoadAsync();
        bool authed = !string.IsNullOrWhiteSpace(token);

        if (authed)
        {
            await _appVersionService.HandleStartupUpdateAsync();
        }

        _shell.ApplyAuth(authed);
    }

    public static void SwitchToLoggedInShell()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var sp = Current?.Handler?.MauiContext?.Services;
            var shell = sp?.GetRequiredService<AppShell>();
            if (shell == null) return;

            Current!.MainPage = shell;
            shell.ApplyAuth(true);
        });
    }

    public static void SwitchToLoggedOutShell()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var sp = Current?.Handler?.MauiContext?.Services;
            var shell = sp?.GetRequiredService<AppShell>();
            if (shell == null) return;

            Current!.MainPage = shell;
            shell.ApplyAuth(false);
        });
    }

    // ===== ȫ쳣 =====
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Error(ex, "δȫ쳣");
        }
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "δ۲쳣");
        e.SetObserved();
    }
}
