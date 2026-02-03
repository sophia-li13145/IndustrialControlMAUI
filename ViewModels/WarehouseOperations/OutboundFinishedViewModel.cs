using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutboundFinishedViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;
        private readonly IOutboundMaterialService _api;

        // === 基础信息（由搜索页带入） ===
        [ObservableProperty] private string? outstockId;
        [ObservableProperty] private string? outstockNo;
        [ObservableProperty] private string? deliveryNo;
        [ObservableProperty] private string? customer;
        [ObservableProperty] private string? expectedDeliveryTime;
        [ObservableProperty] private string? saleNo;
        [ObservableProperty] private string? deliveryMemo;

        // 串行化“扫码→接口→刷新”，避免同时两次刷新导致闪烁
        private readonly SemaphoreSlim _scanLock = new(1, 1);
        // 版本号用于避免乱序刷新（可选，后面增量刷新会用到）
        private int _scannedVersion = 0;

        // —— 防抖窗口（同码短时间内直接丢弃）
        private const int SameCodeWindowMs = 600;
        private string? _lastCode;
        private long _lastTick;

        // —— 同码并发“处理中”栅栏，避免同一条码并行进来
        private readonly HashSet<string> _inflight = new(StringComparer.OrdinalIgnoreCase);

        // —— UI 列表的本地互斥（占位查找+插入要原子化）
        private readonly object _listLock = new();



        // 列表数据源
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<string> AvailableBins { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OutScannedItem> ScannedList { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OutPendingItem> PendingList { get; } = new();

        [ObservableProperty] private OutScannedItem? selectedScanItem;

        // Tab 控制
        [ObservableProperty] private bool isPendingVisible = true;
        [ObservableProperty] private bool isScannedVisible = false;

        // Tab 颜色
        [ObservableProperty] private string pendingTabColor = "#E6F2FF";
        [ObservableProperty] private string scannedTabColor = "White";
        [ObservableProperty] private string pendingTextColor = "#007BFF";
        [ObservableProperty] private string scannedTextColor = "#333333";

        // 命令
        public IRelayCommand ShowPendingCommand { get; }
        public IRelayCommand ShowScannedCommand { get; }

        /// <summary>执行 OutboundFinishedViewModel 初始化逻辑。</summary>
        public OutboundFinishedViewModel(IOutboundMaterialService api)
        {
            _api = api;
            ShowPendingCommand = new RelayCommand(() => SwitchTab(true));
            ShowScannedCommand = new RelayCommand(() => SwitchTab(false));
        }

        // ================ 初始化入口（页面 OnAppearing 调用） ================
        public async Task InitializeFromSearchAsync(
            string outstockId, string outstockNo, string deliveryNo, string customer,
            string expectedDeliveryTime, string saleNo, string deliveryMemo)
        {
            // 1) 基础信息
            OutstockId = outstockId;
            OutstockNo = outstockNo;
            DeliveryNo = deliveryNo;
            Customer = customer;
            ExpectedDeliveryTime = expectedDeliveryTime;
            SaleNo = saleNo;
            DeliveryMemo = deliveryMemo;

            // 2) 下拉库位（如无接口可留空或使用后端返回的 location 聚合）
            AvailableBins.Clear();

            // 3) 拉取两张表
            await LoadPendingAsync();
            await LoadScannedAsync();

            // 默认显示“待入库明细”
            SwitchTab(true);
        }

        /// <summary>执行 SwitchTab 逻辑。</summary>
        public void SwitchTab(bool showPending)
        {
            IsPendingVisible = showPending;
            IsScannedVisible = !showPending;
            if (showPending)
            {
                PendingTabColor = "#E6F2FF"; ScannedTabColor = "White";
                PendingTextColor = "#007BFF"; ScannedTextColor = "#333333";
            }
            else
            {
                PendingTabColor = "White"; ScannedTabColor = "#E6F2FF";
                PendingTextColor = "#333333"; ScannedTextColor = "#007BFF";
            }
        }

        /// <summary>执行 LoadPendingAsync 逻辑。</summary>
        private async Task LoadPendingAsync()
        {
            PendingList.Clear();
            if (string.IsNullOrWhiteSpace(OutstockId)) return;

            var rows = await _api.GetOutStockDetailAsync(OutstockId!);
            foreach (var r in rows)
            {
                PendingList.Add(new OutPendingItem
                {
                    Name = r.MaterialName ?? "",
                    MaterialCode = r.MaterialCode ?? "",
                    Spec = r.Spec ?? "",
                    Location = r.Location ?? "",
                    ProductionBatch = r.ProductionBatch ?? "",
                    StockBatch = r.StockBatch ?? "",
                    OutstockQty = r.OutstockQty,
                    Qty = r.Qty
                });


                // 聚合可选库位
                if (!string.IsNullOrWhiteSpace(r.Location) && !AvailableBins.Contains(r.Location))
                    AvailableBins.Add(r.Location);
            }
        }


        // 首次加载
        /// <summary>执行 LoadScannedAsync 逻辑。</summary>
        private async Task LoadScannedAsync(int versionGuard = 0)
        {
            if (string.IsNullOrWhiteSpace(OutstockId))
            {
                ScannedList.Clear();
                return;
            }

            var rows = await _api.GetOutStockScanDetailAsync(OutstockId!);
            if (versionGuard != 0 && versionGuard != Volatile.Read(ref _scannedVersion)) return;

            if (rows is null || rows.Count == 0)
            {
                ScannedList.Clear();
                return;
            }

            var grouped = rows
                .GroupBy(r => (r.Barcode ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    var qty = g.Sum(x => x.Qty);
                    var loc = g.Select(x => (x.Location ?? "").Trim()).LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "请选择";
                    var wh = g.Select(x => (x.WarehouseCode ?? "").Trim()).LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";
                    var pass = g.Any(x => x.ScanStatus);

                    return new OutScannedItem
                    {
                        Barcode = (first.Barcode ?? "").Trim(),
                        Name = first.MaterialName ?? "",
                        Spec = first.Spec ?? "",
                        Qty = qty,
                        Location = string.IsNullOrWhiteSpace(loc) ? "请选择" : loc,
                        WarehouseCode = wh,
                        ScanStatus = pass,
                        DetailId = first.DetailId,
                        Id = OutstockId,
                        IsSelected = false
                    };
                })
                .ToList();

            ApplyScannedDiff(grouped);

            foreach (var it in grouped)
                if (!string.IsNullOrWhiteSpace(it.Location) && it.Location != "请选择" && !AvailableBins.Contains(it.Location))
                    AvailableBins.Add(it.Location);
        }

        // Diff 版：后续刷新一律用它
        /// <summary>执行 LoadScannedAsyncDiff 逻辑。</summary>
        private async Task LoadScannedAsyncDiff(int versionGuard = 0)
        {
            if (string.IsNullOrWhiteSpace(OutstockId))
                return;

            var rows = await _api.GetOutStockScanDetailAsync(OutstockId!);
            if (versionGuard != 0 && versionGuard != Volatile.Read(ref _scannedVersion)) return;

            if (rows is null || rows.Count == 0) return;

            var grouped = rows
                .GroupBy(r => (r.Barcode ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    var qty = g.Sum(x => x.Qty);
                    var loc = g.Select(x => (x.Location ?? "").Trim()).LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "请选择";
                    var wh = g.Select(x => (x.WarehouseCode ?? "").Trim()).LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";
                    var pass = g.Any(x => x.ScanStatus);

                    return new OutScannedItem
                    {
                        Barcode = (first.Barcode ?? "").Trim(),
                        Name = first.MaterialName ?? "",
                        Spec = first.Spec ?? "",
                        Qty = qty,
                        Location = string.IsNullOrWhiteSpace(loc) ? "请选择" : loc,
                        WarehouseCode = wh,
                        ScanStatus = pass,
                        DetailId = first.DetailId,
                        Id = OutstockId,
                        IsSelected = false
                    };
                })
                .ToList();

            ApplyScannedDiff(grouped);

            foreach (var it in grouped)
                if (!string.IsNullOrWhiteSpace(it.Location) && it.Location != "请选择" && !AvailableBins.Contains(it.Location))
                    AvailableBins.Add(it.Location);
        }


        // === 新增：本地去重合并，仅按条码维度保持“一条” ===
        /// <summary>执行 UpsertScannedLocalThreadSafe 逻辑。</summary>
        private void UpsertScannedLocalThreadSafe(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return;

            lock (_listLock)
            {
                var exist = ScannedList.FirstOrDefault(x =>
                    string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
                if (exist != null)
                {
                    exist.IsSelected = true;
                    SelectedScanItem = exist;
                    return;
                }

                ScannedList.Add(new OutScannedItem
                {
                    IsSelected = true,
                    Barcode = barcode,
                    Name = "",
                    Spec = "",
                    Location = "请选择",
                    Qty = 0,
                    ScanStatus = true,    // 预期态，待服务端校正
                    WarehouseCode = "",
                    DetailId = "",
                    Id = OutstockId
                });
                SelectedScanItem = ScannedList[^1];
            }
        }

        // 根据后端返回行（已经按条码聚合好的 OutScannedItem 列表）做差量应用
        /// <summary>执行 ApplyScannedDiff 逻辑。</summary>
        private void ApplyScannedDiff(List<OutScannedItem> newItems)
        {
            // 旧数据索引
            var oldMap = ScannedList.ToDictionary(x => x.Barcode ?? "", StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 更新/新增 —— 保持“稳定顺序”（不全量清空，不重排）
            foreach (var it in newItems)
            {
                var key = it.Barcode ?? "";
                if (oldMap.TryGetValue(key, out var exist))
                {
                    // 只更新字段，避免替换对象导致“删除→新增”的闪动
                    if (!string.Equals(exist.Name, it.Name, StringComparison.Ordinal)) exist.Name = it.Name;
                    if (!string.Equals(exist.Spec, it.Spec, StringComparison.Ordinal)) exist.Spec = it.Spec;
                    if (exist.Qty != it.Qty) exist.Qty = it.Qty;
                    if (!string.Equals(exist.Location, it.Location, StringComparison.Ordinal)) exist.Location = it.Location;
                    if (!string.Equals(exist.WarehouseCode, it.WarehouseCode, StringComparison.Ordinal)) exist.WarehouseCode = it.WarehouseCode;
                    if (exist.ScanStatus != it.ScanStatus) exist.ScanStatus = it.ScanStatus;
                    if (exist.DetailId != it.DetailId) exist.DetailId = it.DetailId;
                    if (exist.Id != it.Id) exist.Id = it.Id;
                }
                else
                {
                    // 新条目：稳定插入在末尾（不排序，避免 UI 重排）
                    ScannedList.Add(it);
                }

                if (!string.IsNullOrWhiteSpace(it.Location) && it.Location != "请选择" && !AvailableBins.Contains(it.Location))
                    AvailableBins.Add(it.Location);

                seen.Add(key);
            }

            // 删除新结果里不存在的旧项（倒序移除）
            for (int i = ScannedList.Count - 1; i >= 0; i--)
            {
                var bc = ScannedList[i].Barcode ?? "";
                if (!seen.Contains(bc))
                    ScannedList.RemoveAt(i);
            }
        }

        


        /// <summary>执行 PassScan 逻辑。</summary>
        [RelayCommand]
        private async Task PassScan()
        {
            var picks = ScannedList.Where(x => x.IsSelected).ToList();
            if (picks.Count == 0) { await ShowTip("请先勾选至少一条已扫描记录。"); return; }

            // 组装 [{ barcode, id }]
            var items = picks.Select(x => (barcode: x.Barcode, id: x.Id)).ToList();
            var resp = await _api.ScanConfirmAsync(items);
            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "扫描通过失败，请重试。" : resp.Message!);
                return;
            }

            await LoadPendingAsync();
            await LoadScannedAsyncDiff();
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

            await LoadPendingAsync();
            await LoadScannedAsyncDiff();
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
            if (string.IsNullOrWhiteSpace(OutstockId))
            {
                await ShowTip("缺少 OutstockId，无法出库。请从查询页进入。");
                return;
            }

            // ① 同码防抖
            var now = Environment.TickCount64;
            if (!string.IsNullOrEmpty(_lastCode)
                && string.Equals(_lastCode, barcode, StringComparison.OrdinalIgnoreCase)
                && now - _lastTick < SameCodeWindowMs)
            {
                return;
            }
            _lastCode = barcode;
            _lastTick = now;

            // ② 并发栅栏
            if (!_inflight.Add(barcode)) return;

            try
            {
                // ③ 线程安全占位
                lock (_listLock)
                {
                    var exist = ScannedList.FirstOrDefault(x =>
                        string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));

                    if (exist is null)
                    {
                        var placeholder = new OutScannedItem
                        {
                            Barcode = barcode,
                            Name = "",
                            Spec = "",
                            Qty = 0,
                            Location = "请选择",
                            WarehouseCode = "",
                            ScanStatus = true,
                            DetailId = "",
                            Id = OutstockId ?? "",
                            IsSelected = true
                        };
                        ScannedList.Add(placeholder);
                        SelectedScanItem = placeholder;
                    }
                    else
                    {
                        exist.IsSelected = true;
                        SelectedScanItem = exist;
                    }
                }

                // ④ 串行化：接口 → 刷新（差量）
                await _scanLock.WaitAsync();
                try
                {
                    // TODO: 替换为你的出库接口
                    var resp = await _api.OutStockByBarcodeAsync(OutstockId!, barcode);
                    if (!resp.Succeeded)
                    {
                        await ShowTip(string.IsNullOrWhiteSpace(resp.Message)
                            ? "出库失败，请重试或检查条码。"
                            : resp.Message!);
                        return;
                    }

                    var ver = Interlocked.Increment(ref _scannedVersion);

                    await LoadPendingAsync();
                    await LoadScannedAsyncDiff(ver);

                    var hit = ScannedList.FirstOrDefault(x =>
                        string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
                    if (hit != null)
                    {
                        hit.IsSelected = true;
                        SelectedScanItem = hit;
                        SwitchTab(false);
                    }
                }
                finally
                {
                    _scanLock.Release();
                }
            }
            finally
            {
                _inflight.Remove(barcode);
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
            PendingList.Clear();
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

        // OutboundFinishedViewModel.cs

        /// <summary>执行 AskAsync 逻辑。</summary>
        private Task<bool> AskAsync(string title, string message, string ok = "是", string cancel = "否") =>
            Shell.Current?.DisplayAlert(title, message, ok, cancel) ?? Task.FromResult(false);

        /// <summary>执行 ConfirmOutboundAsync 逻辑。</summary>
        public async Task<bool> ConfirmOutboundAsync()
        {
            if (string.IsNullOrWhiteSpace(OutstockId))
            {
                await ShowTip("缺少 OutstockId，无法确认入库。请从查询页进入。");
                return false;
            }

            // ② 服务端权威校验：是否全部扫码确认，后端接口
            bool serverAllOk = await _api.JudgeOutstockDetailScanAllAsync(OutstockId!);

            //不一致，提示并不入库
            if (!serverAllOk)
            {
                // 直接提示，不再让用户选择
                await ShowTip("已扫描列表与待入库数量不一致，无法继续入库。");
                return false;   // 直接结束方法
            }

            // ③ 调用确认入库接口
            var r = await _api.ConfirmOutstockAsync(OutstockId!);
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

            // 通过反射从行对象里取必要字段（兼容不同命名）
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

            // 组织参数
            var outstockWarehouse = bin.WarehouseName ?? "";
            var outstockWarehouseCode = bin.WarehouseCode ?? "";
            var location = bin.Location ?? "";

            // 调用接口
            var ok = await _api.UpdateOutstockLocationAsync(
                detailId, id, outstockWarehouse, outstockWarehouseCode, location, ct);

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

            // ✅ 成功：先提示，再刷新两张表
            await ShowTip("数量修改成功");

            // 记录一下当前行用于刷新后恢复选中
            var keepBarcode = row.Barcode;

            await LoadPendingAsync();   // 刷新“待入库明细”
            await LoadScannedAsyncDiff();            // 刷新“已扫描明细”

            var hit = ScannedList.FirstOrDefault(x => string.Equals(x.Barcode, keepBarcode, StringComparison.OrdinalIgnoreCase));
            if (hit != null) { hit.IsSelected = true; SelectedScanItem = hit; }

            return true;
        }


    }


}
