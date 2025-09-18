namespace IndustrialControlMAUI;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _sp;

    public AppShell(bool authed)
    {
        InitializeComponent();
        _sp = App.Services ?? throw new InvalidOperationException("Services not ready");
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
        BuildTabs(authed);
    }

    private void BuildTabs(bool authed)
    {
        Items.Clear();

        var bar = new TabBar();

        // 公共页：日志
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

        // 公共页：管理员
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
            // 已登录：插入主页到最前
            bar.Items.Insert(0, new Tab
            {
                Title = "主页",
                Items =
            {
                new ShellContent
                {
                    Route = "Home",
                    ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.HomePage>())
                }
            }
            });

            Items.Add(bar);
            _ = GoToAsync("//Home");
        }
        else
        {
            // 未登录：插入登录到最前
            bar.Items.Insert(0, new Tab
            {
                Title = "登录",
                Items =
            {
                new ShellContent
                {
                    Route = "Login",
                    ContentTemplate = new DataTemplate(() => _sp.GetRequiredService<Pages.LoginPage>())
                }
            }
            });

            Items.Add(bar);
            _ = GoToAsync("//Login");
        }
    }

}
