using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using ConfirmDetail = IndustrialControlMAUI.Models.InStockDetail;
// 使用服务层 DTO，避免 VM 内重复定义
using ConfirmReq = IndustrialControlMAUI.Models.InStockConfirmReq;
using SharedLocationVM = IndustrialControlMAUI.ViewModels.LocationVM;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class InboundMoldViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;

        private readonly IMoldApi _api;
        private readonly IServiceProvider _sp;
        // 串行化“扫码→接口→更新”
        private readonly SemaphoreSlim _scanLock = new(1, 1);

        public Func<Task<SharedLocationVM?>>? PickLocationAsync { get; set; }


        /// <summary>执行 InboundMoldViewModel 初始化逻辑。</summary>
        public InboundMoldViewModel(IMoldApi api)
        {
            _api = api;
        }

        // 绿色卡片列表（扫描成功且“使用中”的模具）
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<MoldScanRow> MoldStatusList { get; } = new();

        [ObservableProperty] private MoldScanRow? selectedRow;

        // ============== 确认入库：弹出库位→组包→提交 ==============
        /// <summary>执行 ConfirmInbound 逻辑。</summary>
        [RelayCommand]
        private async Task ConfirmInbound()
        {
            var rows = MoldStatusList.ToList();
            if (rows.Count == 0)
            {
                await ShowTip("没有可入库的记录。");
                return;
            }

            // 2) 弹出库位选择
            if (PickLocationAsync is null)
            {
                await ShowTip("未提供库位选择回调。");
                return;
            }
            SharedLocationVM? picked = await PickLocationAsync(); // 调回页面方法
            if (picked is null) return;

            var mapped = new BinInfo
            {
                WarehouseCode = picked.WarehouseCode,
                WarehouseName = picked.WarehouseName,
                ZoneCode = picked.Zone,
                RackCode = picked.Rack,
                LayerCode = picked.Layer,
                Location = picked.Location,
                InventoryStatus = picked.InventoryStatus,
                InStock = string.Equals(picked.InventoryStatus, "instock", StringComparison.OrdinalIgnoreCase)
            };

            // 3) 组装请求体（/pda/mold/inStock）
            var req = new ConfirmReq
            {
                instockDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                // memo = null,
                @operator = Preferences.Get("UserName", string.Empty),
                orderType = "in_mold",
                orderTypeName = "模具入库",
                workOrderNo = MoldStatusList[0].WorkOrderNo
            };

            foreach (var r in rows)
            {
                req.wmsMaterialInstockDetailList.Add(new ConfirmDetail
                {
                    instockQty = 1,
                    instockWarehouse = mapped.WarehouseName,
                    instockWarehouseCode = mapped.WarehouseCode,
                    location = mapped.Location,
                    materialCode = r.MoldCode,
                    materialName = r.MoldCode,
                    model = r.MoldModel
                });

                // ✅ 新增：同步更新本地行对象（触发 UI）
                r.Location = mapped.Location ?? "";
                r.WarehouseCode = mapped.WarehouseCode ?? "";
                r.WarehouseName = mapped.WarehouseName ?? "";
            }


            var resp = await _api.ConfirmInStockByListAsync(req);
            if (!resp.Succeeded)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.Message) ? "确认入库失败，请检查数据。" : resp.Message!);
                return;
            }

            // 4) 成功：清空列表或只移除已提交项（这里清空）
            MoldStatusList.Clear();
            await ShowTip("确认入库成功。");
        }

        // ============== 取消一条扫描 ==============
        /// <summary>执行 CancelScan 逻辑。</summary>
        [RelayCommand]
        private async Task CancelScan()
        {
            var checkedRows = MoldStatusList.Where(r => r.IsSelected).ToList();
            if (checkedRows.Count == 0)
            {
                await ShowTip("请先勾选要取消的记录。");
                return;
            }

            foreach (var r in checkedRows)
                MoldStatusList.Remove(r);

            // 可选：清空页面的“当前选中项”
            if (checkedRows.Contains(SelectedRow))
                SelectedRow = null;
        }


        /// <summary>执行 ClearScan 逻辑。</summary>
        public void ClearScan() => MoldStatusList.Clear();

        // ============== 扫描回调：查询→分支处理 ==============
        /// <summary>执行 ScanSubmit 逻辑。</summary>
        [RelayCommand]
        private async Task ScanSubmit()
        {
            var text = ScanCode?.Trim();
            if (!string.IsNullOrEmpty(text))
                await HandleScannedAsync(text, "");
            ScanCode = string.Empty;
        }
        /// <summary>执行 HandleScannedAsync 逻辑。</summary>
        public async Task HandleScannedAsync(string data, string symbology)
        {

            var code = (data ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                await MainThread.InvokeOnMainThreadAsync(() => ShowTip("无效条码。"));
                return;
            }

            await _scanLock.WaitAsync();
            try
            {
                MoldScanRow? row = null;

                // 1. 所有集合/UI对象操作切主线程
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    row = UpsertLocalByMoldCode(code);
                });

                var resp = await _api.InStockScanQueryAsync(code);

                if (row == null)
                    return;

                if (resp is null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        row.UseStatusText = "查询失败";
                    });
                    await MainThread.InvokeOnMainThreadAsync(() => ShowTip("接口无响应。"));
                    return;
                }

                if (resp.success != true || resp.result is null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        row.UseStatusText = "未找到";
                    });
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        ShowTip(string.IsNullOrWhiteSpace(resp.message) ? "未查询到该模具信息" : resp.message!));
                    return;
                }

                var isUsing = resp.result.usageStatus == true;

                var updated = new MoldScanRow
                {
                    MoldCode = resp.result.moldCode ?? code,
                    MoldModel = resp.result.moldModel ?? "",
                    UseStatusText = isUsing ? "使用中" : "未使用",
                    WorkOrderNo = isUsing ? (resp.result.workOrderNo ?? "") : "",
                    OutstockDate = ToYmd(resp.result.outstockDate),
                    Location = resp.result.location ?? "",
                    WarehouseName = resp.result.warehouseName ?? "",
                    WarehouseCode = resp.result.warehouseCode ?? "",
                    IsSelected = true
                };

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ApplyRow(row, updated);
                    row.IsSelected = true;
                    SelectedRow = row;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleScannedAsync] 扫码处理异常: {ex}");

                await MainThread.InvokeOnMainThreadAsync(() => ShowTip($"扫描处理失败：{ex.Message}"));
            }
            finally
            {
                _scanLock.Release();
            }
        }

        // 本地根据 MoldCode 做 Upsert：有就用旧的，没有就占位插入一条（不重复）
        /// <summary>执行 UpsertLocalByMoldCode 逻辑。</summary>
        private MoldScanRow UpsertLocalByMoldCode(string moldCode)
        {
            var exist = MoldStatusList.FirstOrDefault(x =>
                string.Equals(x.MoldCode, moldCode, StringComparison.OrdinalIgnoreCase));

            if (exist != null)
            {
                exist.IsSelected = true;
                SelectedRow = exist;
                return exist;
            }

            // 占位行：接口回来后再更新各字段（不再“删旧插新”）
            var placeholder = new MoldScanRow
            {
                IsSelected = true,
                MoldCode = moldCode,
                MoldModel = "",
                UseStatusText = "查询中…",
                WorkOrderNo = "",
                OutstockDate = "",
                Location = "",
                WarehouseName = "",
                WarehouseCode = ""
            };
            // 稳定插入到顶部（只插一次）
            MoldStatusList.Insert(0, placeholder);
            SelectedRow = placeholder;
            return placeholder;
        }

        // 只更新字段，不替换对象（避免 UI “删除→新增”导致的闪一下）
        /// <summary>执行 ApplyRow 逻辑。</summary>
        private static void ApplyRow(MoldScanRow target, MoldScanRow src)
        {
            if (!string.Equals(target.MoldModel, src.MoldModel, StringComparison.Ordinal)) target.MoldModel = src.MoldModel;
            if (!string.Equals(target.UseStatusText, src.UseStatusText, StringComparison.Ordinal)) target.UseStatusText = src.UseStatusText;
            if (!string.Equals(target.WorkOrderNo, src.WorkOrderNo, StringComparison.Ordinal)) target.WorkOrderNo = src.WorkOrderNo;
            if (!string.Equals(target.OutstockDate, src.OutstockDate, StringComparison.Ordinal)) target.OutstockDate = src.OutstockDate;
            if (!string.Equals(target.Location, src.Location, StringComparison.Ordinal)) target.Location = src.Location;
            if (!string.Equals(target.WarehouseName, src.WarehouseName, StringComparison.Ordinal)) target.WarehouseName = src.WarehouseName;
            if (!string.Equals(target.WarehouseCode, src.WarehouseCode, StringComparison.Ordinal)) target.WarehouseCode = src.WarehouseCode;
        }


        /// <summary>执行 ToYmd 逻辑。</summary>
        private static string ToYmd(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (DateTime.TryParse(s, out var dt)) return dt.ToString("yyyy-MM-dd");
            var i = s.IndexOf(' ');
            return i > 0 ? s[..i] : s;   // "2025-09-05 00:00:00" -> "2025-09-05"
        }


        /// <summary>执行 ShowTip 逻辑。</summary>
        private Task ShowTip(string msg) =>
    MainThread.InvokeOnMainThreadAsync(() =>
        Shell.Current?.DisplayAlert("提示", msg, "确定") ?? Task.CompletedTask);

        // 兼容不同 BinInfo 定义，智能取库位编码

    }

    // 绿色卡片的数据模型
    public partial class MoldScanRow : ObservableObject
    {
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private string moldCode = "";
        [ObservableProperty] private string moldModel = "";
        [ObservableProperty] private string useStatusText = "";
        [ObservableProperty] private string workOrderNo = "";
        [ObservableProperty] private string outstockDate = "";
        [ObservableProperty] private string location = "";
        [ObservableProperty] private string warehouseName = "";
        [ObservableProperty] private string warehouseCode = "";
    }
}
