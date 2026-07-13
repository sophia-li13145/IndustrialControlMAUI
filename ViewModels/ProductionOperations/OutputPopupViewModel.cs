using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutputPopupViewModel : ObservableObject
    {
        private readonly IWorkOrderApi? _api;
        private WorkProcessTaskDetail? _detail;
        public ObservableCollection<TaskMaterialOutput> MaterialOptions { get; } = new();
        public ObservableCollection<FrameOptionItem> SelectedFrames { get; } = new();
        public ObservableCollection<BarcodeScanRecord> ScannedBarcodes { get; } = new();
        public ObservableCollection<BarcodeScanDisplayItem> RecentScannedBarcodes { get; } = new();
        private readonly HashSet<string> _scannedBarcodeSet = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _barcodeScanLock = new(1, 1);
        private string? _lastScanBarcode;
        private DateTime _lastScanAt = DateTime.MinValue;
        private string? _barcodeMaterialCode;
        private string? _barcodeMaterialName;

        [ObservableProperty] private TaskMaterialOutput? selectedMaterial;
        [ObservableProperty] private string? quantityText;
        [ObservableProperty] private string? memo;
        [ObservableProperty] private bool isPickerEnabled = true;
        [ObservableProperty] private bool isQuantityEnabled = true;
        [ObservableProperty] private bool showBatchBarcodeScanButton;
        [ObservableProperty] private string frameHint = "提示：料框非必填，最多选择5个";
        private TaskCompletionSource<OutputPopupResult?>? _tcs;

        public OutputPopupViewModel(IWorkOrderApi? api = null) => _api = api;

        public void Init(
            IEnumerable<TaskMaterialOutput> materialOutputList,
            TaskMaterialOutput? presetMaterialCode = null,
            WorkProcessTaskDetail? detail = null)
        {
            _detail = detail;
            ShowBatchBarcodeScanButton = detail?.finalProcess == true;
            MaterialOptions.Clear();
            SelectedFrames.Clear();
            ScannedBarcodes.Clear();
            RecentScannedBarcodes.Clear();
            _scannedBarcodeSet.Clear();
            _lastScanBarcode = null;
            _lastScanAt = DateTime.MinValue;
            _barcodeMaterialCode = null;
            _barcodeMaterialName = null;
            foreach (var m in materialOutputList ?? Enumerable.Empty<TaskMaterialOutput>())
            {
                if (m is not null) MaterialOptions.Add(m);
            }

            if (presetMaterialCode is not null)
            {
                var hit = MaterialOptions.FirstOrDefault(x => IsSame(x, presetMaterialCode));
                if (hit is null) MaterialOptions.Insert(0, presetMaterialCode);

                SelectedMaterial = MaterialOptions.FirstOrDefault(x => IsSame(x, presetMaterialCode)) ?? presetMaterialCode;
                IsPickerEnabled = false;
            }
            else
            {
                if (MaterialOptions.Count == 1)
                    SelectedMaterial = MaterialOptions[0];

                IsPickerEnabled = true;
            }

            QuantityText = "";
            IsQuantityEnabled = true;
            Memo = "";
            FrameHint = "提示：料框非必填，最多选择5个";
        }

        private static bool IsSame(TaskMaterialOutput a, TaskMaterialOutput b)
            => string.Equals(a?.materialCode, b?.materialCode, StringComparison.OrdinalIgnoreCase);


        [RelayCommand]
        private async Task BatchBarcodeScanAsync()
        {
            if (_detail is null)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "工序任务详情未加载，无法扫码校验。", "好的");
                return;
            }

            if (string.IsNullOrWhiteSpace(_detail.productionBatch) || string.IsNullOrWhiteSpace(_detail.workOrderNo))
            {
                await Application.Current.MainPage.DisplayAlert("提示", "生产批次或工单号为空，无法扫码校验。", "好的");
                return;
            }

            if (_api is null)
            {
                await Application.Current.MainPage.DisplayAlert("失败", "扫码校验服务未初始化。", "OK");
                return;
            }

            if (Application.Current?.MainPage?.Navigation is null) return;

            await Application.Current.MainPage.Navigation.PushAsync(new ContinuousBarcodeScanPage(ValidateBarcodeScanAsync));
        }

        private async Task<BarcodeScanFeedback> ValidateBarcodeScanAsync(string rawBarcode)
        {
            var barcode = rawBarcode.Trim();
            if (string.IsNullOrWhiteSpace(barcode)) return new BarcodeScanFeedback { Success = false };

            var now = DateTime.UtcNow;
            if (string.Equals(_lastScanBarcode, barcode, StringComparison.OrdinalIgnoreCase)
                && (now - _lastScanAt).TotalMilliseconds < 1000)
                return new BarcodeScanFeedback { Success = false, Message = "重复识别已忽略，请继续扫码" };

            _lastScanBarcode = barcode;
            _lastScanAt = now;

            if (_scannedBarcodeSet.Contains(barcode))
            {
                return new BarcodeScanFeedback { Success = false, Message = "条码已存在，请继续扫码" };
            }

            if (!await _barcodeScanLock.WaitAsync(0))
                return new BarcodeScanFeedback { Success = false, Message = "正在校验上一条码，请稍后" };

            try
            {
                var resp = await _api!.ValidateBarcodeScanAsync(new ValidateBarcodeScanReq
                {
                    barcode = barcode,
                    materialCode = SelectedMaterial?.materialCode,
                    productionBatch = _detail?.productionBatch ?? string.Empty,
                    workOrderNo = _detail?.workOrderNo ?? string.Empty
                });

                if (!resp.success || resp.result is null)
                {
                    return new BarcodeScanFeedback { Success = false, Message = resp.message ?? "条码校验失败，请继续扫码" };
                }

                var materialCheck = ValidateScannedMaterial(resp.result);
                if (materialCheck is not null)
                    return materialCheck;

                AddValidatedBarcode(barcode, resp.result);
                return new BarcodeScanFeedback { Success = true, Message = "校验通过，请继续扫码" };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return new BarcodeScanFeedback { Success = false, Message = $"条码校验异常：{ex.Message}" };
            }
            finally
            {
                _barcodeScanLock.Release();
            }
        }


        private BarcodeScanFeedback? ValidateScannedMaterial(ValidateBarcodeScanResp scanResult)
        {
            var materialCode = scanResult.materialCode?.Trim();
            var materialName = scanResult.materialName?.Trim();

            if (ScannedBarcodes.Count == 0)
            {
                _barcodeMaterialCode = materialCode;
                _barcodeMaterialName = materialName;
                return null;
            }

            var codeMatches = string.Equals(_barcodeMaterialCode ?? string.Empty, materialCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            var nameMatches = string.Equals(_barcodeMaterialName ?? string.Empty, materialName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            if (codeMatches && nameMatches) return null;

            return new BarcodeScanFeedback
            {
                Success = false,
                Message = "物料信息不一致，请继续扫码"
            };
        }

        private void AddValidatedBarcode(string barcode, ValidateBarcodeScanResp scanResult)
        {
            var materialCode = scanResult.materialCode;
            var materialName = scanResult.materialName;

            if (!string.IsNullOrWhiteSpace(materialCode))
            {
                var matched = MaterialOptions.FirstOrDefault(x => string.Equals(x.materialCode, materialCode, StringComparison.OrdinalIgnoreCase));
                if (matched is null)
                {
                    matched = new TaskMaterialOutput
                    {
                        materialCode = materialCode,
                        materialName = materialName
                    };
                    MaterialOptions.Insert(0, matched);
                }

                SelectedMaterial = matched;
            }

            if (_scannedBarcodeSet.Add(barcode))
            {
                ScannedBarcodes.Add(new BarcodeScanRecord
                {
                    barcode = barcode,
                    scanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            RefreshRecentScannedBarcodes();
            UpdateBarcodeLockedState();
        }


        partial void OnShowBatchBarcodeScanButtonChanged(bool value)
        {
            if (!value)
                RecentScannedBarcodes.Clear();
            else
                RefreshRecentScannedBarcodes();
        }

        private void RefreshRecentScannedBarcodes()
        {
            RecentScannedBarcodes.Clear();
            if (!ShowBatchBarcodeScanButton) return;

            var recent = ScannedBarcodes
                .Select((record, index) => new BarcodeScanDisplayItem(index + 1, record))
                .TakeLast(10);

            foreach (var item in recent)
                RecentScannedBarcodes.Add(item);
        }

        private void UpdateBarcodeLockedState()
        {
            var hasBarcode = ScannedBarcodes.Count > 0;
            IsPickerEnabled = !hasBarcode;
            IsQuantityEnabled = !hasBarcode;
            QuantityText = hasBarcode ? ScannedBarcodes.Count.ToString() : string.Empty;
        }

        [RelayCommand]
        private void DeleteScannedBarcode(BarcodeScanDisplayItem? item)
        {
            if (item?.Record is null) return;

            var hit = ScannedBarcodes.FirstOrDefault(x => string.Equals(x.barcode, item.Record.barcode, StringComparison.OrdinalIgnoreCase));
            if (hit is null) return;

            ScannedBarcodes.Remove(hit);
            if (!string.IsNullOrWhiteSpace(hit.barcode))
                _scannedBarcodeSet.Remove(hit.barcode);
            if (ScannedBarcodes.Count == 0)
            {
                _barcodeMaterialCode = null;
                _barcodeMaterialName = null;
            }

            RefreshRecentScannedBarcodes();
            UpdateBarcodeLockedState();
        }

        [RelayCommand]
        private async Task ScanFrameAsync()
        {
            if (SelectedMaterial is null)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请先选择产出物料。", "好的");
                return;
            }

            if (Application.Current?.MainPage?.Navigation is null) return;

            var tcs = new TaskCompletionSource<string>();
            await Application.Current.MainPage.Navigation.PushAsync(new QrScanPage(tcs));
            var scanned = await tcs.Task;
            if (string.IsNullOrWhiteSpace(scanned)) return;

            var frameNo = scanned.Trim();
            if (_api is not null)
            {
                var resp = await _api.ScanOutputFrameAsync(frameNo, SelectedMaterial.materialCode ?? string.Empty);
                if (!resp.success || resp.result is null)
                {
                    await Application.Current.MainPage.DisplayAlert("提示", resp.message ?? "料框扫码失败", "好的");
                    return;
                }

                AddSelectedFrame(new FrameOptionItem
                {
                    FrameNo = string.IsNullOrWhiteSpace(resp.result.frameNo) ? frameNo : resp.result.frameNo!,
                    FrameStatus = "占用",
                    MaxLimit = resp.result.maxLimit,
                    MinLimit = resp.result.minLimit
                });
                
            }

        }


        [RelayCommand]
        private void RemoveFrame(FrameOptionItem? item)
        {
            if (item is null) return;
            var hit = SelectedFrames.FirstOrDefault(x => x.FrameNo == item.FrameNo);
            if (hit is not null) SelectedFrames.Remove(hit);
        }

        private void AddSelectedFrame(FrameOptionItem item)
        {
            if (SelectedFrames.Any(x => x.FrameNo == item.FrameNo)) return;
            SelectedFrames.Add(item);
        }

        [RelayCommand]
        private async Task Confirm()
        {
            if (SelectedMaterial is null)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请先选择产出物料。", "我知道了");
                return;
            }
            if (string.IsNullOrWhiteSpace(QuantityText) || !decimal.TryParse(QuantityText, out var qty) || qty <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请输入大于0的产出数量。", "好的");
                return;
            }
            if (ScannedBarcodes.Count > 0)
            {
                qty = ScannedBarcodes.Count;
                QuantityText = qty.ToString();
                IsPickerEnabled = false;
                IsQuantityEnabled = false;
            }
            if (SelectedFrames.Count > 5)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "最多选择5个料框。", "好的");
                return;
            }
            if ((Memo?.Length ?? 0) > 200)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "备注不能超过200个字符。", "好的");
                return;
            }
            var result = new OutputPopupResult
            {
                materialClassName = SelectedMaterial.materialClassName,
                MaterialCode = SelectedMaterial.materialCode,
                MaterialName = SelectedMaterial.materialName,
                materialTypeName = SelectedMaterial.materialTypeName,
                Quantity = qty,
                Unit = SelectedMaterial.unit,
                OperationTime = DateTime.Now,
                Memo = Memo,
                frameNoList = SelectedFrames.Select(x => new OutputFrameSelectionItem { frameNo = x.FrameNo }).ToList(),
                barcodeScanRecordList = ScannedBarcodes.ToList()
            };

            if (_detail is null)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "工序任务详情未加载，无法提交产出。", "OK");
                return;
            }

            if (_api is null)
            {
                await Application.Current.MainPage.DisplayAlert("失败", "新增产出服务未初始化。", "OK");
                return;
            }

            var req = new AddWorkProcessTaskProductOutputReq
            {
                materialClassName = result.materialClassName,
                materialCode = result.MaterialCode,
                materialName = result.MaterialName,
                materialTypeName = result.materialTypeName,
                qty = (double)result.Quantity,
                memo = result.Memo,
                unit = result.Unit,
                workOrderNo = _detail.workOrderNo ?? string.Empty,
                processCode = _detail.processCode,
                processName = _detail.processName,
                schemeNo = _detail.schemeNo,
                platPlanNo = _detail.platPlanNo,
                batchNo = _detail.productionBatch,
                outputFrameList = result.frameNoList.Count > 0 ? result.frameNoList : null,
                barcodeScanRecordList = result.barcodeScanRecordList.Count > 0 ? result.barcodeScanRecordList : null
            };

            ApiResp<bool?> resp;
            try
            {
                resp = await _api.AddWorkProcessTaskProductOutputAsync(req);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Application.Current.MainPage.DisplayAlert("失败", $"提交产出异常：{ex.Message}", "OK");
                return;
            }

            if (!resp.success)
            {
                await Application.Current.MainPage.DisplayAlert("失败", resp.message ?? "提交失败", "OK");
                return;
            }

            ReturnResult(result);
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        public void SetResultTcs(TaskCompletionSource<OutputPopupResult?> tcs) => _tcs = tcs;
        private void ReturnResult(OutputPopupResult? result) => _tcs?.TrySetResult(result);

        [RelayCommand]
        private async Task Cancel()
        {
            ReturnResult(null);
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }

    public record BarcodeScanDisplayItem(int Index, BarcodeScanRecord Record)
    {
        public string Barcode => Record.barcode;
        public string ScanTime => Record.scanTime;
    }

    public class FrameOptionItem
    {
        public string FrameNo { get; set; } = string.Empty;
        public string FrameStatus { get; set; } = string.Empty;
        public decimal MaxLimit { get; set; }
        public decimal MinLimit { get; set; }
        public string DisplayText => string.IsNullOrWhiteSpace(FrameStatus) ? FrameNo : $"{FrameNo}（{FrameStatus}）";
    }
}
