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

    // 分页相关
    private const int PageSize = 10;
    private int _pageNo = 1;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMore = true;

    private CancellationTokenSource? _searchCts;

    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<StockCheckOrderItem> Orders { get; } = new();

    /// <summary>执行 StockCheckSearchViewModel 初始化逻辑。</summary>
    public StockCheckSearchViewModel(IWorkOrderApi api)
    {
        _api = api;
    }

    #region 查询第一页

    /// <summary>执行 SearchAsync 逻辑。</summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        if (IsLoading) return;  // 防止重复点

        try
        {
            IsLoading = true;
            _pageNo = 1;
            HasMore = true;

            var checkNo = SearchCheckNo?.Trim();

            var resp = await _api.PageStockCheckOrdersAsync(
                checkNo: checkNo,
                beginDate: startDate,
                endDate: endDate,
                searchCount: false,
                pageNo: _pageNo,
                pageSize: PageSize);  // 不再传 ct

            var list = resp?.result?.records ?? new List<StockCheckOrderItem>();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Orders.Clear();
                foreach (var o in list)
                    Orders.Add(o);
            });

            HasMore = list.Count >= PageSize;

            if (list.Count == 0)
            {
                await Shell.Current.DisplayAlert("提示", "未查询到任何盘点单", "确定");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("查询失败", ex.Message, "确定");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region 上拉加载更多

    /// <summary>执行 LoadMoreAsync 逻辑。</summary>
    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!HasMore || IsLoading || IsLoadingMore) return;

        try
        {
            IsLoadingMore = true;
            _pageNo++;

            var checkNo = SearchCheckNo?.Trim();

            var resp = await _api.PageStockCheckOrdersAsync(
                checkNo: checkNo,
                beginDate: startDate,
                endDate: endDate,
                searchCount: false,
                pageNo: _pageNo,
                pageSize: PageSize);

            var list = resp?.result?.records ?? new List<StockCheckOrderItem>();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var o in list)
                    Orders.Add(o);
            });

            HasMore = list.Count >= PageSize;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("加载更多失败", ex.Message, "确定");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    #endregion


    #region 灵活盘点（原来的保持不变）

    /// <summary>执行 OpenFlexibleCheckAsync 逻辑。</summary>
    [RelayCommand]
    private async Task OpenFlexibleCheckAsync()
    {
        await Shell.Current.GoToAsync(nameof(FlexibleStockCheckPage));
    }

    /// <summary>执行 GoFlexibleAsync 逻辑。</summary>
    [RelayCommand]
    private async Task GoFlexibleAsync(StockCheckOrderItem? item)
    {
        if (item is null) return;

        var query = new Dictionary<string, object?>
        {
            ["CheckNo"] = item.checkNo,
            ["CheckId"] = item.id,
            ["WarehouseName"] = item.warehouseName,  // 中文没问题
            ["AuditStatus"] = item.auditStatus
        };

        await Shell.Current.GoToAsync(nameof(FlexibleStockCheckPage), query);
    }

    #endregion
}

