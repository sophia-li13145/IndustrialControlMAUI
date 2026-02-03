using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using SharedLocationVM = IndustrialControlMAUI.ViewModels.LocationVM;

namespace IndustrialControlMAUI.Pages;

[QueryProperty(nameof(OutstockId), "outstockId")]
[QueryProperty(nameof(OutstockNo), "outstockNo")]
[QueryProperty(nameof(RequisitionMaterialNo), "requisitionMaterialNo")]
[QueryProperty(nameof(WorkOrderNo), "workOrderNo")]
[QueryProperty(nameof(Memo), "memo")]
public partial class OutboundMaterialPage : ContentPage
{
    private readonly OutboundMaterialViewModel _vm;
    public string? OutstockId { get; set; }
    public string? OutstockNo { get; set; }
    public string? RequisitionMaterialNo { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? Memo { get; set; }
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _sp;
    private bool _loadedOnce = false;

    /// <summary>执行 OutboundMaterialPage 初始化逻辑。</summary>
    public OutboundMaterialPage(IServiceProvider sp, OutboundMaterialViewModel vm, IDialogService dialogs)
    {
        InitializeComponent();
        BindingContext = vm;
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
        // 防止重复初始化
        if (_loadedOnce) return;
        _loadedOnce = true;

        // ✅ 用搜索页带过来的基础信息初始化页面，并拉取两张表
        if (!string.IsNullOrWhiteSpace(OutstockId))
        {
            await _vm.InitializeFromSearchAsync(
                outstockId: OutstockId ?? "",
                outstockNo: OutstockNo ?? "",
                requisitionMaterialNo: RequisitionMaterialNo ?? "",
                workOrderNo: WorkOrderNo ?? "",
                memo: Memo ?? ""
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
        // 退出页面即注销（防止多个程序/页面抢处理）

        base.OnDisappearing();
    }

    // 新增：扫码按钮事件
    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        // 等待扫码结果
        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 回填到输入框
        ScanEntry.Text = result.Trim();

        // 同步到 ViewModel
        if (BindingContext is OutboundMaterialViewModel vm)
        {
            // 交给 VM 统一处理（第二个参数随意标记来源）
            await _vm.HandleScannedAsync(ScanEntry.Text!, "KEYBOARD");

            // 清空并继续聚焦，方便下一次输入/扫码
            ScanEntry.Text = string.Empty;
            ScanEntry.Focus();
        }
    }


    /// <summary>
    /// 确认入库按钮点击
    /// </summary>
    async void OnConfirmClicked(object sender, EventArgs e)
    {
        var ok = await _vm.ConfirmOutboundAsync();
        if (ok)
        {
            await DisplayAlert("提示", "入库成功", "确定");
            _vm.ClearAll();

            // ✅ 返回到工单查询页面（InboundMaterialSearchPage）
            await Shell.Current.GoToAsync(nameof(OutboundMaterialSearchPage));
        }
        else
        {
            await DisplayAlert("提示", "入库失败，请检查数据", "确定");
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

        // 8) （可选）切回“已扫描”页签
        _vm.SwitchTab(false);
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
