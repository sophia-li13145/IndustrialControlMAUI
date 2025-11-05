namespace IndustrialControlMAUI;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _sp;
    public const string RouteHome = "//Home";
    public const string RouteLogin = "//Login";
    public AppShell(IServiceProvider sp)
    {
        InitializeComponent();
        _sp = sp;
        RegisterRoutes();

        // 构造时先放一个“未登录”的壳；真正状态由 App 调用 ApplyAuth 设置
        BuildTabs(authed: false);
    }
    private void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(Pages.InboundMaterialSearchPage), typeof(Pages.InboundMaterialSearchPage));
        Routing.RegisterRoute(nameof(Pages.InboundMaterialPage), typeof(Pages.InboundMaterialPage));
        Routing.RegisterRoute(nameof(Pages.InboundProductionSearchPage), typeof(Pages.InboundProductionSearchPage));
        Routing.RegisterRoute(nameof(Pages.InboundProductionPage), typeof(Pages.InboundProductionPage));
        Routing.RegisterRoute(nameof(Pages.OutboundMaterialSearchPage), typeof(Pages.OutboundMaterialSearchPage));
        Routing.RegisterRoute(nameof(Pages.OutboundMaterialPage), typeof(Pages.OutboundMaterialPage));
        Routing.RegisterRoute(nameof(Pages.OutboundFinishedPage), typeof(Pages.OutboundFinishedPage));
        Routing.RegisterRoute(nameof(Pages.OutboundFinishedSearchPage), typeof(Pages.OutboundFinishedSearchPage));
        Routing.RegisterRoute(nameof(Pages.InboundMoldPage), typeof(Pages.InboundMoldPage));
        Routing.RegisterRoute(nameof(Pages.OutboundMoldPage), typeof(Pages.OutboundMoldPage));
        Routing.RegisterRoute(nameof(Pages.OutboundMoldSearchPage), typeof(Pages.OutboundMoldSearchPage));
        Routing.RegisterRoute(nameof(Pages.WorkOrderSearchPage), typeof(Pages.WorkOrderSearchPage));
        Routing.RegisterRoute(nameof(Pages.MoldOutboundExecutePage), typeof(Pages.MoldOutboundExecutePage));
        Routing.RegisterRoute(nameof(Pages.ProcessTaskSearchPage), typeof(Pages.ProcessTaskSearchPage));
        Routing.RegisterRoute(nameof(Pages.WorkProcessTaskDetailPage), typeof(Pages.WorkProcessTaskDetailPage));
        Routing.RegisterRoute(nameof(Pages.ProcessQualitySearchPage), typeof(Pages.ProcessQualitySearchPage));
        Routing.RegisterRoute(nameof(Pages.ProcessQualityDetailPage), typeof(Pages.ProcessQualityDetailPage));
        Routing.RegisterRoute(nameof(Pages.FinishedQualitySearchPage), typeof(Pages.FinishedQualitySearchPage));
        Routing.RegisterRoute(nameof(Pages.FinishedQualityDetailPage), typeof(Pages.FinishedQualityDetailPage));
        Routing.RegisterRoute(nameof(Pages.InspectionSearchPage), typeof(Pages.InspectionSearchPage));
        Routing.RegisterRoute(nameof(Pages.InspectionDetailPage), typeof(Pages.InspectionDetailPage));
        Routing.RegisterRoute(nameof(Pages.MaintenanceSearchPage), typeof(Pages.MaintenanceSearchPage));
        Routing.RegisterRoute(nameof(Pages.MaintenanceDetailPage), typeof(Pages.MaintenanceDetailPage));
        Routing.RegisterRoute(nameof(Pages.RepairSearchPage), typeof(Pages.RepairSearchPage));
        Routing.RegisterRoute(nameof(Pages.RepairDetailPage), typeof(Pages.RepairDetailPage));
        Routing.RegisterRoute(nameof(Pages.ManualReadingPage),typeof(Pages.ManualReadingPage));
    }

    /// <summary>根据是否登录重建 TabBar 并跳转到对应根。</summary>
    public void ApplyAuth(bool authed) =>
        MainThread.BeginInvokeOnMainThread(async () => await ApplyAuthAsync(authed));

    public async Task ApplyAuthAsync(bool authed)
    {
        BuildTabs(authed);  // 重建 Items
        var target = authed ? RouteHome : RouteLogin;
        await GoToAsync(target);
    }

    private void BuildTabs(bool authed)
    {
        Items.Clear();

        var bar = new TabBar();

        // 公共：日志
        bar.Items.Add(new Tab
        {
            Title = "日志",
            Items =
            {
                new ShellContent
                {
                    Route = "Logs",
                    ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.LogsPage>())
                }
            }
        });

        // 公共：管理员
        bar.Items.Add(new Tab
        {
            Title = "管理员",
            Items =
            {
                new ShellContent
                {
                    Route = "Admin",
                    ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.AdminPage>())
                }
            }
        });

        if (authed)
        {
            // 已登录：主页放在最前，并把根路由命名为 Home
            bar.Items.Insert(0, new Tab
            {
                Title = "主页",
                Items =
                {
                    new ShellContent
                    {
                        Route = "Home",    // 对应 RouteHome = "//Home"
                        ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.HomePage>())
                    }
                }
            });
        }
        else
        {
            // 未登录：登录页在最前，并把根路由命名为 Login
            bar.Items.Insert(0, new Tab
            {
                Title = "登录",
                Items =
                {
                    new ShellContent
                    {
                        Route = "Login",   // 对应 RouteLogin = "//Login"
                        ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.LoginPage>())
                    }
                }
            });
        }

        Items.Add(bar);
    }
}
