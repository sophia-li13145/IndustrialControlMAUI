using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutboundMoldViewModel : ObservableObject
    {
        [ObservableProperty] private string? scanCode;
        private readonly IMoldApi _api;

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
        public IAsyncRelayCommand ConfirmCommand { get; }

        public OutboundMoldViewModel(IMoldApi api)
        {
            _api = api;
            ShowPendingCommand = new RelayCommand(() => SwitchTab(true));
            ShowScannedCommand = new RelayCommand(() => SwitchTab(false));
            //ConfirmCommand = new AsyncRelayCommand(ConfirmOutboundAsync);
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


        [RelayCommand]
        private async Task PassScan()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法确认。请从查询页进入。");
                return;
            }
            await ShowTip("已确认通过。");
        }


        [RelayCommand]
        private async Task CancelScan()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法取消。请从查询页进入。");
                return;
            }


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


        }


        private Task ShowTip(string message) =>
            Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;


        public void ClearScan() => ScannedList.Clear();
        public void ClearAll()
        {
            PendingList.Clear();
            ScannedList.Clear();
        }



        public async Task<bool> ConfirmOutboundAsync()
        {
            if (string.IsNullOrWhiteSpace(InstockId))
            {
                await ShowTip("缺少 InstockId，无法确认入库。请从查询页进入。");
                return false;
            }



            return true;
        }

    }

    // === 列表行模型 ===


}
