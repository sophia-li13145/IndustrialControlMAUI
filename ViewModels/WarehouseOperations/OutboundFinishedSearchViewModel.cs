using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class OutboundFinishedSearchViewModel : ObservableObject
{
    private readonly IOutboundMaterialService _dataSvc;

    [ObservableProperty] private string searchOrderNo;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(7);
    private CancellationTokenSource? _searchCts;
    // 仅用于“高亮选中”
    [ObservableProperty] private OutboundOrderSummary? selectedOrder;

    /// <summary>执行 OutboundFinishedSearchViewModel 初始化逻辑。</summary>
    public OutboundFinishedSearchViewModel(IOutboundMaterialService dataSvc)
    {
        _dataSvc = dataSvc;
        Orders = new ObservableCollection<OutboundOrderSummary>();
    }

    public ObservableCollection<OutboundOrderSummary> Orders { get; }

    /// <summary>执行 SearchAsync 逻辑。</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        
        var ct = _searchCts.Token;
        try
        {
            var outstockStatusList = new[] { "0", "1" };
            var list = await _dataSvc.ListOutboundOrdersAsync(
            searchOrderNo,           // 单号/条码
            startDate,               // 开始日期
            endDate,                 // 结束日期（Service 内会扩到 23:59:59）
            outstockStatusList,
            "out_delivery",                    // 不传单值 orderType，用 null 更清晰
            null,           // 多类型数组
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
                await Shell.Current.DisplayAlert("提示", "未查询到任何出库单", "确定");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("查询失败", ex.Message, "确定");
        }
    }

    // === 方案A：命令接收“当前项”作为参数，不依赖 SelectedOrder ===
    /// <summary>执行 GoOutboundAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanGoOutbound))]
    private async Task GoOutboundAsync(OutboundOrderSummary? item)
    {
        if (item is null) return;

        /// <summary>执行 E 逻辑。</summary>
        static string E(string? v) => Uri.EscapeDataString(v ?? "");

        var o = item;

        await Shell.Current.GoToAsync(
    nameof(Pages.OutboundFinishedPage),
    new Dictionary<string, object>
    {
        ["outstockId"] = o.outstockId,
        ["outstockNo"] = o.outstockNo,
        ["deliveryNo"] = o.deliveryNo,
        ["deliveryMemo"] = o.deliveryMemo,
        ["customer"] = o.customer,
        ["saleNo"] = o.saleNo,
        ["expectedDeliveryTime"] = o.expectedDeliveryTime
    });

    }

    // 与命令同签名的 CanExecute
    /// <summary>执行 CanGoOutbound 逻辑。</summary>
    private bool CanGoOutbound(OutboundOrderSummary? item) => item != null;
}


