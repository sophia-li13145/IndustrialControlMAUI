using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using SharedLocationVM = IndustrialControlMAUI.ViewModels.LocationVM;


namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(InstockId), "instockId")]
[QueryProperty(nameof(InstockNo), "instockNo")]
[QueryProperty(nameof(OrderType), "orderType")]
[QueryProperty(nameof(OrderTypeName), "orderTypeName")]
[QueryProperty(nameof(ArrivalNo), "arrivalNo")]
[QueryProperty(nameof(PurchaseNo), "purchaseNo")]
[QueryProperty(nameof(SupplierName), "supplierName")]
[QueryProperty(nameof(CreatedTime), "createdTime")]
public partial class InboundMaterialPage : ContentPage
{
    private readonly InboundMaterialViewModel _vm;
    public string? InstockId { get; set; }
    public string? InstockNo { get; set; }
    public string? OrderType { get; set; }
    public string? OrderTypeName { get; set; }
    public string? ArrivalNo { get; set; }
    public string? PurchaseNo { get; set; }
    public string? SupplierName { get; set; }
    public string? CreatedTime { get; set; }
    private readonly IDialogService _dialogs;
    private bool _loadedOnce = false;
    private readonly IServiceProvider _sp;
    private bool _isConfirming;
    private CancellationTokenSource? _qtyUpdateCts;

    /// <summary>执行 InboundMaterialPage 初始化逻辑。</summary>
    public InboundMaterialPage(IServiceProvider sp, InboundMaterialViewModel vm,IDialogService dialogs)
    {
        InitializeComponent();
        _sp = sp;
        BindingContext = vm;

        _vm = vm;
        _dialogs = dialogs;


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

        // 防止重复初始化
        if (_loadedOnce) return;
        _loadedOnce = true;

        if (!string.IsNullOrWhiteSpace(InstockId))
        {
            await _vm.InitializeFromSearchAsync(
                instockId: InstockId ?? "",
                instockNo: InstockNo ?? "",
                orderType: OrderType ?? "",
                orderTypeName: OrderTypeName ?? "",
                purchaseNo: PurchaseNo ?? "",
                arrivalNo: ArrivalNo ?? "",
                supplierName: SupplierName ?? "",
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



    /// <summary>
    /// 确认按钮点击，防止连续点击导致重复提交。
    /// </summary>
    async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (_isConfirming)
        {
            return;
        }

        _isConfirming = true;
        ConfirmButtonContainer.IsEnabled = false;
        ConfirmButtonContainer.Opacity = 0.6;

        try
        {
            var ok = await _vm.ConfirmInboundAsync();
            if (ok)
            {
                await DisplayAlert("提示", "入库成功", "确定");
                _vm.ClearAll();

                await Shell.Current.GoToAsync(nameof(InboundMaterialSearchPage));
            }
            else
            {
                await DisplayAlert("提示", "入库失败，请检查数据", "确定");
            }
        }
        finally
        {
            _isConfirming = false;
            ConfirmButtonContainer.IsEnabled = true;
            ConfirmButtonContainer.Opacity = 1;
        }
    }

    /// <summary>执行 OnBinTapped 逻辑。</summary>
    private async void OnBinTapped(object? sender, TappedEventArgs e)
    {
        // 1) 取到行对象
        if ((sender as BindableObject)?.BindingContext is not IndustrialControlMAUI.ViewModels.OutScannedItem item)
            return;

        // 2) 未扫描通过禁止修改
        if (!item.ScanStatus)
        {
            await DisplayAlert("提示", "该行未扫描通过，不能修改库位。", "确定");
            return;
        }

        // 3) 打开库位选择页（统一用 ShowAsync）
        var picked = await WarehouseLocationPickerPage.ShowAsync(_sp, this);
        if (picked is null) return;

        // 4) 映射为后端需要的结构
        var bin = new BinInfo
        {
            WarehouseCode = picked.WarehouseCode,
            WarehouseName = picked.WarehouseName,
            ZoneCode = picked.Zone,
            RackCode = picked.Rack,
            LayerCode = picked.Layer,
            Location = picked.Location,
            InventoryStatus = picked.InventoryStatus,
            InStock = string.Equals(picked.InventoryStatus, "instock", StringComparison.OrdinalIgnoreCase)
        };

        // 5) 先调接口保存（让 VM 负责请求）
        var ok = await _vm.UpdateRowLocationAsync(item, bin);
        if (!ok)
        {
            await DisplayAlert("提示", "库位更新失败，请重试。", "确定");
            return;
        }

        // 6) ✅ 本地行对象立刻同步（触发 UI 刷新）
        item.Location = string.IsNullOrWhiteSpace(bin.Location) ? "请选择" : bin.Location!;
        item.WarehouseCode = bin.WarehouseCode ?? "";

        // 7) （兜底，可选）若模板或转换器未触发刷新，则替换集合项强制刷新
        var target = _vm.ScannedList.FirstOrDefault(x =>
        string.Equals(x.DetailId, item.DetailId, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            target.Location = item.Location;
            target.WarehouseCode = item.WarehouseCode;
        }
        _vm.SwitchTab(false);

        
    }




    /// <summary>数量输入框获得焦点时清掉占位 0，方便直接输入目标数量。</summary>
    private void OnQtyFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && string.Equals(entry.Text?.Trim(), "0", StringComparison.Ordinal))
        {
            entry.Text = string.Empty;
        }
    }

    /// <summary>数量输入变化后立即提交，不再等待键盘回车。</summary>
    private async void OnQtyTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || !entry.IsFocused) return;
        if (entry.BindingContext is not IndustrialControlMAUI.ViewModels.OutScannedItem row) return;

        _qtyUpdateCts?.Cancel();
        _qtyUpdateCts?.Dispose();
        _qtyUpdateCts = null;
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) return;

        var cts = _qtyUpdateCts = new CancellationTokenSource();

        try
        {
            // 等待短暂输入间隔，避免连续按键时重复提交中间值。
            await Task.Delay(300, cts.Token);
            if (cts.IsCancellationRequested || !entry.IsFocused) return;

            // 只看 ScanStatus：未通过则不提交
            if (!row.ScanStatus)
            {
                await DisplayAlert("提示", "该行尚未扫描通过，不能修改数量。", "确定");
                return;
            }

            await _vm.UpdateQuantityForRowAsync(row, showSuccessTip: false);
        }
        catch (TaskCanceledException)
        {
            // 用户继续输入时取消上一次待提交。
        }
        catch (Exception ex) when (ex is System.Net.WebException || ex.Message.Contains("Socket closed", StringComparison.OrdinalIgnoreCase))
        {
            // 自动提交过程中如果用户继续输入/页面网络连接被关闭，不要让 async void 事件处理器抛出未处理异常。
        }
    }

    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result)) return;

        // 直接交给 VM，别再设置 ScanEntry.Text
        await _vm.HandleScannedAsync(result.Trim(), "CAMERA");

        // 保持体验一致
        ScanEntry.Text = string.Empty;
        ScanEntry.Focus();
    }


}
