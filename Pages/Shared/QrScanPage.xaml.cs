using ZXing.Net.Maui;                     
using SkiaSharp;
using Microsoft.Maui.Devices;
using BarcodeFormat = ZXing.BarcodeFormat;                       

namespace IndustrialControlMAUI.Pages;

public partial class QrScanPage : ContentPage
{
    private readonly TaskCompletionSource<string> _tcs;
    private bool _returned;
    private bool _isPickingImage;
    /// <summary>执行 QrScanPage 初始化逻辑。</summary>
    public QrScanPage(TaskCompletionSource<string> tcs)
    {
        InitializeComponent();
        _tcs = tcs;

        // 模拟器通常只配置前置/虚拟摄像头；手持机默认使用后置摄像头。
        barcodeView.CameraLocation = DeviceInfo.DeviceType == DeviceType.Virtual
            ? CameraLocation.Front
            : CameraLocation.Rear;

        // 直接在这里设置一次就够了
        barcodeView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }


    // 扫码事件
    /// <summary>执行 BarcodesDetected 逻辑。</summary>
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
    /// <summary>执行 PickFromGalleryButton_Clicked 逻辑。</summary>
    private async void PickFromGalleryButton_Clicked(object? sender, EventArgs e)
    {
        if (_returned || _isPickingImage) return;

        _isPickingImage = true;
        try
        {
            try { barcodeView.IsDetecting = false; } catch { }
            ResultLabel.Text = "正在打开相册...";

            var pick = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择包含二维码/条码的图片",
                FileTypes = FilePickerFileType.Images
            });

            if (pick is null)
            {
                RestartDetectingAfterImagePick();
                return;
            }

            ResultLabel.Text = "正在识别图片...";
            await using var stream = await pick.OpenReadAsync();
            using var skBitmap = SKBitmap.Decode(stream);
            if (skBitmap is null)
            {
                await DisplayAlert("提示", "无法读取该图片。", "确定");
                RestartDetectingAfterImagePick();
                return;
            }

            var result = DecodeWithZxing(skBitmap);
            if (result is null || string.IsNullOrWhiteSpace(result.Text))
            {
                await DisplayAlert(
                    "提示",
                    $"未识别到条码。\n原图: {skBitmap.Width}x{skBitmap.Height}\n请确认图片里的二维码/条码清晰且四周留有空白边距。",
                    "确定");
                RestartDetectingAfterImagePick();
                return;
            }

            if (_returned) return;
            _returned = true;

            _tcs.TrySetResult(result.Text.Trim());
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"识别失败：{ex.Message}", "确定");
            RestartDetectingAfterImagePick();
        }
        finally
        {
            _isPickingImage = false;
        }
    }

    /// <summary>
    /// 统一的 ZXing 解码逻辑（原图和放大图都走它）
    /// </summary>
    private ZXing.Result? DecodeWithZxing(SKBitmap bitmap)
    {
        var reader = CreateImageBarcodeReader();

        // 相册图片可能是小图、深色码、透明底或模拟器导入后被压缩；多种预处理可以明显提升识别率。
        var direct = reader.Decode(bitmap);
        if (direct is not null) return direct;

        foreach (var scale in new[] { 1.5f, 2.5f, 4f })
        {
            using var resized = ResizeBitmap(bitmap, scale);
            var resizedResult = reader.Decode(resized);
            if (resizedResult is not null) return resizedResult;

            using var grayscale = ToGrayscaleBitmap(resized);
            var grayscaleResult = reader.Decode(grayscale);
            if (grayscaleResult is not null) return grayscaleResult;
        }

        using var originalGrayscale = ToGrayscaleBitmap(bitmap);
        return reader.Decode(originalGrayscale);
    }

    private static ZXing.SkiaSharp.BarcodeReader CreateImageBarcodeReader()
    {
        var options = new ZXing.Common.DecodingOptions
        {
            TryHarder = true,
            TryInverted = true,
            PossibleFormats = new[]
            {
                BarcodeFormat.QR_CODE,
                BarcodeFormat.DATA_MATRIX,
                BarcodeFormat.CODE_128,
                BarcodeFormat.CODE_39,
                BarcodeFormat.EAN_13,
                BarcodeFormat.EAN_8,
                BarcodeFormat.ITF,
                BarcodeFormat.UPC_A,
                BarcodeFormat.UPC_E
            }
        };

        return new ZXing.SkiaSharp.BarcodeReader
        {
            AutoRotate = true,
            Options = options
        };
    }

    private static SKBitmap ResizeBitmap(SKBitmap source, float scale)
    {
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new SKBitmap(new SKImageInfo(width, height));

        using var canvas = new SKCanvas(resized);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source,
            new SKRect(0, 0, source.Width, source.Height),
            new SKRect(0, 0, width, height));
        canvas.Flush();

        return resized;
    }

    private static SKBitmap ToGrayscaleBitmap(SKBitmap source)
    {
        var grayscale = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Opaque));

        using var canvas = new SKCanvas(grayscale);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new[]
            {
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0,      0,      0,      1, 0
            })
        };

        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();

        return grayscale;
    }



    // 前后摄像头切换
    /// <summary>执行 SwitchCameraButton_Clicked 逻辑。</summary>
    private void SwitchCameraButton_Clicked(object sender, EventArgs e)
    {
        var wasDetecting = barcodeView.IsDetecting;
        barcodeView.IsDetecting = false;
        barcodeView.CameraLocation =
            barcodeView.CameraLocation == CameraLocation.Rear
            ? CameraLocation.Front
            : CameraLocation.Rear;
        ResultLabel.Text = barcodeView.CameraLocation == CameraLocation.Rear
            ? "已切换到后置摄像头"
            : "已切换到前置/虚拟摄像头";
        barcodeView.IsDetecting = wasDetecting;
    }

    // 手电筒开关
    /// <summary>执行 TorchButton_Clicked 逻辑。</summary>
    private void TorchButton_Clicked(object sender, EventArgs e)
    {
        barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
    }

    private void RestartDetectingAfterImagePick()
    {
        _isPickingImage = false;
        ResultLabel.Text = barcodeView.CameraLocation == CameraLocation.Rear
            ? "请对准二维码..."
            : "模拟器已默认使用前置/虚拟摄像头，请对准二维码...";
        try { barcodeView.IsDetecting = true; } catch { }
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isPickingImage) return;

        // ✅ 动态请求相机权限（防止直接闪退）
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            _returned = true;
            _tcs.TrySetResult(string.Empty);
            await DisplayAlert("提示", "未授予相机权限，无法使用扫码功能。", "确定");
            await Navigation.PopAsync();
            return;
        }
        ResultLabel.Text = barcodeView.CameraLocation == CameraLocation.Rear
            ? "请对准二维码..."
            : "模拟器已默认使用前置/虚拟摄像头，请对准二维码...";
        barcodeView.IsDetecting = true;
    }

    /// <summary>执行 OnDisappearing 逻辑。</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // ✅ 防御性判断，防止闪退
        if (barcodeView != null)
        {
            barcodeView.IsDetecting = false;
        }

        // Android 打开系统相册/文件选择器时页面可能触发 OnDisappearing；此时不能提前返回空扫码结果。
        if (_isPickingImage) return;

        if (!_returned)
        {
            _returned = true;
            _tcs.TrySetResult(string.Empty);
        }
    }

}
