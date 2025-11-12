using ZXing.Net.Maui;                     
using SkiaSharp;
using BarcodeFormat = ZXing.BarcodeFormat;                       

namespace IndustrialControlMAUI.Pages;

public partial class QrScanPage : ContentPage
{
    private readonly TaskCompletionSource<string> _tcs;
    private bool _returned;
    public QrScanPage(TaskCompletionSource<string> tcs)
    {
        InitializeComponent();
        _tcs = tcs;
        // 🔴 关键：在 Handler 映射之前就让 Options != null
        // 关键：注册 HandlerChanging（在 SetHandler/MapOptions 之前触发）
        barcodeView.HandlerChanging += (s, e) =>
        {
            if (barcodeView.Options is null)
            {
                barcodeView.Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.All,
                    AutoRotate = true,
                    Multiple = false
                };
            }
        };
    }

    // 扫码事件
    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_returned) return; // 防止重复触发

        var first = e.Results.FirstOrDefault();
        if (first == null || string.IsNullOrWhiteSpace(first.Value))
            return;

        _returned = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { barcodeView.IsDetecting = false; } catch { }
            _tcs.TrySetResult(first.Value.Trim());
            await Navigation.PopAsync();
        });
    }

    // 新增：从相册选择图片并识别
    private async void PickFromGalleryButton_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // 可选：暂停取景器识别，避免前台还在扫到结果引发重复返回
            try { barcodeView.IsDetecting = false; } catch { }

            var pick = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择包含二维码/条码的图片",
                FileTypes = FilePickerFileType.Images
            });

            if (pick is null)
            {
                // 用户取消
                try { barcodeView.IsDetecting = true; } catch { }
                return;
            }

            await using var stream = await pick.OpenReadAsync();

            // 用 SkiaSharp 解码为位图
            using var skBitmap = SKBitmap.Decode(stream);
            if (skBitmap is null)
            {
                await DisplayAlert("提示", "无法读取该图片。", "确定");
                try { barcodeView.IsDetecting = true; } catch { }
                return;
            }

            // ZXing 解码
            var reader = new ZXing.SkiaSharp.BarcodeReader
            {
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                            BarcodeFormat.QR_CODE,
                            BarcodeFormat.DATA_MATRIX,
                            BarcodeFormat.AZTEC,
                            BarcodeFormat.PDF_417,
                            BarcodeFormat.CODE_128, BarcodeFormat.CODE_39,
                            BarcodeFormat.EAN_13,  BarcodeFormat.EAN_8,
                            BarcodeFormat.ITF,     BarcodeFormat.UPC_A,
                            BarcodeFormat.UPC_E
                        }
                }
            };

            var result = reader.Decode(skBitmap);

            if (result is null || string.IsNullOrWhiteSpace(result.Text))
            {
                await DisplayAlert("提示", "未在图片中识别到二维码/条码。", "确定");
                try { barcodeView.IsDetecting = true; } catch { }
                return;
            }

            // 与摄像头识别一致：只返回一次结果并关闭页面
            if (_returned) return;
            _returned = true;

            _tcs.TrySetResult(result.Text.Trim());
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"识别失败：{ex.Message}", "确定");
            try { barcodeView.IsDetecting = true; } catch { }
        }
    }
    

    // 前后摄像头切换
    private void SwitchCameraButton_Clicked(object sender, EventArgs e)
    {
        barcodeView.CameraLocation =
            barcodeView.CameraLocation == CameraLocation.Rear
            ? CameraLocation.Front
            : CameraLocation.Rear;
    }

    // 手电筒开关
    private void TorchButton_Clicked(object sender, EventArgs e)
    {
        barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ 动态请求相机权限（防止直接闪退）
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("提示", "未授予相机权限，无法使用扫码功能。", "确定");
            await Navigation.PopAsync();
            return;
        }
        barcodeView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // ✅ 防御性判断，防止闪退
        if (barcodeView != null)
        {
            barcodeView.IsDetecting = false;
            barcodeView.Handler?.DisconnectHandler();
        }
    }

}
