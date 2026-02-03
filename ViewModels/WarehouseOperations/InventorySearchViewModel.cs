using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 库存查询 VM：通过条码查询库存信息
    /// </summary>
    public partial class InventorySearchViewModel : ObservableObject
    {
        private readonly IWorkOrderApi _api;
        private readonly SemaphoreSlim _scanLock = new(1, 1);

        /// <summary>执行 InventorySearchViewModel 初始化逻辑。</summary>
        public InventorySearchViewModel(IWorkOrderApi api)
        {
            _api = api;
        }

        // ===== 输入 / 状态 =====

        [ObservableProperty]
        private string? scanCode;

        [ObservableProperty]
        private bool isBusy;          // 首次查询 Loading

        [ObservableProperty]
        private bool isLoadingMore;   // 上拉加载更多 Loading

        [ObservableProperty]
        private bool hasMore;         // 是否还有下一页

        private const int PageSize = 10;
        private int _pageNo = 0;      // 当前已加载到第几页
        private string? _currentBarcode; // 当前查询使用的条码

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<InventoryRecord> InventoryList { get; } = new();

        // ================== 命令：扫码提交 ==================

        /// <summary>执行 ScanSubmit 逻辑。</summary>
        [RelayCommand]
        private async Task ScanSubmit()
        {
            var code = ScanCode?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                await ShowTip("请输入或扫描条码。");
                return;
            }

            await QueryInventoryAsync(code);   // 查第一页 10 条
            ScanCode = string.Empty;
        }

        /// <summary>执行 Search 逻辑。</summary>
        [RelayCommand]
        private Task Search() => ScanSubmit();

        // ================== 对外查询接口 ==================

        /// <summary>
        /// 查询指定条码的库存（重置为第一页）
        /// </summary>
        public async Task QueryInventoryAsync(string barcode)
        {
            // 防止并发
            if (IsBusy) return;

            await LoadPageAsync(barcode, append: false);
        }

        /// <summary>
        /// 上拉加载更多命令，绑定给 CollectionView
        /// </summary>
        /// <summary>执行 LoadMoreAsync 逻辑。</summary>
        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if (IsLoadingMore || !HasMore) return;
            if (string.IsNullOrEmpty(_currentBarcode)) return;

            await LoadPageAsync(_currentBarcode, append: true);
        }

        // ================== 核心分页加载逻辑 ==================

        /// <summary>执行 LoadPageAsync 逻辑。</summary>
        private async Task LoadPageAsync(string barcode, bool append)
        {
            await _scanLock.WaitAsync();
            try
            {
                if (append)
                {
                    IsLoadingMore = true;
                }
                else
                {
                    IsBusy = true;
                    _pageNo = 0;         // 重新从第一页开始
                    HasMore = true;
                    _currentBarcode = barcode;
                }

                var nextPage = _pageNo + 1;

                var resp = await _api.PageInventoryAsync(
                    barcode: barcode,
                    pageNo: nextPage,
                    pageSize: PageSize,
                    searchCount: false);

                if (resp == null || resp.success != true || resp.result == null)
                {
                    if (!append) // 只在首次查询时提示错误
                    {
                        var msg = string.IsNullOrWhiteSpace(resp?.message)
                            ? "查询库存失败，请稍后重试。"
                            : resp!.message!;
                        await ShowTip(msg);
                    }
                    HasMore = false;
                    return;
                }

                var records = resp.result.records ?? new List<InventoryRecord>();

                if (!append)
                    InventoryList.Clear();

                // 连续编号
                var index = append ? InventoryList.Count + 1 : 1;
                foreach (var r in records)
                {
                    r.index = index++;
                    InventoryList.Add(r);
                }

                _pageNo = nextPage;
                HasMore = records.Count >= PageSize;

                if (!append && InventoryList.Count == 0)
                {
                    await ShowTip("未查询到对应的库存信息。");
                }
            }
            catch (Exception ex)
            {
                if (!append)
                    await ShowTip("查询异常：" + ex.Message);
                // 加载更多失败就不弹，避免打扰
            }
            finally
            {
                if (append)
                    IsLoadingMore = false;
                else
                    IsBusy = false;

                _scanLock.Release();
            }
        }

        // ================== 辅助方法 ==================

        /// <summary>执行 ShowTip 逻辑。</summary>
        private Task ShowTip(string msg) =>
            Shell.Current?.DisplayAlert("提示", msg, "确定") ?? Task.CompletedTask;
    }

}
