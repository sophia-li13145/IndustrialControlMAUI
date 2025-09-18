using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class OutboundMaterialSearchViewModel : ObservableObject
{
    private readonly IOutboundMaterialService _dataSvc;

    [ObservableProperty] private string searchOrderNo;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    private CancellationTokenSource? _searchCts;
    // 仅用于“高亮选中”
    [ObservableProperty] private OutboundOrderSummary? selectedOrder;

    public OutboundMaterialSearchViewModel(IOutboundMaterialService dataSvc)
    {
        _dataSvc = dataSvc;
        Orders = new ObservableCollection<OutboundOrderSummary>();
    }

    public ObservableCollection<OutboundOrderSummary> Orders { get; }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try
        {
            var orderTypeList = new[] { "out_return", "out_requisition", "out_other" };
            var outstockStatusList = new[] { "0", "1" };
            var list = await _dataSvc.ListOutboundOrdersAsync(
            searchOrderNo,           // 单号/条码
            startDate,               // 开始日期
            endDate,                 // 结束日期（Service 内会扩到 23:59:59）
            outstockStatusList,
            null,                    // 不传单值 orderType，用 null 更清晰
            orderTypeList,           // 多类型数组
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
    [RelayCommand(CanExecute = nameof(CanGoOutbound))]
    private async Task GoOutboundAsync(OutboundOrderSummary? item)
    {
        if (item is null) return;

        static string E(string? v) => Uri.EscapeDataString(v ?? "");

        var o = item;

        await Shell.Current.GoToAsync(
    nameof(Pages.OutboundMaterialPage),
    new Dictionary<string, object>
    {
        ["outstockId"] = o.outstockId,
        ["outstockNo"] = o.outstockNo,
        ["requisitionMaterialNo"] = o.requisitionMaterialNo,
        ["workOrderNo"] = o.workOrderNo,
        ["memo"] = o.memo
    });

    }

    // 与命令同签名的 CanExecute
    private bool CanGoOutbound(OutboundOrderSummary? item) => item != null;
}

// 用于列表显示的精简 DTO
public record OutboundOrderSummary(
    string outstockId,
    string outstockNo,
    string orderType,
    string orderTypeName,
    string workOrderNo,
    string returnNo,
    string deliveryNo,
    string requisitionMaterialNo,
    string customer,
    string deliveryMemo,
    string expectedDeliveryTime,
    string memo,
    string saleNo,
    string createdTime
);
