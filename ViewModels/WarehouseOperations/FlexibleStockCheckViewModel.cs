using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 灵活盘点页 VM
    /// </summary>
    public partial class FlexibleStockCheckViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IWorkOrderApi _api;
        private readonly SemaphoreSlim _scanLock = new(1, 1);
        private readonly CancellationTokenSource _cts = new();

        public FlexibleStockCheckViewModel(IWorkOrderApi api)
        {
            _api = api;
        }

        /// <summary>盘点单主表 id（从上一页带入）</summary>
        [ObservableProperty]
        private string? stockCheckId;

        /// <summary>盘点单号（从上一页带入）</summary>
        [ObservableProperty]
        private string? checkNo;

        [ObservableProperty]
        private string? checkId;

        /// <summary>仓库名称（从上一页或接口首条记录带入）</summary>
        [ObservableProperty]
        private string? warehouseName;

        /// <summary>库位号</summary>
        [ObservableProperty]
        private string? warehouseCode;

        /// <summary>物料条码</summary>
        [ObservableProperty]
        private string? materialBarcode;

        [ObservableProperty]
        private string? locationCode;

        /// <summary>正在查询/保存</summary>
        [ObservableProperty]
        private bool isBusy;

        // 是否为灵活盘点入口：默认 true，表示不带盘点单号
        [ObservableProperty]
        private bool isFlexibleMode = true;


        /// <summary>盘点明细列表</summary>
        public ObservableCollection<StockCheckDetailItem> Details { get; } = new();

        // ===== 弹窗相关属性 =====

        /// <summary>弹窗是否可见</summary>
        [ObservableProperty]
        private bool isEditDialogVisible;

        /// <summary>当前编辑的明细</summary>
        [ObservableProperty]
        private StockCheckDetailItem? editingItem;

        /// <summary>弹窗里的盘点数量（字符串，方便绑定输入）</summary>
        [ObservableProperty]
        private string? editCheckQtyText;

        /// <summary>弹窗里的备注</summary>
        [ObservableProperty]
        private string? editMemo;
        [ObservableProperty]
        private string? auditStatus;      // 0-待执行 1-执行中 2-已完成

        [ObservableProperty]
        private bool canEdit = true;      // 是否可以编辑/结存

        private StockCheckDetailItem? _lastSelectedItem;

        partial void OnAuditStatusChanged(string? value)
        {
            CanEdit = !string.Equals(value, "2");   // 已完成不能编辑
        }


        // ========== Shell 传参 ==========
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // 是否带了 CheckNo，如果带了就是“普通盘点”模式
            if (query.TryGetValue("CheckNo", out var c) && c is string s1 && !string.IsNullOrWhiteSpace(s1))
            {
                CheckNo = s1;
                IsFlexibleMode = false;   // 普通盘点
                if (query.TryGetValue("WarehouseName", out var w) && w is string s2)
                    WarehouseName = s2;
                if (query.TryGetValue("AuditStatus", out var a) && a is string s3)
                    AuditStatus = s3;   // 触发 OnAuditStatusChanged
                if (query.TryGetValue("CheckId", out var b) && b is string s4)
                    CheckId = s4;   
            }
            else
            {
                IsFlexibleMode = true;    // 灵活盘点，不强制需要 CheckNo
            }
        }


        // ========== 命令 ==========

        /// <summary>
        /// 页面初始化时调用：根据传入的盘点单号 + 库位号加载列表
        /// </summary>
        public Task InitialLoadAsync()
        {
            // 只在还没加载过的情况下加载一次
            if (!string.IsNullOrWhiteSpace(CheckNo) && Details.Count == 0)
            {
                return QueryDetailsAsync(null, null);
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task ScanLocationSubmit()
        {
            var loc = LocationCode?.Trim();
            if (string.IsNullOrEmpty(loc))
            {
                await ShowTip("请先输入或扫描库位号。");
                return;
            }

            await QueryDetailsAsync(loc, MaterialBarcode);

            // 如果只有一条数据，直接弹窗编辑
            if (Details.Count == 1)
            {
                await OpenEditDialog(Details[0]);
            }
        }

        [RelayCommand]
        private async Task ScanMaterialSubmit()
        {
            var code = MaterialBarcode?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                await ShowTip("请先输入或扫描物料条码。");
                return;
            }

            await QueryDetailsAsync(LocationCode, code);

            if (Details.Count == 1)
            {
                await  OpenEditDialog(Details[0]);
            }
        }


        /// <summary>点击列表某一行</summary>
        [RelayCommand]
        private async Task OpenEditDialog(StockCheckDetailItem item)
        {
            if (!IsFlexibleMode && !CanEdit)
            {
                await ShowTip("该盘点单已完成，不能再编辑。");
                return;
            }

            try
            {
                IsBusy = true;
                await Task.Yield();

                // 只处理上一次和当前两个对象
                if (_lastSelectedItem != null)
                    _lastSelectedItem.IsSelected = false;

                item.IsSelected = true;
                _lastSelectedItem = item;

                EditingItem = item;
                EditCheckQtyText = item.checkQty.ToString();
                EditMemo = item.memo;

                IsEditDialogVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }




        /// <summary>弹窗点“取消”</summary>
        [RelayCommand]
        private void CancelEdit()
        {
            IsEditDialogVisible = false;
            EditingItem = null;
            EditCheckQtyText = null;
            EditMemo = null;
        }

        /// <summary>弹窗点“确认” —— 普通盘点调接口，灵活盘点只改本地缓存</summary>
        [RelayCommand]
        private async Task ConfirmEdit()
        {
            // ===== 通用校验部分 =====
            if (!IsFlexibleMode && !CanEdit)
            {
                await ShowTip("该盘点单已完成，不能再编辑。");
                return;
            }

            if (EditingItem is null)
            {
                IsEditDialogVisible = false;
                return;
            }

            // 灵活盘点不需要主表 id 校验，所以这个校验只对普通盘点生效
            if (!IsFlexibleMode && string.IsNullOrWhiteSpace(EditingItem.id))
            {
                await ShowTip("缺少盘点单明细 id，无法保存。");
                return;
            }

            if (!decimal.TryParse(EditCheckQtyText?.Trim(), out var checkQty))
            {
                await ShowTip("请输入正确的盘点数量。");
                return;
            }

            var item = EditingItem;
            var profitLoss = checkQty - item.instockQty;

            // ===== 分支 1：灵活盘点 —— 只改页面缓存，不调接口 =====
            if (IsFlexibleMode)
            {
                // 本地更新列表显示
                item.checkQty = checkQty;
                item.profitLossQty = profitLoss;
                item.memo = EditMemo;

                // 如果行模型没有属性通知，这里仍然用 Remove/Insert 触发 UI 刷新
                var idx = Details.IndexOf(item);
                if (idx >= 0)
                {
                    Details.RemoveAt(idx);
                    Details.Insert(idx, item);
                }

                // 关闭弹窗即可，真正提交由“结存”按钮统一处理
                IsEditDialogVisible = false;
                return;
            }

            // ===== 分支 2：普通盘点 —— 调保存接口 =====
            var req = new StockCheckEditReq
            {
                id = CheckId,
                saveOrHand = "1", // 如需区分暂存/提交，可在这里赋值
                wmsInstockCheckDetailList =
        {
            new StockCheckEditDetailReq
            {
                id = item.id,
                checkQty = checkQty,
                profitLossQty = profitLoss,
                dataBelong = item.dataBelong,
                memo = EditMemo
            }
        }
            };

            try
            {
                IsBusy = true;
                var ok = await _api.EditStockCheckAsync(req, _cts.Token);

                if (!ok.Succeeded)
                {
                    await ShowTip(string.IsNullOrWhiteSpace(ok.Message) ? "保存失败" : ok.Message!);
                    return;
                }

                // 本地更新列表显示
                item.checkQty = checkQty;
                item.profitLossQty = profitLoss;
                item.memo = EditMemo;

                var idx = Details.IndexOf(item);
                if (idx >= 0)
                {
                    Details.RemoveAt(idx);
                    Details.Insert(idx, item);
                }

                IsEditDialogVisible = false;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await ShowTip("保存异常：" + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SettleAsync()
        {
            if (!IsFlexibleMode && !CanEdit)
            {
                await ShowTip("该盘点单已完成，无需重复结存。");
                return;
            }

            IsBusy = true;
            try
            {
                // ==========【模式 1：灵活盘点】==========
                if (IsFlexibleMode)
                {
                    // 灵活盘点：只取盘点数量不为 0 的
                    var all = Details.Where(d => d.checkQty != 0).ToList();

                    if (all.Count == 0)
                    {
                        await ShowTip("没有可结存的数据，请先录入盘点数量。");
                        return;
                    }

                    // ==========【开始组装灵活盘点结存请求体】==========
                    var first = all.First();

                    var flexReq = new FlexibleStockCheckAddReq
                    {
                        memo = null,
                        saveOrHand = "2",          // 1-保存,2-结存
                        warehouseCode = first.warehouseCode,
                        warehouseName = first.warehouseName,
                    };

                    foreach (var r in all)
                    {
                        flexReq.wmsInstockCheckDetailList.Add(new FlexibleStockCheckAddDetailReq
                        {
                            checkQty = r.checkQty,
                            instockQty = r.instockQty,
                            profitLossQty = r.profitLossQty,
                            location = r.location,
                            materialCode = r.materialCode,
                            materialName = r.materialName,
                            stockBatch = r.stockBatch,
                            unit = r.unit,
                            memo = r.memo,
                            warehouseCode = r.warehouseCode,
                            warehouseName = r.warehouseName,
                            dataBelong = r.dataBelong,
                            spec = r.spec,
                            model = r.model,
                            productionBatch = r.productionBatch,
                            productionDate = r.productionDate
                        });
                    }

                    // ==========【调用灵活盘点结存接口】==========
                    var ok = await _api.AddFlexibleStockCheckAsync(flexReq, _cts.Token);
                    if (!ok.Succeeded)
                    {
                        await ShowTip(ok.Message ?? "灵活盘点结存失败");
                        return;
                    }

                    await ShowTip("灵活盘点结存成功");
                    AuditStatus = "2";    // 已完成
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // ==========【模式 2：普通盘点】==========
                if (string.IsNullOrWhiteSpace(CheckNo))
                {
                    await ShowTip("缺少盘点单号，无法结存。");
                    return;
                }

                var resp = await _api.PageStockCheckDetailsAsync(
                    checkNo: CheckNo!,
                    location: null,
                    materialBarcode: null,
                    searchCount: false,
                    pageNo: 1,
                    pageSize: 2000,
                    ct: _cts.Token);

                if (resp == null || resp.success != true || resp.result == null)
                {
                    await ShowTip(resp?.message ?? "查询盘点明细失败。");
                    return;
                }

                var allNormal = resp.result.records ?? new();

                if (allNormal.Count == 0)
                {
                    await ShowTip("当前盘点单没有明细，无法结存。");
                    return;
                }

                // 普通盘点必须检查全部录入
                var notFilled = allNormal.Where(x => x.checkQty == 0).ToList();
                if (notFilled.Any())
                {
                    var f = notFilled.First();
                    await ShowTip($"未全部完成盘点，例如库位：{f.location}，物料：{f.materialCode}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(CheckId))
                {
                    await ShowTip("缺少盘点单主表 id，无法结存。");
                    return;
                }

                // ==========【开始组装普通盘点结存请求体】==========
                var editReq = new StockCheckEditReq
                {
                    id = CheckId,
                    // 这里用 "2" 表示结存/提交，如果后台约定结存也是 "1" 就保持一致改回 "1"
                    saveOrHand = "2"
                };

                foreach (var r in allNormal)
                {
                    editReq.wmsInstockCheckDetailList.Add(new StockCheckEditDetailReq
                    {
                        id = r.id,
                        checkQty = r.checkQty,
                        profitLossQty = r.profitLossQty,
                        dataBelong = r.dataBelong,
                        memo = r.memo
                    });
                }

                // ==========【调用普通盘点结存接口】==========
                var ok2 = await _api.EditStockCheckAsync(editReq, _cts.Token);
                if (!ok2.Succeeded)
                {
                    await ShowTip(ok2.Message ?? "结存失败");
                    return;
                }

                await ShowTip("结存成功");
                AuditStatus = "2";    // 已完成
                await Shell.Current.GoToAsync("..");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await ShowTip("结存异常：" + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }






        public async Task QueryDetailsAsync(string? location, string? materialBarcode)
        {
            if (!IsFlexibleMode && string.IsNullOrWhiteSpace(CheckNo))
            {
                await ShowTip("缺少盘点单号，无法查询盘点明细。");
                return;
            }

            var gotLock = false;
            try
            {
                await _scanLock.WaitAsync(_cts.Token);
                gotLock = true;

                IsBusy = true;

                var resp = await _api.PageStockCheckDetailsAsync(
                    checkNo: CheckNo!,
                    location: location,
                    materialBarcode: materialBarcode,
                    searchCount: false,
                    pageNo: 1,
                    pageSize: 10,
                    ct: _cts.Token);

                Details.Clear();

                if (resp == null || resp.success != true || resp.result == null)
                {
                    var msg = string.IsNullOrWhiteSpace(resp?.message)
                        ? "查询盘点明细失败，请稍后重试。"
                        : resp!.message!;
                    await ShowTip(msg);
                    return;
                }

                var records = resp.result.records ?? new List<StockCheckDetailItem>();
                var i = 1;
                foreach (var r in records)
                {
                    r.index = i++;
                    Details.Add(r);
                }
            }
            catch (OperationCanceledException)
            {
                // 可按需忽略
            }
            catch (Exception ex)
            {
                await ShowTip("查询异常：" + ex.Message);
            }
            finally
            {
                IsBusy = false;
                if (gotLock)
                    _scanLock.Release();
            }
        }


        private Task ShowTip(string msg) =>
            Shell.Current?.DisplayAlert("提示", msg, "确定") ?? Task.CompletedTask;
    }
}
