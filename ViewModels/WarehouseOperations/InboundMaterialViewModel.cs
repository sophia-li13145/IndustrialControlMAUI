using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using Serilog;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class InboundMaterialViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;
        private readonly IInboundMaterialService _api;

        // === 基础信息（由搜索页带入） ===
        [ObservableProperty] private string? instockId;
        [ObservableProperty] private string? instockNo;
        [ObservableProperty] private string? orderType;
        [ObservableProperty] private string? orderTypeName;
        [ObservableProperty] private string? purchaseNo;
        [ObservableProperty] private string? arrivalNo;
        [ObservableProperty] private string? supplierName;
        [ObservableProperty] private string? createdTime;
        private readonly SemaphoreSlim _scanLock = new(1, 1);
        private int _scannedVersion;

        // 列表数据源
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<string> AvailableBins { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OutScannedItem> ScannedList { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<PendingItem> PendingList { get; } = new();

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
        public IAsyncRelayCommand ConfirmCommand { get; }

        /// <summary>执行 InboundMaterialViewModel 初始化逻辑。</summary>
        public InboundMaterialViewModel(IInboundMaterialService warehouseSvc)
        {
            Log.Information("MaterialPage");
            _api = warehouseSvc;
            ShowPendingCommand = new RelayCommand(() => SwitchTab(true));
            ShowScannedCommand = new RelayCommand(() => SwitchTab(false));
            //ConfirmCommand = new AsyncRelayCommand(ConfirmInboundAsync);
        }

        // ================ 初始化入口（页面 OnAppearing 调用） ================
        public async Task InitializeFromSearchAsync(
            string instockId, string instockNo, string orderType, string orderTypeName,string purchaseNo,
            string arrivalNo, string supplierName, string createdTime)
        {
            // 1) 基础信息
            InstockId = instockId;
            InstockNo = instockNo;
            OrderType = orderType;
            OrderTypeName = orderTypeName;
            PurchaseNo = purchaseNo;
            ArrivalNo = arrivalNo;
            SupplierName = supplierName;
            CreatedTime = createdTime;

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
            if (string.IsNullOrWhiteSpace(InstockId)) return;

            var rows = await _api.GetInStockDetailAsync(InstockId!);
            foreach (var r in rows)
            {
                PendingList.Add(new PendingItem
                {
                    Name = r.MaterialName ?? "",
                    Spec = r.Spec ?? "",
                    PendingQty = r.PendingQty,
                    Bin = string.IsNullOrWhiteSpace(r.Location) ? "请选择" : r.Location!,
                    ScannedQty = r.ScannedQty
                });

                // 聚合可选库位
                if (!string.IsNullOrWhiteSpace(r.Location) && !AvailableBins.Contains(r.Location))
                    AvailableBins.Add(r.Location);
            }
        }

        // versionGuard 保留你之前的版本守卫参数
        /// <summary>执行 LoadScannedAsync 逻辑。</summary>
        private async Task LoadScannedAsync(int versionGuard = 0)
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                ScannedList.Clear();
                return;
            }

            var rows = await _api.GetInStockScanDetailAsync(InstockId!);
            if (versionGuard != 0 && versionGuard != Volatile.Read(ref _scannedVersion)) return;

            // 接口无数据 => 清空并返回
            if (rows is null || rows.Count == 0)
            {
                ScannedList.Clear();
                return;
            }

            // 先在内存里聚合（注意库位/仓库取“最后一条非空”）
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
                        Id = InstockId,
                        IsSelected = false
                    };
                })
                .ToList();

            // 关键：增量应用，避免闪现
            ApplyScannedDiff(grouped);
        }

        // === 新增：本地去重合并，仅按条码维度保持“一条” ===
        /// <summary>执行 UpsertScannedLocal 逻辑。</summary>
        private void UpsertScannedLocal(string barcode)
        {
            // 只要列表里已经有这条码 → 不新增、不替换，只保持选中
            var exist = ScannedList.FirstOrDefault(x =>
                string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            if (exist != null)
            {
                // 这里不强行改数量，避免和服务端不一致
                exist.IsSelected = true;
                SelectedScanItem = exist;
                SwitchTab(false);
                return;
            }

            // 本地还没有：先用一个占位对象“乐观加入”一条（不设置数量/库位等）
            var placeholder = new OutScannedItem
            {
                Barcode = barcode,
                Name = "",          // 等服务端回来再填
                Spec = "",
                Qty = 0,            // 等服务端回来再填
                Location = "请选择",
                WarehouseCode = "",
                ScanStatus = true,  // 已扫通过的预期态，等刷新校正
                DetailId = "",
                Id = InstockId ?? "",
                IsSelected = true
            };
            ScannedList.Add(placeholder);
            SelectedScanItem = placeholder;
            SwitchTab(false);
        }


        /// <summary>执行 ApplyScannedDiff 逻辑。</summary>
        private void ApplyScannedDiff(List<OutScannedItem> newItems)
        {
            // 旧数据映射：按条码快速查找
            var oldMap = ScannedList.ToDictionary(x => x.Barcode ?? "", StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 先更新 / 新增（保持原有顺序，不重排）
            foreach (var it in newItems)
            {
                var key = it.Barcode ?? "";
                if (oldMap.TryGetValue(key, out var exist))
                {
                    if (exist.Qty != it.Qty) exist.Qty = it.Qty;
                    if (!string.Equals(exist.Location, it.Location, StringComparison.Ordinal))
                        exist.Location = it.Location;
                    if (!string.Equals(exist.WarehouseCode, it.WarehouseCode, StringComparison.Ordinal))
                        exist.WarehouseCode = it.WarehouseCode;
                    if (exist.ScanStatus != it.ScanStatus) exist.ScanStatus = it.ScanStatus;

                    // 保持当前选中不丢
                    if (exist.IsSelected) SelectedScanItem = exist;
                }
                else
                {
                    // 新条目：追加在末尾（稳定插入，不触发重排）
                    ScannedList.Add(it);
                }

                // 下拉库位聚合
                if (!string.IsNullOrWhiteSpace(it.Location) && it.Location != "请选择" && !AvailableBins.Contains(it.Location))
                    AvailableBins.Add(it.Location);

                seen.Add(key);
            }

            // 删除“新结果里不存在的旧项”
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
            await LoadScannedAsync();
            await ShowTip("已确认通过。");
            SwitchTab(false);
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
            await LoadScannedAsync();
            await ShowTip("已取消扫描。");
            SwitchTab(false);
        }

        /// <summary>执行 HandleScannedAsync 逻辑。</summary>
        public async Task HandleScannedAsync(string data, string symbology)
        {
            var barcode = (data ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode)) { await ShowTip("无效条码。"); return; }
            if (string.IsNullOrWhiteSpace(InstockId)) { await ShowTip("缺少 InstockId，无法入库。请从查询页进入。"); return; }

            // ✅ 关键：先本地“只保留一条”以避免 UI 出现两条
            UpsertScannedLocal(barcode);

            await _scanLock.WaitAsync(); // 串行化一次完整“扫码→刷新”
            try
            {
                var resp = await _api.InStockByBarcodeAsync(InstockId!, barcode);
                if (!resp.Succeeded)
                {
                    await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "入库失败，请重试或检查条码。" : resp.Message!);
                    return;
                }

                // 每次刷新前 bump 版本
                var ver = Interlocked.Increment(ref _scannedVersion);

                // 待入库刷新
                await LoadPendingAsync();

                // 带版本的已扫描刷新（里面是差量 Apply，不会清表重绑）
                await LoadScannedAsync(ver);

                // 选中刚才那条
                var hit = ScannedList.FirstOrDefault(x =>
                    string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
                if (hit != null) { hit.IsSelected = true; SelectedScanItem = hit; SwitchTab(false); }
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



        /// <summary>执行 ConfirmInboundAsync 逻辑。</summary>
        public async Task<bool> ConfirmInboundAsync()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法确认入库。请从查询页进入。");
                return false;
            }

            // ② 服务端权威校验：是否全部扫码确认，后端接口
            bool serverAllOk = await _api.JudgeInstockDetailScanAllAsync(InstockId!);
            //不一致，提示并不入库
            if (!serverAllOk)
            {
                // 直接提示，不再让用户选择
                await ShowTip("已扫描列表与待入库数量不一致，无法继续入库。");
                return false;   // 直接结束方法
            }

            // ③ 调用确认入库接口
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
            var instockWarehouse = bin.WarehouseName ?? "";
            var instockWarehouseCode = bin.WarehouseCode ?? "";
            var location = bin.Location ?? "";

            // 调用接口
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

            // ✅ 成功：先提示，再刷新两张表
            await ShowTip("数量修改成功");

            // 记录一下当前行用于刷新后恢复选中
            var keepBarcode = row.Barcode;

            await LoadPendingAsync();   // 刷新“待入库明细”
            await LoadScannedAsync();   // 刷新“已扫描明细”

            var hit = ScannedList.FirstOrDefault(x => string.Equals(x.Barcode, keepBarcode, StringComparison.OrdinalIgnoreCase));
            if (hit != null) { hit.IsSelected = true; SelectedScanItem = hit; SwitchTab(false); }

            return true;
        }


    }

    // === 列表行模型 ===
    public class PendingItem
    {
        public string Name { get; set; } = ""; //产品名称
        public string MaterialCode { get; set; } = ""; //产品编码
        public string Spec { get; set; } = "";//规格
        public string Location { get; set; } = "";//出库库位
        public string ProductionBatch { get; set; } = "";//生产批号
        public int PendingQty { get; set; }
        public string Bin { get; set; } = "请选择";
        public int ScannedQty { get; set; }
    }

    public partial class OutScannedItem : ObservableObject
    {
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private string barcode = "";
        [ObservableProperty] private string name = "";
        [ObservableProperty] private string spec = "";
        [ObservableProperty] private string location = "请选择";
        [ObservableProperty] private int qty;
        [ObservableProperty] private int outstockQty;
        [ObservableProperty] private string detailId;
        [ObservableProperty] private string id;
        [ObservableProperty] private bool scanStatus;
        [ObservableProperty] private string warehouseCode;
    }
}
