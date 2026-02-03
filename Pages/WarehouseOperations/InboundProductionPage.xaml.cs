using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using SharedLocationVM = IndustrialControlMAUI.ViewModels.LocationVM;

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
    private readonly IServiceProvider _sp;
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

    /// <summary>执行 InboundProductionPage 初始化逻辑。</summary>
    public InboundProductionPage(IServiceProvider sp,InboundProductionViewModel vm,  IDialogService dialogs)
    {
        InitializeComponent();
        BindingContext = vm;
       // _scanSvc = scanSvc;
        _vm = vm;
        _dialogs = dialogs;
        _sp = sp;

    }
    /// <summary>执行 OnScanEntryCompleted 逻辑。</summary>
    private async void OnScanEntryCompleted(object? sender, EventArgs e)
    {
        // 取输入框内容
        var code = ScanEntry?.Text?.Trim();

        // 可选：空码直接返回
        if (string.IsNullOrWhiteSpace(code))
        {
            // 也可以静默返回，不弹提示
            return;
        }

        // 交给 VM 统一处理（第二个参数随意标记来源）
        await _vm.HandleScannedAsync(code!, "KEYBOARD");

        // 清空并继续聚焦，方便下一次输入/扫码
        ScanEntry.Text = string.Empty;
        ScanEntry.Focus();
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
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

    /// <summary>执行 OnDisappearing 逻辑。</summary>
    protected override void OnDisappearing()
    {

        base.OnDisappearing();
    }

    // 扫码按钮事件
    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        // 等待扫码结果
        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // ✅ 直接交给 VM，避免回填 Entry.Text 导致 Completed/事件再次触发
        await _vm.HandleScannedAsync(result.Trim(), "CAMERA");

        // 体验：清空并聚焦输入框
        ScanEntry.Text = string.Empty;
        ScanEntry.Focus();
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
            await Shell.Current.GoToAsync(nameof(InboundProductionSearchPage));
        }
        else
        {
            await DisplayAlert("提示", "入库失败，请检查数据", "确定");
        }
    }


    /// <summary>执行 OnBinTapped 逻辑。</summary>
    private async void OnBinTapped(object? sender, TappedEventArgs e)
    {
        // ① 一定要从 sender 的 BindingContext 拿当前行
        if ((sender as BindableObject)?.BindingContext is not OutScannedItem item) return;

        // ② 打开选择页
        var picked = await WarehouseLocationPickerPage.ShowAsync(_sp, this);
        if (picked is null) return;

        // ③ 映射
        var bin = new BinInfo
        {
            WarehouseCode = picked.WarehouseCode,
            WarehouseName = picked.WarehouseName,
            ZoneCode = picked.Zone,
            RackCode = picked.Rack,
            LayerCode = picked.Layer,
            Location = picked.Location
        };

        // ④ 后端保存
        var ok = await _vm.UpdateRowLocationAsync(item, bin);
        if (!ok) { await DisplayAlert("提示", "库位更新失败", "确定"); return; }

        // ⑤ 本地行对象立刻更新（触发 UI）
        item.Location = string.IsNullOrWhiteSpace(bin.Location) ? "请选择" : bin.Location!;
        item.WarehouseCode = bin.WarehouseCode ?? "";

        var target = _vm.ScannedList.FirstOrDefault(x =>
        string.Equals(x.DetailId, item.DetailId, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            target.Location = item.Location;
            target.WarehouseCode = item.WarehouseCode;
        }
    }



    /// <summary>执行 OnQtyCompleted 逻辑。</summary>
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
