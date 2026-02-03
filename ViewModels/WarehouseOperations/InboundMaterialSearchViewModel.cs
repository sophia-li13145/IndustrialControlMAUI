
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using Serilog;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class InboundMaterialSearchViewModel : ObservableObject
{
    private readonly IInboundMaterialService _dataSvc;

    [ObservableProperty] private string searchOrderNo;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(7);
    private CancellationTokenSource? _searchCts;
    // 仅用于“高亮选中”
    [ObservableProperty] private InboundOrderSummary? selectedOrder;

    /// <summary>执行 InboundMaterialSearchViewModel 初始化逻辑。</summary>
    public InboundMaterialSearchViewModel(IInboundMaterialService dataSvc)
    {
        _dataSvc = dataSvc;
        Orders = new ObservableCollection<InboundOrderSummary>();
    }

    public ObservableCollection<InboundOrderSummary> Orders { get; }

    /// <summary>执行 SearchAsync 逻辑。</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try
        {
            var instockStatusList = new[] { "0", "1" };
            var orderTypeList = new[] { "in_other", "in_purchase", "in_return" };
            var list = await _dataSvc.ListInboundOrdersAsync(
            searchOrderNo,           // 单号/条码
            startDate,               // 开始日期
            endDate,
            // 结束日期（Service 内会扩到 23:59:59）
            instockStatusList,
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
    /// <summary>执行 GoInboundAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanGoInbound))]
    private async Task GoInboundAsync(InboundOrderSummary? item)
    {
        if (item is null) return;

        /// <summary>执行 E 逻辑。</summary>
        static string E(string? v) => Uri.EscapeDataString(v ?? "");

        var o = item;
        Log.Information("MaterialPage准备跳转");
        await Shell.Current.GoToAsync(
    nameof(Pages.InboundMaterialPage),
    new Dictionary<string, object>
    {
        ["instockId"] = o.instockId,
        ["instockNo"] = o.instockNo,
        ["orderType"] = o.orderType,
        ["orderTypeName"] = o.orderTypeName,
        ["supplierName"] = o.supplierName,
        ["purchaseNo"] = o.purchaseNo,
        ["arrivalNo"] = o.arrivalNo,
        ["createdTime"] = o.createdTime
    });

    }

    // 与命令同签名的 CanExecute
    /// <summary>执行 CanGoInbound 逻辑。</summary>
    private bool CanGoInbound(InboundOrderSummary? item) => item != null;
}

// 用于列表显示的精简 DTO
public record InboundOrderSummary(
    string instockId,
    string instockNo,
    string orderType,
    string orderTypeName,
    string purchaseNo,
    string supplierName,
    string arrivalNo,
    string workOrderNo,
    string materialName,
    int instockQty,
    string createdTime
);
