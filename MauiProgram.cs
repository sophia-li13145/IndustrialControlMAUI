using CommunityToolkit.Maui;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using Microsoft.Extensions.Logging;

namespace IndustrialControlMAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#if DEBUG
            builder.Logging.AddDebug(); // ✅ 放在 Build 之前
#endif
            builder.Services.AddSingleton<AppShell>();
            // 注册 ConfigLoader
            builder.Services.AddSingleton<IConfigLoader, ConfigLoader>();
            builder.Services.AddSingleton<LogService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();


            // 扫码服务
            builder.Services.AddSingleton<ScanService>();
            builder.Services.AddSingleton<IBinPickerService, BinPickerService>();


            // ===== 注册 ViewModels =====
            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            builder.Services.AddTransient<ViewModels.HomeViewModel>();
            builder.Services.AddTransient<ViewModels.AdminViewModel>();
            builder.Services.AddTransient<ViewModels.LogsViewModel>();
            builder.Services.AddTransient<BinPickerViewModel>();
            builder.Services.AddTransient<ViewModels.InboundMaterialSearchViewModel>();
            builder.Services.AddTransient<ViewModels.InboundMaterialViewModel>();
            builder.Services.AddTransient<ViewModels.InboundProductionViewModel>();
            builder.Services.AddTransient<ViewModels.InboundProductionSearchViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundMaterialViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundMaterialSearchViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundFinishedViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundFinishedSearchViewModel>();
            builder.Services.AddTransient<ViewModels.InboundMoldViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundMoldSearchViewModel>();
            builder.Services.AddTransient<ViewModels.OutboundMoldViewModel>();
            builder.Services.AddTransient<ViewModels.WorkOrderSearchViewModel>();
            builder.Services.AddTransient<ViewModels.MoldOutboundExecuteViewModel>();

            // ===== 注册 Pages（DI 创建）=====
            builder.Services.AddTransient<Pages.LoginPage>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.AdminPage>();
            builder.Services.AddTransient<Pages.LogsPage>();

            // 注册需要路由的页面
            builder.Services.AddTransient<Pages.BinPickerPage>();
            builder.Services.AddTransient<Pages.BinListPage>(); // 供 VM 弹出列表
            builder.Services.AddTransient<Pages.InboundMaterialSearchPage>();
            builder.Services.AddTransient<Pages.InboundMaterialPage>();
            builder.Services.AddTransient<Pages.InboundProductionPage>();
            builder.Services.AddTransient<Pages.InboundProductionSearchPage>();
            builder.Services.AddTransient<Pages.OutboundMaterialPage>();
            builder.Services.AddTransient<Pages.OutboundMaterialSearchPage>();
            builder.Services.AddTransient<Pages.OutboundFinishedPage>();
            builder.Services.AddTransient<Pages.OutboundFinishedSearchPage>();
            builder.Services.AddTransient<Pages.InboundMoldPage>();
            builder.Services.AddTransient<Pages.OutboundMoldSearchPage>();
            builder.Services.AddTransient<Pages.OutboundMoldPage>();
            builder.Services.AddTransient<Pages.WorkOrderSearchPage>();
            builder.Services.AddTransient<Pages.MoldOutboundExecutePage>();
            // 先注册配置加载器
            builder.Services.AddSingleton<IConfigLoader, ConfigLoader>();

            // 授权处理器
            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddHttpClient<IWorkOrderApi, WorkOrderApi>(ConfigureBaseAddress)
                .AddHttpMessageHandler<AuthHeaderHandler>();

            builder.Services.AddHttpClient<IInboundMaterialService, InboundMaterialService>(ConfigureBaseAddress)
                .AddHttpMessageHandler<AuthHeaderHandler>();
            builder.Services.AddHttpClient<IOutboundMaterialService, OutboundMaterialService>(ConfigureBaseAddress)
               .AddHttpMessageHandler<AuthHeaderHandler>();
            builder.Services.AddHttpClient<IMoldApi, MoldApi>(ConfigureBaseAddress)
             .AddHttpMessageHandler<AuthHeaderHandler>();
            var app = builder.Build();
            App.Services = app.Services;
            return app;
        }

        private static void ConfigureBaseAddress(IServiceProvider sp, HttpClient http)
        {
            var cfg = sp.GetRequiredService<IConfigLoader>().Load();
            var scheme = (string?)cfg?["server"]?["scheme"] ?? "http";
            var ip = (string?)cfg?["server"]?["ipAddress"] ?? "127.0.0.1";
            var port = (int?)cfg?["server"]?["port"] ?? 80;
            var baseUrl = port is > 0 and < 65536 ? $"{scheme}://{ip}:{port}" : $"{scheme}://{ip}";
            http.BaseAddress = new Uri(baseUrl);
        }
    }
}
