using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    public partial class OutputPopupViewModel : ObservableObject
    {
        private readonly IWorkOrderApi? _api;
        public ObservableCollection<TaskMaterialOutput> MaterialOptions { get; } = new();
        public ObservableCollection<FrameOptionItem> SelectedFrames { get; } = new();

        [ObservableProperty] private TaskMaterialOutput? selectedMaterial;
        [ObservableProperty] private string? quantityText;
        [ObservableProperty] private string? memo;
        [ObservableProperty] private bool isPickerEnabled = true;
        [ObservableProperty] private string frameHint = "提示：该料框最大容量5件";
        private TaskCompletionSource<OutputPopupResult?>? _tcs;

        public OutputPopupViewModel(IWorkOrderApi? api = null) => _api = api;

        public void Init(IEnumerable<TaskMaterialOutput> materialOutputList, TaskMaterialOutput? presetMaterialCode = null)
        {
            MaterialOptions.Clear();
            SelectedFrames.Clear();
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
            Memo = "";
            FrameHint = "提示：该料框最大容量5件";
        }

        private static bool IsSame(TaskMaterialOutput a, TaskMaterialOutput b)
            => string.Equals(a?.materialCode, b?.materialCode, StringComparison.OrdinalIgnoreCase);

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
            if (SelectedFrames.Count <= 0 || SelectedFrames.Count >= 6)
            {
                await Application.Current.MainPage.DisplayAlert("提示", "请选择1到5个料框。", "好的");
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
                frameNoList = SelectedFrames.Select((x, idx) => new OutputFrameSelectionItem { frameNo = x.FrameNo}).ToList()
            };

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

    public class FrameOptionItem
    {
        public string FrameNo { get; set; } = string.Empty;
        public string FrameStatus { get; set; } = string.Empty;
        public decimal MaxLimit { get; set; }
        public decimal MinLimit { get; set; }
        public string DisplayText => string.IsNullOrWhiteSpace(FrameStatus) ? FrameNo : $"{FrameNo}（{FrameStatus}）";
    }
}
