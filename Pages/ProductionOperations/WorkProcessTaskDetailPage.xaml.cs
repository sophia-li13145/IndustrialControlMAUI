using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class WorkProcessTaskDetailPage : ContentPage
{
    private readonly WorkProcessTaskDetailViewModel _vm;
    /// <summary>执行 WorkProcessTaskDetailPage 初始化逻辑。</summary>
    public WorkProcessTaskDetailPage() : this(ServiceHelper.GetService<WorkProcessTaskDetailViewModel>()) { }

    /// <summary>执行 WorkProcessTaskDetailPage 初始化逻辑。</summary>
    public WorkProcessTaskDetailPage(WorkProcessTaskDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
        vm.TabChanged += (_, __) => ApplyTab(vm.ActiveTab);
        Loaded += (_, __) => ApplyTab(vm.ActiveTab);
        Seg.SizeChanged += (_, __) => ApplyTab(vm.ActiveTab);
    }

    /// <summary>执行 ApplyTab 逻辑。</summary>
    private void ApplyTab(DetailTab tab)
    {
        Grid.SetColumn(Knob, tab == DetailTab.Input ? 0 : 1);
    }


    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 同步一次
        SyncSegWidth();

        // 视口变化时重新同步（旋转/缩放/状态栏变化）
        BodyScroll.SizeChanged += OnViewportSizeChanged;

        //  Tab 初始校正
        if (BindingContext is WorkProcessTaskDetailViewModel vm)
            ApplyTab(vm.ActiveTab);
    }

    /// <summary>执行 OnViewportSizeChanged 逻辑。</summary>
    private void OnViewportSizeChanged(object? sender, EventArgs e) => SyncSegWidth();

    /// <summary>执行 SyncSegWidth 逻辑。</summary>
    private void SyncSegWidth()
    {
        // ScrollView 可视宽度（去除边距）
        var viewport = BodyScroll.Width
                       - BodyScroll.Padding.Left - BodyScroll.Padding.Right;

        if (viewport > 0)
            Seg.WidthRequest = viewport; // 强制与视口等宽
    }

    /// <summary>执行 OnReportQtyCompleted 逻辑。</summary>
    private void OnReportQtyCompleted(object sender, EventArgs e)
    {

            _vm.SubmitReportQtyCommand.Execute(null);
        
    }


}