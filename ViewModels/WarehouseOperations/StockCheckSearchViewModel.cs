using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services; 
using System.Collections.ObjectModel;
using System.Text.Json;

namespace IndustrialControlMAUI.ViewModels;

/// <summary>
/// 库存盘点查询 VM
/// </summary>
public partial class StockCheckSearchViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;

    [ObservableProperty] private string? searchCheckNo;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(7);

    private CancellationTokenSource? _searchCts;

    public ObservableCollection<StockCheckOrderItem> Orders { get; } = new();

    public StockCheckSearchViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    #region 查询

    [RelayCommand]
    public async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            var checkNo = SearchCheckNo?.Trim();
          

            var resp = await _api.PageStockCheckOrdersAsync(
                checkNo: checkNo,
                beginDate: startDate,
                endDate: endDate,
                searchCount: false,
                pageNo: 1,
                pageSize: 50,
                ct: ct);

            var list = resp?.result?.records ?? new List<StockCheckOrderItem>();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Orders.Clear();
                foreach (var o in list)
                    Orders.Add(o);
            });

            if (list.Count == 0)
            {
                await Shell.Current.DisplayAlert("提示", "未查询到任何盘点单", "确定");
            }
        }
        catch (OperationCanceledException)
        {
            // 用户快速连续点查询时取消，不提示
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("查询失败", ex.Message, "确定");
        }
    }

    #endregion

    #region 灵活盘点

    [RelayCommand]
    private async Task OpenFlexibleCheckAsync()
    {
        // 先简单留个入口，你后面可以跳转到灵活盘点页面
        await Shell.Current.GoToAsync(nameof(FlexibleStockCheckPage));
    }

    [RelayCommand]
    private async Task GoFlexibleAsync(StockCheckOrderItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync(
     $"{nameof(FlexibleStockCheckPage)}" +
     $"?CheckNo={item.checkNo}&CheckId={item.id}" +
     $"&WarehouseCode={item.warehouseCode}" +
     $"&AuditStatus={item.auditStatus}");

    }
    #endregion
}
