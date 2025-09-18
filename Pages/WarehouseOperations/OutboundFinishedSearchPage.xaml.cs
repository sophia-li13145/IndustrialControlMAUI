using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
namespace IndustrialControlMAUI.Pages;
public partial class OutboundFinishedSearchPage : ContentPage
{

    private readonly ScanService _scanSvc;
    private readonly OutboundFinishedSearchViewModel _vm;
    public OutboundFinishedSearchPage(OutboundFinishedSearchViewModel vm, ScanService scanSvc)
    {
        _vm = vm;

        BindingContext = vm;
        //_scanSvc = scanSvc;
        InitializeComponent();
        // 可选：配置前后缀与防抖
        //_scanSvc.Prefix = null;     // 例如 "}q" 之类的前缀；没有就留 null
                                    // _scanSvc.Suffix = "\n";     // 如果设备会附带换行，可去掉；没有就设 null
                                    //_scanSvc.DebounceMs = 250;
        //_scanSvc.Suffix = null;   // 先关掉
        //_scanSvc.DebounceMs = 0;  // 先关掉
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // 动态注册广播接收器（只在当前页面前台时生效）
        //_scanSvc.Scanned += OnScanned;
        //_scanSvc.StartListening();
        //键盘输入
        //_scanSvc.Attach(OrderEntry);
        OrderEntry.Focus();


    }

    /// <summary>
    /// 清空扫描记录
    /// </summary>
    void OnClearClicked(object sender, EventArgs e)
    {
        OrderEntry.Text = string.Empty;
        OrderEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        // 退出页面即注销（防止多个程序/页面抢处理）
        //_scanSvc.Scanned -= OnScanned;
        //_scanSvc.StopListening();

        base.OnDisappearing();
    }

    private void OnScanned(string data, string type)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 常见处理：自动填入单号/条码并触发查询或加入明细
            _vm.SearchOrderNo = data;
        });
    }


}
