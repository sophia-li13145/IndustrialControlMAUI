using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class WorkProcessTaskDetailPage : ContentPage
{
    private readonly WorkProcessTaskDetailViewModel _vm;
    public WorkProcessTaskDetailPage() : this(ServiceHelper.GetService<WorkProcessTaskDetailViewModel>()) { }

    public WorkProcessTaskDetailPage(WorkProcessTaskDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
        vm.TabChanged += (_, __) => ApplyTab(vm.ActiveTab);
        Loaded += (_, __) => ApplyTab(vm.ActiveTab);
        Seg.SizeChanged += (_, __) => ApplyTab(vm.ActiveTab);
    }

    private void ApplyTab(DetailTab tab)
    {
        Grid.SetColumn(Knob, tab == DetailTab.Input ? 0 : 1);
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 初次同步一次
        SyncSegWidth();

        // 视口变化时（旋转/窗口调整/首次布局），都同步一次
        BodyScroll.SizeChanged += OnViewportSizeChanged;

        // 你的 Tab 初始化（如果有）
        if (BindingContext is WorkProcessTaskDetailViewModel vm)
            ApplyTab(vm.ActiveTab);
    }

    private void OnViewportSizeChanged(object? sender, EventArgs e) => SyncSegWidth();

    private void SyncSegWidth()
    {
        // ScrollView 可见区域宽度（去掉内边距）
        var viewport = BodyScroll.Width
                       - BodyScroll.Padding.Left - BodyScroll.Padding.Right;

        if (viewport > 0)
            Seg.WidthRequest = viewport; // 强制与视口等宽 -> 紫色底铺满
    }

    private void OnReportQtyCompleted(object sender, EventArgs e)
    {

            _vm.SubmitReportQtyCommand.Execute(null);
        
    }


}