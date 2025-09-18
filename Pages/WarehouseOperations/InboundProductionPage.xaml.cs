using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(InstockId), "instockId")]
[QueryProperty(nameof(InstockNo), "instockNo")]
[QueryProperty(nameof(OrderType), "orderType")]
[QueryProperty(nameof(OrderTypeName), "orderTypeName")]
[QueryProperty(nameof(PurchaseNo), "purchaseNo")]
[QueryProperty(nameof(SupplierName), "supplierName")]
[QueryProperty(nameof(WorkOrderNo), "workOrderNo")]
[QueryProperty(nameof(MaterialName), "materialName")]
[QueryProperty(nameof(InstockQty), "instockQty")]
[QueryProperty(nameof(CreatedTime), "createdTime")]
public partial class InboundProductionPage : ContentPage
{
    //private readonly ScanService _scanSvc;
    private readonly InboundProductionViewModel _vm;
    public string? InstockId { get; set; }
    public string? InstockNo { get; set; }
    public string? OrderType { get; set; }
    public string? OrderTypeName { get; set; }
    public string? PurchaseNo { get; set; }
    public string? SupplierName { get; set; }
    public string? CreatedTime { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? MaterialName { get; set; }
    public int InstockQty { get; set; }

    private readonly IDialogService _dialogs;

    public InboundProductionPage(InboundProductionViewModel vm,  IDialogService dialogs)
    {
        InitializeComponent();
        BindingContext = vm;
       // _scanSvc = scanSvc;
        _vm = vm;
        _dialogs = dialogs;
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

        // ✅ 用搜索页带过来的基础信息初始化页面，并拉取两张表
        if (!string.IsNullOrWhiteSpace(InstockId))
        {
            await _vm.InitializeFromSearchAsync(
                instockId: InstockId ?? "",
                instockNo: InstockNo ?? "",
                orderType: OrderType ?? "",
                orderTypeName: OrderTypeName ?? "",
                purchaseNo: PurchaseNo ?? "",
                supplierName: SupplierName ?? "",
                workOrderNo: WorkOrderNo ?? "",
                materialName: MaterialName ?? "",
                instockQty: InstockQty,
                createdTime: CreatedTime ?? ""
            );
        }

        //_scanSvc.Scanned += OnScanned;
        //_scanSvc.StartListening();
        //_scanSvc.Attach(ScanEntry);
        ScanEntry.Focus();
    }


    /// <summary>
    /// 清空扫描记录
    /// </summary>
    void OnClearClicked(object sender, EventArgs e)
    {
        _vm.ClearScan();
        ScanEntry.Text = string.Empty;
        ScanEntry.Focus();
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
            _vm.ScanCode = data;

            // 你原本的逻辑：若识别到是订单号 → 查询；若是包装码 → 加入列表等
            await _vm.HandleScannedAsync(data, type);
        });
    }


    /// <summary>
    /// 确认入库按钮点击
    /// </summary>
    async void OnConfirmClicked(object sender, EventArgs e)
    {
        var ok = await _vm.ConfirmInboundAsync();
        if (ok)
        {
            await DisplayAlert("提示", "入库成功", "确定");
            _vm.ClearAll();

            // ✅ 返回到工单查询页面（InboundMaterialSearchPage）
            await Shell.Current.GoToAsync($"//{nameof(InboundProductionSearchPage)}");
        }
        else
        {
            await DisplayAlert("提示", "入库失败，请检查数据", "确定");
        }
    }


    private async void OnBinTapped(object? sender, TappedEventArgs e)
    {
        var bindable = sender as BindableObject;
        var row = bindable?.BindingContext;
        if (row == null) return;

        var type = row.GetType();
        var currentBin = type.GetProperty("Location")?.GetValue(row)?.ToString();

        // 1) 打开公共组件选择库位（你已完成的组件）
        var picked = await BinPickerPage.ShowAsync(currentBin);
        if (picked == null) return;

        // 2) 调用 VM：带上行对象 + 选中的 BinInfo，内部会调接口 & 回填行
        await _vm.UpdateRowLocationAsync(row, picked);
    }

    private async void OnQtyCompleted(object sender, EventArgs e)
    {
        if (sender is not Entry entry) return;
        if (entry.BindingContext is not IndustrialControlMAUI.ViewModels.OutScannedItem row) return;

        // 只看 ScanStatus：未通过则不提交
        if (!row.ScanStatus)
        {
            await DisplayAlert("提示", "该行尚未扫描通过，不能修改数量。", "确定");
            return;
        }

        await _vm.UpdateQuantityForRowAsync(row);
    }

}
