using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class InboundProductionSearchViewModel : ObservableObject
{
    private readonly IInboundMaterialService _dataSvc;

    [ObservableProperty] private string searchOrderNo;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(7);
    private CancellationTokenSource? _searchCts;
    // 仅用于“高亮选中”
    [ObservableProperty] private InboundOrderSummary? selectedOrder;

    public InboundProductionSearchViewModel(IInboundMaterialService dataSvc)
    {
        _dataSvc = dataSvc;
        Orders = new ObservableCollection<InboundOrderSummary>();
    }

    public ObservableCollection<InboundOrderSummary> Orders { get; }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        var instockStatusList = new[] { "0", "1" };
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try
        {
            var list = await _dataSvc.ListInboundOrdersAsync(
            searchOrderNo,           // 单号/条码
            startDate,               // 开始日期
            endDate,// 结束日期（Service 内会扩到 23:59:59）
            instockStatusList,
            "in_production",         // 不传单值 orderType，用 null 更清晰
            null,
            ct                       // ← 新增：取消令牌
            );

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Orders.Clear();
                if (list != null)
                {
                    foreach (var o in list)
                        Orders.Add(o);
                }
            });

            if (list == null || !list.Any())
                await Shell.Current.DisplayAlert("提示", "未查询到任何入库单", "确定");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("查询失败", ex.Message, "确定");
        }
    }

    // === 方案A：命令接收“当前项”作为参数，不依赖 SelectedOrder ===
    [RelayCommand(CanExecute = nameof(CanGoInbound))]
    private async Task GoInboundAsync(InboundOrderSummary? item)
    {
        if (item is null) return;

        static string E(string? v) => Uri.EscapeDataString(v ?? "");

        var o = item;

        await Shell.Current.GoToAsync(
    nameof(Pages.InboundProductionPage),
    new Dictionary<string, object>
    {
        ["instockId"] = o.instockId,
        ["instockNo"] = o.instockNo,
        ["orderType"] = o.orderType,
        ["orderTypeName"] = o.orderTypeName,
        ["purchaseNo"] = o.purchaseNo,
        ["supplierName"] = o.supplierName,
        ["arrivalNo"] = o.arrivalNo,
        ["instockQty"] = o.instockQty,
        ["materialName"] = o.materialName,
        ["workOrderNo"] = o.workOrderNo,
        ["createdTime"] = o.createdTime
    });

    }

    // 与命令同签名的 CanExecute
    private bool CanGoInbound(InboundOrderSummary? item) => item != null;
}
