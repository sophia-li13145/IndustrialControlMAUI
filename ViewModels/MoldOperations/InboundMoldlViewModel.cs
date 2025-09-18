using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Pages;   // 用于弹出 BinPickerPage / BinListPage
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using ConfirmDetail = IndustrialControlMAUI.Services.InStockDetail;
// 使用服务层 DTO，避免 VM 内重复定义
using ConfirmReq = IndustrialControlMAUI.Services.InStockConfirmReq;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class InboundMoldViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;

        private readonly IMoldApi _api;

        public InboundMoldViewModel(IMoldApi api)
        {
            _api = api;
        }

        // 绿色卡片列表（扫描成功且“使用中”的模具）
        public ObservableCollection<MoldScanRow> MoldStatusList { get; } = new();

        [ObservableProperty] private MoldScanRow? selectedRow;

        // ============== 确认入库：弹出库位→组包→提交 ==============
        [RelayCommand]
        private async Task ConfirmInbound()
        {
            // 1) 选择需要提交的行：优先选中项；若无勾选则提交全部
            var rows = MoldStatusList.Where(x => x.IsSelected).ToList();
            if (rows.Count == 0) rows = MoldStatusList.ToList();
            if (rows.Count == 0)
            {
                await ShowTip("没有可入库的记录。");
                return;
            }

            // 2) 弹出库位选择（预选：取第一条的 Location）
            var preselect = rows[0].Location;
            var pickedBin = await BinPickerPage.ShowAsync(preselect);   // 由页面加载库位并返回 BinInfo
            if (pickedBin is null) return; // 取消

            // 从 BinInfo 智能提取库位编码（兼容 Code/BinCode/Location 等命名）
            string? pickedCode = GetBinCode(pickedBin);
            if (string.IsNullOrWhiteSpace(pickedCode))
            {
                await ShowTip("未能识别所选库位编码，请重试或检查库位数据。");
                return;
            }

            // 3) 组装请求体（/normalService/pda/mold/inStock）
            var req = new ConfirmReq
            {
                instockDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                memo = null,
                @operator = null,
                orderType = null,
                orderTypeName = null,
                workOrderNo = null
            };

            foreach (var r in rows)
            {
                // 库位使用用户刚刚选择的 pickedCode；其余字段按当前页面能拿到的值映射
                req.wmsMaterialInstockDetailList.Add(new ConfirmDetail
                {
                    instockQty = 1,
                    instockWarehouse = null,
                    instockWarehouseCode = string.IsNullOrWhiteSpace(r.WarehouseCode) ? null : r.WarehouseCode, // 来自查询
                    location = pickedCode,                           // ★ 用户从 BinPicker 选的库位
                    materialCode = r.MoldCode,                       // ★ 模具编号作为编码
                    materialName = string.IsNullOrWhiteSpace(r.MoldModel) ? r.MoldCode : r.MoldModel,
                    model = r.MoldModel
                });

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


        public void ClearScan() => MoldStatusList.Clear();

        // ============== 扫描回调：查询→分支处理 ==============
        public async Task HandleScannedAsync(string data, string symbology)
        {
            var code = (data ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                await ShowTip("无效条码。");
                return;
            }

            var resp = await _api.InStockScanQueryAsync(code);
            if (resp is null)
            {
                await ShowTip("接口无响应。");
                return;
            }
            if (resp.success != true || resp.result is null)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.message) ? "未查询到该模具信息" : resp.message!);
                return;
            }

            // 不再分支 return；统一加入列表
            var isUsing = resp.result.usageStatus == true; // true=使用中，false=在库/未使用
            var row = new MoldScanRow
            {
                IsSelected = false,
                MoldCode = resp.result.moldCode ?? "",
                MoldModel = resp.result.moldModel ?? "",
                UseStatusText = isUsing ? "使用中" : "未使用",           // 你也可改成 "未使用"
                WorkOrderNo = isUsing ? (resp.result.workOrderNo ?? "") : "", // 仅使用中才显示工单号
                OutstockDate = ToYmd(resp.result.outstockDate),
                Location = resp.result.location ?? "",
                WarehouseName = resp.result.warehouseName ?? "",
                WarehouseCode = resp.result.warehouseCode ?? ""
            };

            // 去重：同一模具编号只保留最新一条
            var old = MoldStatusList.FirstOrDefault(x =>
                string.Equals(x.MoldCode, row.MoldCode, StringComparison.OrdinalIgnoreCase));
            if (old != null) MoldStatusList.Remove(old);

            MoldStatusList.Insert(0, row);
            SelectedRow = row;
        }
        private static string ToYmd(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (DateTime.TryParse(s, out var dt)) return dt.ToString("yyyy-MM-dd");
            var i = s.IndexOf(' ');
            return i > 0 ? s[..i] : s;   // "2025-09-05 00:00:00" -> "2025-09-05"
        }


        private Task ShowTip(string msg) =>
            Shell.Current?.DisplayAlert("提示", msg, "确定") ?? Task.CompletedTask;

        // 兼容不同 BinInfo 定义，智能取库位编码
        private static string? GetBinCode(object? bin)
        {
            if (bin is null) return null;
            var props = new[] { "Code", "BinCode", "Location", "Bin", "Id", "code", "binCode" };
            foreach (var p in props)
            {
                var pi = bin.GetType().GetProperty(p);
                if (pi != null)
                {
                    var v = pi.GetValue(bin)?.ToString();
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            return null;
        }
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
