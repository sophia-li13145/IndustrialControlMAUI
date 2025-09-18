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
        [ObservableProperty] private string? supplierName;
        [ObservableProperty] private string? createdTime;

        // 列表数据源
        public ObservableCollection<string> AvailableBins { get; } = new();
        public ObservableCollection<OutScannedItem> ScannedList { get; } = new();
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
            string instockId, string instockNo, string orderType, string orderTypeName,
            string purchaseNo, string supplierName, string createdTime)
        {
            // 1) 基础信息
            InstockId = instockId;
            InstockNo = instockNo;
            OrderType = orderType;
            OrderTypeName = orderTypeName;
            PurchaseNo = purchaseNo;
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

        private void SwitchTab(bool showPending)
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

        private async Task LoadScannedAsync()
        {
            ScannedList.Clear();
            if (string.IsNullOrWhiteSpace(InstockId)) return;

            var rows = await _api.GetInStockScanDetailAsync(InstockId!);
            foreach (var r in rows)
            {
                ScannedList.Add(new OutScannedItem
                {
                    IsSelected = false,
                    Barcode = r.Barcode ?? "",
                    Name = r.MaterialName ?? "",
                    Spec = r.Spec ?? "",
                    Location = string.IsNullOrWhiteSpace(r.Location) ? "请选择" : r.Location!,
                    Qty = r.Qty,
                    ScanStatus = r.ScanStatus,
                    WarehouseCode = r.WarehouseCode ?? "",
                    DetailId = r.DetailId,
                    Id = InstockId
                });

                if (!string.IsNullOrWhiteSpace(r.Location) && !AvailableBins.Contains(r.Location))
                    AvailableBins.Add(r.Location);
            }
        }

        // InboundMaterialViewModel.cs

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
        }

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
        }



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

            // 调用扫码入库接口
            var resp = await _api.InStockByBarcodeAsync(InstockId!, barcode);

            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "入库失败，请重试或检查条码。" : resp.Message!);
                return;
            }

            // 成功 → 刷新“待入库明细”和“已扫描明细”
            await LoadPendingAsync();
            await LoadScannedAsync();

            // UI 友好：尝试高亮刚扫的那一条
            var hit = ScannedList.FirstOrDefault(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            if (hit != null)
            {
                hit.IsSelected = true;
                SelectedScanItem = hit;
                // 切到“已扫描”页签更直观（可选）
                // SwitchTab(false);
            }
        }


        private Task ShowTip(string message) =>
            Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;


        public void ClearScan() => ScannedList.Clear();
        public void ClearAll()
        {
            PendingList.Clear();
            ScannedList.Clear();
        }

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

        // InboundMaterialViewModel.cs

        private Task<bool> AskAsync(string title, string message, string ok = "是", string cancel = "否") =>
            Shell.Current?.DisplayAlert(title, message, ok, cancel) ?? Task.FromResult(false);

        public async Task<bool> ConfirmInboundAsync()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法确认入库。请从查询页进入。");
                return false;
            }

            // ② 服务端权威校验：是否全部扫码确认，后端接口
            bool serverAllOk = await _api.JudgeInstockDetailScanAllAsync(InstockId!);

            // 任意一处不一致 → 提示是否继续
            if (!serverAllOk)
            {
                bool goOn = await AskAsync("提示", "已扫描列表与待入库数量不一致，是否继续入库？");
                if (!goOn) return false;
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
            if (hit != null) { hit.IsSelected = true; SelectedScanItem = hit; }

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
        [ObservableProperty] private string detailId;
        [ObservableProperty] private string id;
        [ObservableProperty] private bool scanStatus;
        [ObservableProperty] private string warehouseCode;
    }
}
