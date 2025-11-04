using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 出库-模具页面 ViewModel
    /// 依赖 IMoldApi.GetViewAsync(workOrderNo) 获取「型号+基础需求数量+模具编码列表」视图
    /// </summary>
    public partial class OutboundMoldViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IMoldApi _api;

        #region 构造 & 注入
        public OutboundMoldViewModel(IMoldApi api)
        {
            _api = api;
            MoldGroups = new ObservableCollection<MoldGroupVM>();
            ScannedList = new ObservableCollection<ScannedRow>();

            ShowScannedCommand = new RelayCommand(() =>
            {
                IsScannedVisible = true;
                ScannedTabColor = "#CCCCCC";
                ScannedTextColor = "Green";
            });

            CancelScanCommand = new AsyncRelayCommand(CancelScanAsync);
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync);
            ScanSubmitCommand = new AsyncRelayCommand(ScanSubmitAsync);
            
        }
        // 无 DI 场景可用的辅助构造（可按需删除）
        public OutboundMoldViewModel() : this(
            (IMoldApi)App.Current!.Handler!.MauiContext!.Services.GetService(typeof(IMoldApi))!)
        { }
        #endregion

        #region 顶部信息 & 输入
        [ObservableProperty] private string? workOrderNo;
        [ObservableProperty] private string? materialName;
        [ObservableProperty] private string? scanCode;
        #endregion

        #region 分组（一级=型号/数量，二级=模具编码列表）
        public ObservableCollection<MoldGroupVM> MoldGroups { get; }
        #endregion

        #region 扫描明细
        public ObservableCollection<ScannedRow> ScannedList { get; }

        [ObservableProperty] private ScannedRow? selectedScanItem;

        [ObservableProperty] private bool isScannedVisible = true;
        [ObservableProperty] private string scannedTabColor = "#CCCCCC";
        [ObservableProperty] private string scannedTextColor = "Green";

        private CancellationToken _lifecycleToken = CancellationToken.None;
        public void SetLifecycleToken(CancellationToken token) => _lifecycleToken = token;
        #endregion

        #region 命令
        public ICommand ShowScannedCommand { get; }
        public IAsyncRelayCommand CancelScanCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IAsyncRelayCommand ScanSubmitCommand { get; }
        #endregion

        #region 加载
        public async Task LoadAsync(string workOrderNo)
        {
            WorkOrderNo = workOrderNo;

            var view = await _api.GetViewAsync(workOrderNo, _lifecycleToken);
            MaterialName = view.MaterialName;

            MoldGroups.Clear();
            foreach (var m in view.Models)
            {
                var group = new MoldGroupVM(m.ModelCode, m.BaseQty);
                int idx = 1;
                foreach (var code in m.MoldNumbers)
                {
                    group.Items.Add(new MoldItemVM { Index = idx++, MoldNumber = code });
                }
                MoldGroups.Add(group);
            }

            // 默认展开第一组（可按需注释）
            if (MoldGroups.Count > 0)
                MoldGroups[0].SetExpanded(true);
            // 2) ★ 已扫描列表（来自后端）
            ScannedList.Clear();
            var i = 1;
            foreach (var s in view.Scanned)
            {
                ScannedList.Add(new ScannedRow
                {
                    Index = i++,
                    MoldCode = s.MoldCode,
                    MoldModel = s.MoldModel,
                    OutQty = 1,                      // 后端若有数量字段可替换
                    Location = s.Location ?? "",
                    IsSelected = false
                });
            }
            view.Scanned.Where(x => !x.IzOutStock);
        }
        #endregion

        #region 扫码加入明细（支持扫“模具编码”，或扫“型号”批量加入）

        private async Task ScanSubmitAsync()
        {
            var code = (ScanCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code)) return;


            if (string.IsNullOrWhiteSpace(WorkOrderNo))
            {
                await Application.Current.MainPage.DisplayAlert("提示", "缺少工单号，无法校验该模具编码。", "知道了");
                return;
            }

            try
            {
                var resp = await _api.OutStockScanQueryAsync(code, WorkOrderNo, _lifecycleToken);
                var ok = resp?.success == true && resp.result != null;
                if (!ok)
                {
                    await Application.Current.MainPage.DisplayAlert("提示", $"未查询到该模具编码：{code}", "知道了");
                    return;
                }


                var r = resp!.result!;
                var moldCode = (r.moldCode ?? string.Empty).Trim();
                var moldModel = (r.moldModel ?? string.Empty).Trim();
                var location = (r.location ?? string.Empty).Trim();
                var warehouseCode = (r.warehouseCode ?? string.Empty).Trim();
                var warehouseName = (r.warehouseName ?? string.Empty).Trim();
                var isOut = r.izOutStock??false;

                if (isOut)
                {
                    await Application.Current.MainPage.DisplayAlert("已出库", $"模具[{moldCode}] 已完成出库，不能重复出库。", "知道了");
                    return;
                }

                // 校验型号是否在当前工单的分组中
                var grp = MoldGroups.FirstOrDefault(g =>
                    string.Equals(g.ModelCode?.Trim(), moldModel, StringComparison.OrdinalIgnoreCase));

                if (grp == null)
                {
                    await Application.Current.MainPage.DisplayAlert("不在列表", $"模具型号 [{moldModel}] 不在当前工单的需求列表中。", "知道了");
                    return;
                }

                // 加入扫描列表（或数量+1）
                AddOrIncreaseScanned(moldCode, moldModel, location, warehouseCode, warehouseName);

                // 清空输入，准备下一次
                ScanCode = string.Empty;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"扫描校验失败：{ex.Message}", "好的");
            }
        }

        // 原方法保留，但增加 location 参数（旧调用点不再使用）
        private void AddOrIncreaseScanned(string moldCode, string modelCode, string? location = null, string? warehouseCode = null, string? warehouseName = null)
        {
            var exist = ScannedList.FirstOrDefault(x =>
                string.Equals(x.MoldCode, moldCode, StringComparison.OrdinalIgnoreCase));

            if (exist != null)
            {
                exist.OutQty += 1;
                // 若后端返回了库位，补上
                if (!string.IsNullOrWhiteSpace(location) && string.IsNullOrWhiteSpace(exist.Location))
                    exist.Location = location;
                return;
            }

            ScannedList.Add(new ScannedRow
            {
                Index = ScannedList.Count + 1,
                MoldCode = moldCode,
                MoldModel = modelCode,
                OutQty = 1,
                Location = location ?? string.Empty,
                OutstockWarehouse = warehouseName ?? string.Empty,
                OutstockWarehouseCode = warehouseCode ?? string.Empty,
                IsSelected = false
            });
        }
       
        #endregion

        #region 取消扫描（支持勾选多选移除）
        private async Task CancelScanAsync()
        {
            var toRemove = ScannedList.Where(x => x.IsSelected).ToList();
            if (toRemove.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请勾选要取消的记录。", "知道了");
                return;
            }

            foreach (var r in toRemove)
                ScannedList.Remove(r);

            // 重新编号
            for (int i = 0; i < ScannedList.Count; i++)
                ScannedList[i].Index = i + 1;

            await Application.Current.MainPage.DisplayAlert("提示", "已取消选择的扫描记录。", "OK");
        }
        #endregion

        #region 确认出库
        private bool _confirming;

        private async Task ConfirmAsync()
        {
            if (_confirming) return;

            if (string.IsNullOrWhiteSpace(WorkOrderNo))
            {
                await Application.Current.MainPage.DisplayAlert("错误", "缺少工单号，无法确认出库。", "OK");
                return;
            }

            // 过滤无效行（空编码或数量<=0）
            var validItems = ScannedList
                .Where(x => !string.IsNullOrWhiteSpace(x.MoldCode) && x.OutQty > 0)
                .ToList();

            if (validItems.Count == 0)
            {
                bool goOn = await Application.Current.MainPage.DisplayAlert("提示", "当前扫描明细为空，确定继续出库？", "继续", "取消");
                if (!goOn) return;
            }

            // 组装请求
            var req = new MoldOutConfirmReq
            {
                workOrderNo = WorkOrderNo,
                outstockDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                @operator = Preferences.Get("UserName", string.Empty), 
                orderType   = "out_mold",
                orderTypeName = "模具出库",
                wmsMaterialOutstockDetailList = validItems.Select(x => new MoldOutDetail
                {
                    materialName = x.MoldCode,
                    materialCode = x.MoldCode!,
                    model = x.MoldModel ?? string.Empty,
                    outstockQty = x.OutQty,
                    location = x.Location ?? string.Empty,
                    // 如后端需要库/库位编码，可在此补充：
                    outstockWarehouse     = x.OutstockWarehouse,
                    outstockWarehouseCode = x.OutstockWarehouseCode,
                }).ToList()
            };

            try
            {
                _confirming = true;

                var ok = await _api.ConfirmOutStockAsync(req);
                if (!ok.Succeeded)
                {
                    await Application.Current.MainPage.DisplayAlert("错误", ok.Message ?? "确认出库失败。", "OK");
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("成功", "已确认出库。", "OK");

                // 清空并刷新（把“已扫描列表”同步刷新）
                ScannedList.Clear();
                await LoadAsync(WorkOrderNo);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"提交失败：{ex.Message}", "OK");
            }
            finally
            {
                _confirming = false;
            }
        }

        #endregion
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("workOrderNo", out var v) && v is string no && !string.IsNullOrWhiteSpace(no))
            {
                // 异步加载放到主线程调度
                MainThread.BeginInvokeOnMainThread(async () => await LoadAsync(no));
            }
        }
    }

    #region 分组子VM
    public partial class MoldGroupVM : ObservableObject
    {
        public MoldGroupVM(string modelCode, int baseQty)
        {
            ModelCode = modelCode;
            BaseQty = baseQty;

            Items = new ObservableCollection<MoldItemVM>();
            ToggleCommand = new RelayCommand(() =>
            {
                IsExpanded = !IsExpanded;
                ToggleIndicator = IsExpanded ? "-" : "+";
            });
        }

        [ObservableProperty] private string modelCode;
        [ObservableProperty] private int baseQty;

        [ObservableProperty] private bool isExpanded = false;
        [ObservableProperty] private string toggleIndicator = "+";

        public ObservableCollection<MoldItemVM> Items { get; }

        public ICommand ToggleCommand { get; }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;
            ToggleIndicator = expanded ? "-" : "+";
        }
    }

    public partial class MoldItemVM : ObservableObject
    {
        [ObservableProperty] private int index;
        [ObservableProperty] private string? moldNumber;
    }

    public partial class ScannedRow : ObservableObject
    {
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private int index;
        [ObservableProperty] private string? moldCode;
        [ObservableProperty] private string? moldName;
        [ObservableProperty] private string? moldModel;
        [ObservableProperty] private int outQty;
        [ObservableProperty] private string? location;
        [ObservableProperty] private string? outstockWarehouse;
        [ObservableProperty] private string? outstockWarehouseCode;
    }
    #endregion
}
