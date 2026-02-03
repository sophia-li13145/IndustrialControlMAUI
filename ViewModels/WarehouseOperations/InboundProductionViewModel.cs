using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class InboundProductionViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;
        private readonly IInboundMaterialService _api;

        // === 基础信息（由搜索页带入） ===
        [ObservableProperty] private string? instockId;
        [ObservableProperty] private string? instockNo;
        [ObservableProperty] private string? orderType;
        [ObservableProperty] private string? orderTypeName;
        [ObservableProperty] private string? purchaseNo;
        [ObservableProperty] private string? supplierName;
        [ObservableProperty] private string? workOrderNo;
        [ObservableProperty] private string? materialName;
        [ObservableProperty] private int instockQty;
        [ObservableProperty] private string? createdTime;

        // 列表数据源
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<string> AvailableBins { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OutScannedItem> ScannedList { get; } = new();

        [ObservableProperty] private OutScannedItem? selectedScanItem;

        // Tab 颜色
        [ObservableProperty] private string pendingTabColor = "#E6F2FF";
        [ObservableProperty] private string scannedTabColor = "White";
        [ObservableProperty] private string pendingTextColor = "#007BFF";
        [ObservableProperty] private string scannedTextColor = "#333333";

        // ✅ 串行 & 版本控制，避免相机快速扫时并发刷新导致重复
        private readonly SemaphoreSlim _scanLock = new(1, 1);
        private int _scannedVersion;

        /// <summary>执行 InboundProductionViewModel 初始化逻辑。</summary>
        public InboundProductionViewModel(IInboundMaterialService warehouseSvc)
        {
            _api = warehouseSvc;
        }

        // ================ 初始化入口（页面 OnAppearing 调用） ================
        public async Task InitializeFromSearchAsync(
            string instockId, string instockNo, string orderType, string orderTypeName,
            string purchaseNo, string supplierName, string createdTime, string workOrderNo, string materialName, int instockQty)
        {
            // 1) 基础信息
            InstockId = instockId;
            InstockNo = instockNo;
            OrderType = orderType;
            OrderTypeName = orderTypeName;
            PurchaseNo = purchaseNo;
            SupplierName = supplierName;
            CreatedTime = createdTime;
            WorkOrderNo = workOrderNo;
            MaterialName = materialName;
            InstockQty = instockQty;

            // 2) 下拉库位
            AvailableBins.Clear();

            // 3) 拉取扫描表（初始化场景不做版本守卫）
            await LoadScannedAsync();
        }

        // ✅ 带版本的已扫描刷新：versionGuard=0 表示无守卫（初始化时）
        /// <summary>执行 LoadScannedAsync 逻辑。</summary>
        private async Task LoadScannedAsync(int versionGuard = 0)
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                ScannedList.Clear();
                return;
            }

            var rows = await _api.GetInStockScanDetailAsync(InstockId!);
            if (rows is null || rows.Count == 0)
            {
                // 空数据也要遵守版本；只有匹配版本才清空
                if (versionGuard == 0 || versionGuard == Volatile.Read(ref _scannedVersion))
                    ScannedList.Clear();
                return;
            }

            // 按条码聚合（忽略大小写）
            var grouped = rows
            .GroupBy(r => (
                DetailId: (r.DetailId ?? string.Empty).Trim(),
                Barcode: (r.Barcode ?? string.Empty).Trim()
            ))
            .Select(g =>
            {
                var first = g.First();
                var totalQty = g.Sum(x => x.Qty);

                // 仍然取“最后一条非空”库位/仓库，避免旧值覆盖
                var loc = g.Select(x => (x.Location ?? "").Trim())
                           .LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "请选择";
                var wh = g.Select(x => (x.WarehouseCode ?? "").Trim())
                           .LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";

                var status = g.Any(x => x.ScanStatus);

                return new OutScannedItem
                {
                    IsSelected = false,
                    Barcode = g.Key.Barcode,
                    DetailId = g.Key.DetailId,   // ✅ 关键：一行 = 一个明细
                    Id = InstockId,              // 还是这张单的 InstockId
                    Name = first.MaterialName ?? "",
                    Spec = first.Spec ?? "",
                    Qty = totalQty,
                    Location = string.IsNullOrWhiteSpace(loc) ? "请选择" : loc,
                    WarehouseCode = wh,
                    ScanStatus = status
                };
            })
            .ToList();

            // ✅ 版本守卫：过期结果不渲染
            if (versionGuard != 0 && versionGuard != Volatile.Read(ref _scannedVersion))
                return;

            // ✅ 渲染：先清后加，避免累积
            ScannedList.Clear();
            foreach (var it in grouped)
            {
                ScannedList.Add(it);
                if (!string.IsNullOrWhiteSpace(it.Location) && it.Location != "请选择" && !AvailableBins.Contains(it.Location))
                    AvailableBins.Add(it.Location);
            }
        }

        /// <summary>执行 PassScan 逻辑。</summary>
        [RelayCommand]
        private async Task PassScan()
        {
            var picks = ScannedList.Where(x => x.IsSelected).ToList();
            if (picks.Count == 0) { await ShowTip("请先勾选至少一条已扫描记录。"); return; }

            var items = picks.Select(x => (barcode: x.Barcode, id: x.Id)).ToList();
            var resp = await _api.ScanConfirmAsync(items);
            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "扫描通过失败，请重试。" : resp.Message!);
                return;
            }
            var ver = Interlocked.Increment(ref _scannedVersion);
            await LoadScannedAsync(ver);
            await ShowTip("已确认通过。");
        }

        /// <summary>执行 CancelScan 逻辑。</summary>
        [RelayCommand]
        private async Task CancelScan()
        {
            var picks = ScannedList.Where(x => x.IsSelected).ToList();
            if (picks.Count == 0) { await ShowTip("请先勾选至少一条记录。"); return; }

            var items = picks.Select(x => (barcode: x.Barcode, id: x.Id)).ToList();
            var resp = await _api.CancelScanAsync(items);
            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "取消扫描失败，请重试。" : resp.Message!);
                return;
            }

            var ver = Interlocked.Increment(ref _scannedVersion);
            await LoadScannedAsync(ver);
            await ShowTip("已取消扫描。");
        }

        /// <summary>执行 HandleScannedAsync 逻辑。</summary>
        public async Task HandleScannedAsync(string data, string symbology)
        {
            var barcode = (data ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                await ShowTip("无效条码。");
                return;
            }

            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法入库。请从查询页进入。");
                return;
            }

            await _scanLock.WaitAsync(); // ✅ 串行化完整流程
            try
            {
                var resp = await _api.InStockByBarcodeAsync(InstockId!, barcode);
                if (!resp.Succeeded)
                {
                    await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "入库失败，请重试或检查条码。" : resp.Message!);
                    return;
                }

                // bump 版本并刷新
                var ver = Interlocked.Increment(ref _scannedVersion);
                await LoadScannedAsync(ver);

                var hit = ScannedList.FirstOrDefault(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                {
                    hit.IsSelected = true;
                    SelectedScanItem = hit;
                }
            }
            finally
            {
                _scanLock.Release();
            }
        }

        /// <summary>执行 ShowTip 逻辑。</summary>
        private Task ShowTip(string message) =>
            Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

        /// <summary>执行 ClearScan 逻辑。</summary>
        public void ClearScan() => ScannedList.Clear();

        /// <summary>执行 ClearAll 逻辑。</summary>
        public void ClearAll()
        {
            ScannedList.Clear();
        }

        /// <summary>执行 SetItemBin 逻辑。</summary>
        public void SetItemBin(object row, string bin)
        {
            if (row is OutScannedItem item) { item.Location = bin; return; }
            var barcode = row?.GetType().GetProperty("Barcode")?.GetValue(row)?.ToString();
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var target = ScannedList.FirstOrDefault(x => x.Barcode == barcode);
                if (target != null) target.Location = bin;
            }
        }

        /// <summary>执行 AskAsync 逻辑。</summary>
        private Task<bool> AskAsync(string title, string message, string ok = "是", string cancel = "否") =>
            Shell.Current?.DisplayAlert(title, message, ok, cancel) ?? Task.FromResult(false);

        /// <summary>执行 ConfirmInboundAsync 逻辑。</summary>
        public async Task<bool> ConfirmInboundAsync()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法确认入库。请从查询页进入。");
                return false;
            }

            bool serverAllOk = await _api.JudgeInstockDetailScanAllAsync(InstockId!);
            if (!serverAllOk)
            {
                await ShowTip("已扫描列表与待入库数量不一致，无法继续入库。");
                return false;
            }

            var r = await _api.ConfirmInstockAsync(InstockId!);
            if (!r.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(r.Message) ? "确认入库失败，请重试。" : r.Message!);
                return false;
            }

            return true;
        }

        /// <summary>执行 UpdateRowLocationAsync 逻辑。</summary>
        public async Task<bool> UpdateRowLocationAsync(object row, BinInfo bin, CancellationToken ct = default)
        {
            if (row is null || bin is null) return false;

            var t = row.GetType();
            string detailId =
                t.GetProperty("DetailId")?.GetValue(row)?.ToString()
                ?? t.GetProperty("detailId")?.GetValue(row)?.ToString()
                ?? string.Empty;

            string id =
                t.GetProperty("Id")?.GetValue(row)?.ToString()
                ?? t.GetProperty("id")?.GetValue(row)?.ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(detailId) || string.IsNullOrWhiteSpace(id))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Application.Current.MainPage.DisplayAlert("提示", "缺少必要字段：detailId 或 id。", "确定"));
                return false;
            }

            var instockWarehouse = bin.WarehouseName ?? "";
            var instockWarehouseCode = bin.WarehouseCode ?? "";
            var location = bin.Location ?? "";

            var ok = await _api.UpdateInstockLocationAsync(
                detailId, id, instockWarehouse, instockWarehouseCode, location, ct);

            if (!ok.Succeeded)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Application.Current.MainPage.DisplayAlert("提示", ok.Message ?? "更新库位失败", "确定"));
                return false;
            }

            return true;
        }

        /// <summary>执行 UpdateQuantityForRowAsync 逻辑。</summary>
        public async Task<bool> UpdateQuantityForRowAsync(OutScannedItem row, CancellationToken ct = default)
        {
            if (row is null) return false;
            if (!row.ScanStatus)
            {
                await ShowTip("该行尚未扫描通过，不能修改数量。");
                return false;
            }
            if (row.Qty < 0)
            {
                await ShowTip("数量不能为负数。");
                return false;
            }

            var resp = await _api.UpdateQuantityAsync(row.Barcode, row.DetailId, row.Id, row.Qty, ct);
            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "更新数量失败" : resp.Message!);
                return false;
            }

            await ShowTip("数量修改成功");

            var keepBarcode = row.Barcode;
            var ver = Interlocked.Increment(ref _scannedVersion);
            await LoadScannedAsync(ver);

            var hit = ScannedList.FirstOrDefault(x => string.Equals(x.Barcode, keepBarcode, StringComparison.OrdinalIgnoreCase));
            if (hit != null) { hit.IsSelected = true; SelectedScanItem = hit; }

            return true;
        }
    }
}
