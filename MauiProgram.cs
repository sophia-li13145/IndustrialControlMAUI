using CommunityToolkit.Maui;
using IndustrialControl.ViewModels.Energy;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.Tools;
using IndustrialControlMAUI.ViewModels;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;


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
                .UseBarcodeReader()
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
            builder.Services.AddSingleton<AuthState>();
            builder.Services.AddTransient<TokenExpiredHandler>();

            // 扫码服务
            builder.Services.AddSingleton<ScanService>();


            // ===== 注册 ViewModels =====
            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            builder.Services.AddTransient<ViewModels.HomeViewModel>();
            builder.Services.AddTransient<ViewModels.AdminViewModel>();
            builder.Services.AddTransient<ViewModels.LogsViewModel>();
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
            builder.Services.AddTransient<ViewModels.ProcessTaskSearchViewModel>();
            builder.Services.AddTransient<ViewModels.WarehouseLocationPickerViewModel>();
            builder.Services.AddTransient<ViewModels.WorkProcessTaskDetailViewModel>();
            builder.Services.AddTransient<ViewModels.ProcessQualityDetailViewModel>();
            builder.Services.AddTransient<ViewModels.ProcessQualitySearchViewModel>();
            builder.Services.AddTransient<ViewModels.FinishedQualityDetailViewModel>();
            builder.Services.AddTransient<ViewModels.FinishedQualitySearchViewModel>();
            builder.Services.AddTransient<ViewModels.InspectionDetailViewModel>();
            builder.Services.AddTransient<ViewModels.InspectionSearchViewModel>();
            builder.Services.AddTransient<ViewModels.InspectionRunDetailViewModel>();
            builder.Services.AddTransient<ViewModels.InspectionRunSearchViewModel>();
            builder.Services.AddTransient<ViewModels.MaintenanceDetailViewModel>();
            builder.Services.AddTransient<ViewModels.MaintenanceSearchViewModel>();
            builder.Services.AddTransient<ViewModels.MaintenanceRunDetailViewModel>();
            builder.Services.AddTransient<ViewModels.MaintenanceRunSearchViewModel>();
            builder.Services.AddTransient<ViewModels.RepairDetailViewModel>();
            builder.Services.AddTransient<ViewModels.RepairSearchViewModel>();
            builder.Services.AddTransient<ViewModels.RepairRunDetailViewModel>();
            builder.Services.AddTransient<ViewModels.RepairRunSearchViewModel>();
            builder.Services.AddTransient<MeterSelectViewModel>();
            builder.Services.AddTransient<ManualReadingViewModel>();

            // ===== 注册 Pages（DI 创建）=====
            builder.Services.AddTransient<Pages.LoginPage>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.AdminPage>();
            builder.Services.AddTransient<Pages.LogsPage>();

            // 注册需要路由的页面
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
            builder.Services.AddTransient<Pages.ProcessTaskSearchPage>();
            builder.Services.AddTransient<Pages.WorkProcessTaskDetailPage>();
            builder.Services.AddTransient<Pages.ProcessQualitySearchPage>();
            builder.Services.AddTransient<Pages.ProcessQualityDetailPage>();
            builder.Services.AddTransient<Pages.FinishedQualitySearchPage>();
            builder.Services.AddTransient<Pages.FinishedQualityDetailPage>();
            builder.Services.AddTransient<Pages.InspectionSearchPage>();
            builder.Services.AddTransient<Pages.InspectionDetailPage>();
            builder.Services.AddTransient<Pages.InspectionRunSearchPage>();
            builder.Services.AddTransient<Pages.InspectionRunDetailPage>();
            builder.Services.AddTransient<Pages.MaintenanceSearchPage>();
            builder.Services.AddTransient<Pages.MaintenanceDetailPage>();
            builder.Services.AddTransient<Pages.MaintenanceRunSearchPage>();
            builder.Services.AddTransient<Pages.MaintenanceRunDetailPage>();
            builder.Services.AddTransient<Pages.RepairSearchPage>();
            builder.Services.AddTransient<Pages.RepairDetailPage>();
            builder.Services.AddTransient<Pages.RepairRunSearchPage>();
            builder.Services.AddTransient<Pages.RepairRunDetailPage>();
            builder.Services.AddTransient<MeterSelectPopup>();
            builder.Services.AddTransient<ManualReadingPage>();

            builder.Services.AddTransient<WarehouseLocationPickerPage>();
            builder.Services.AddTransient<QrScanPage>();
            // 先注册配置加载器
            builder.Services.AddSingleton<IConfigLoader, ConfigLoader>();

            // 授权处理器
            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddHttpClient<IWorkOrderApi, WorkOrderApi>(ConfigureBaseAddress)
                .AddHttpMessageHandler<AuthHeaderHandler>()
                .AddHttpMessageHandler<TokenExpiredHandler>();

            builder.Services.AddHttpClient<IInboundMaterialService, InboundMaterialService>(ConfigureBaseAddress)
                .AddHttpMessageHandler<AuthHeaderHandler>()
                .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IOutboundMaterialService, OutboundMaterialService>(ConfigureBaseAddress)
               .AddHttpMessageHandler<AuthHeaderHandler>()
               .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IMoldApi, MoldApi>(ConfigureBaseAddress)
             .AddHttpMessageHandler<AuthHeaderHandler>()
             .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IWarehouseService, WarehouseService>(ConfigureBaseAddress)
            .AddHttpMessageHandler<AuthHeaderHandler>()
           .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IQualityApi, QualityApi>(ConfigureBaseAddress)
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IAuthApi, AuthApi>(ConfigureBaseAddress)
           .AddHttpMessageHandler<AuthHeaderHandler>()
           .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IEquipmentApi, EquipmentApi>(ConfigureBaseAddress)
          .AddHttpMessageHandler<AuthHeaderHandler>()
          .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IAttachmentApi, AttachmentApi>(ConfigureBaseAddress)
          .AddHttpMessageHandler<AuthHeaderHandler>()
          .AddHttpMessageHandler<TokenExpiredHandler>();
            builder.Services.AddHttpClient<IEnergyApi, EnergyApi>(ConfigureBaseAddress)
          .AddHttpMessageHandler<AuthHeaderHandler>()
          .AddHttpMessageHandler<TokenExpiredHandler>();

            var app = builder.Build();
            App.Services = app.Services;
            //CrashTrap.Init(); //Debug
            return app;
        }

        private static void ConfigureBaseAddress(IServiceProvider sp, HttpClient http)
        {
            var cfg = sp.GetRequiredService<IConfigLoader>();
            var baseUrl = cfg.GetBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("配置文件缺少有效的 BaseUrl。");

            if (baseUrl.EndsWith("/"))
                baseUrl = baseUrl.TrimEnd('/');

            http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
    }
}
