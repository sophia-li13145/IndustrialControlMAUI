using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutboundMaterialViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;
        private readonly IOutboundMaterialService _api;

        // === 基础信息（由搜索页带入） ===
        [ObservableProperty] private string? outstockId;
        [ObservableProperty] private string? outstockNo;
        [ObservableProperty] private string? requisitionMaterialNo;
        [ObservableProperty] private string? workOrderNo;
        [ObservableProperty] private string? memo;

        // 列表数据源
        public ObservableCollection<string> AvailableBins { get; } = new();
        public ObservableCollection<OutScannedItem> ScannedList { get; } = new();
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

        public OutboundMaterialViewModel(IOutboundMaterialService api)
        {
            _api = api;
            ShowPendingCommand = new RelayCommand(() => SwitchTab(true));
            ShowScannedCommand = new RelayCommand(() => SwitchTab(false));
        }

        // ================ 初始化入口（页面 OnAppearing 调用） ================
        public async Task InitializeFromSearchAsync(
            string outstockId, string outstockNo,
            string requisitionMaterialNo, string workOrderNo, string memo)
        {

            // 1) 基础信息
            OutstockId = outstockId;
            OutstockNo = outstockNo;
            RequisitionMaterialNo = requisitionMaterialNo;
            WorkOrderNo = workOrderNo;
            Memo = memo;


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
            if (string.IsNullOrWhiteSpace(OutstockId)) return;

            var rows = await _api.GetOutStockDetailAsync(OutstockId!);
            foreach (var r in rows)
            {
                PendingList.Add(new OutPendingItem
                {
                    Name = r.MaterialName ?? "",
                    MaterialCode = r.MaterialCode ?? "",
                    Spec = r.Spec ?? "",
                    Location = r.Location,
                    ProductionBatch = r.ProductionBatch,
                    StockBatch = r.StockBatch,
                    OutstockQty = r.OutstockQty,
                    Qty = r.Qty
                });

                // 聚合可选库位
                if (!string.IsNullOrWhiteSpace(r.Location) && !AvailableBins.Contains(r.Location))
                    AvailableBins.Add(r.Location);
            }
        }

        private async Task LoadScannedAsync()
        {
            ScannedList.Clear();
            if (string.IsNullOrWhiteSpace(OutstockId)) return;

            var rows = await _api.GetOutStockScanDetailAsync(OutstockId!);
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
                    Id = OutstockId
                });

                if (!string.IsNullOrWhiteSpace(r.Location) && !AvailableBins.Contains(r.Location))
                    AvailableBins.Add(r.Location);
            }
        }

        // OutboundMaterialViewModel.cs

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

            if (string.IsNullOrWhiteSpace(OutstockId))
            {
                await ShowTip("缺少 OutstockId，无法入库。请从查询页进入。");
                return;
            }

            // 调用扫码入库接口
            var resp = await _api.OutStockByBarcodeAsync(OutstockId!, barcode);

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

        // OutboundMaterialViewModel.cs

        private Task<bool> AskAsync(string title, string message, string ok = "是", string cancel = "否") =>
            Shell.Current?.DisplayAlert(title, message, ok, cancel) ?? Task.FromResult(false);

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

    public class OutPendingItem
    {
        public string Name { get; set; } = ""; //产品名称
        public string MaterialCode { get; set; } = ""; //产品编码
        public string Spec { get; set; } = "";//规格
        public string Location { get; set; } = "";//出库库位
        public string ProductionBatch { get; set; } = "";//生产批号

        public string StockBatch { get; set; } = "";//批次号
        public int OutstockQty { get; set; } //出库数量
        public int Qty { get; set; } //已扫描数
        public string Bin { get; set; } = "请选择";

    }
}
