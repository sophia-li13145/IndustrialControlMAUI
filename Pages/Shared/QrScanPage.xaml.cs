using ZXing.Net.Maui;                     
using SkiaSharp;
using BarcodeFormat = ZXing.BarcodeFormat;                       

namespace IndustrialControlMAUI.Pages;

public partial class QrScanPage : ContentPage
{
    private readonly TaskCompletionSource<string> _tcs;
    private bool _returned;
    private bool _isPickingImage;
    private bool _isBarcodeHandlerAttached;
    /// <summary>执行 QrScanPage 初始化逻辑。</summary>
    public QrScanPage(TaskCompletionSource<string> tcs)
    {
        InitializeComponent();
        _tcs = tcs;

        // 扫码默认使用后置摄像头；如当前设备没有可用后置摄像头，可通过“切换摄像头”手动切到前置/虚拟摄像头。
        barcodeView.CameraLocation = CameraLocation.Rear;

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
        MainThread.BeginInvokeOnMainThread(() => CompleteScanAsync(first.Value.Trim()));
    }

    private async void CompleteScanAsync(string result)
    {
        _returned = true;
        StopDetectingAndUnsubscribe();
        _tcs.TrySetResult(result);
        await Navigation.PopAsync();
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

            var pick = await PickGalleryImageAsync();

            if (pick is null)
            {
                RestartDetectingAfterImagePick();
                return;
            }

            ResultLabel.Text = "正在识别图片...";
            await using var stream = await pick.OpenReadAsync();
            using var skBitmap = DecodeBitmap(stream);
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

            StopDetectingAndUnsubscribe();
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

    private static async Task<FileResult?> PickGalleryImageAsync()
    {
        try
        {
            // Android 模拟器里的“Photos/相册”图片通过 MediaPicker 读取最稳定；失败时再退回文件选择器。
            return await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "选择包含二维码/条码的图片"
            });
        }
        catch (FeatureNotSupportedException)
        {
            return await PickImageWithFilePickerAsync();
        }
        catch (PermissionException)
        {
            return await PickImageWithFilePickerAsync();
        }
    }

    private static Task<FileResult?> PickImageWithFilePickerAsync()
    {
        return FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "选择包含二维码/条码的图片",
            FileTypes = FilePickerFileType.Images
        });
    }

    /// <summary>
    /// 统一的 ZXing 解码逻辑。相册图片常见问题包括：没有二维码留白、透明底、尺寸过小、
    /// 模拟器导入后压缩/发灰、深色模式反色等，所以需要按多种预处理方式重试。
    /// </summary>
    private ZXing.Result? DecodeWithZxing(SKBitmap bitmap)
    {
        var reader = CreateImageBarcodeReader();
        using var normalized = NormalizeBitmap(bitmap);

        foreach (var candidate in CreateDecodeCandidates(normalized))
        {
            using (candidate)
            {
                var result = reader.Decode(candidate);
                if (result is not null) return result;
            }
        }

        return null;
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

    private static SKBitmap? DecodeBitmap(Stream stream)
    {
        using var codec = SKCodec.Create(stream);
        if (codec is null) return null;

        var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null) return null;

        return ApplyCodecOrigin(bitmap, codec.EncodedOrigin);
    }

    private static SKBitmap ApplyCodecOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft)
        {
            return source;
        }

        var swapsSize = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsSize ? source.Height : source.Width;
        var height = swapsSize ? source.Width : source.Height;
        var rotated = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque));

        using (var canvas = new SKCanvas(rotated))
        {
            canvas.Clear(SKColors.White);

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    canvas.Scale(-1, 1, width / 2f, height / 2f);
                    break;
                case SKEncodedOrigin.BottomRight:
                    canvas.RotateDegrees(180, width / 2f, height / 2f);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Scale(1, -1, width / 2f, height / 2f);
                    break;
                case SKEncodedOrigin.LeftTop:
                    canvas.RotateDegrees(90);
                    canvas.Scale(1, -1);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    canvas.Translate(width, height);
                    canvas.RotateDegrees(90);
                    canvas.Scale(-1, 1);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, height);
                    canvas.RotateDegrees(270);
                    break;
            }

            canvas.DrawBitmap(source, 0, 0);
            canvas.Flush();
        }

        source.Dispose();
        return rotated;
    }

    private static IEnumerable<SKBitmap> CreateDecodeCandidates(SKBitmap source)
    {
        yield return source.Copy();

        using var padded = AddQuietZone(source);
        yield return padded.Copy();

        yield return ToGrayscaleBitmap(source);
        yield return ToHighContrastBitmap(source);
        yield return ToGrayscaleBitmap(padded);
        yield return ToHighContrastBitmap(padded);

        foreach (var scale in new[] { 1.5f, 2.5f, 4f })
        {
            using var resized = ResizeBitmap(padded, scale);
            yield return resized.Copy();
            yield return ToGrayscaleBitmap(resized);
            yield return ToHighContrastBitmap(resized);
        }
    }

    private static SKBitmap NormalizeBitmap(SKBitmap source)
    {
        var normalized = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Opaque));

        using var canvas = new SKCanvas(normalized);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();

        return normalized;
    }

    private static SKBitmap AddQuietZone(SKBitmap source)
    {
        // 二维码边缘没有白色 quiet zone 时，ZXing 很容易定位失败；相册截图/裁剪图尤其常见。
        var padding = Math.Max(32, (int)Math.Round(Math.Min(source.Width, source.Height) * 0.12));
        var padded = new SKBitmap(new SKImageInfo(source.Width + padding * 2, source.Height + padding * 2, SKColorType.Bgra8888, SKAlphaType.Opaque));

        using var canvas = new SKCanvas(padded);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, padding, padding);
        canvas.Flush();

        return padded;
    }

    private static SKBitmap ToHighContrastBitmap(SKBitmap source)
    {
        var highContrast = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Opaque));

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetPixel(x, y);
                var luminance = (color.Red * 299 + color.Green * 587 + color.Blue * 114) / 1000;
                highContrast.SetPixel(x, y, luminance < 160 ? SKColors.Black : SKColors.White);
            }
        }

        return highContrast;
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
            : "当前为前置/虚拟摄像头，请对准二维码...";
        try { barcodeView.IsDetecting = true; } catch { }
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isPickingImage) return;

        // 动态请求相机权限；即使拒绝，也保留页面让用户可以继续使用“相册识别”。
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            ResultLabel.Text = "未授予相机权限，可点击相册识别二维码/条码";
            try { barcodeView.IsDetecting = false; } catch { }
            return;
        }

        ResultLabel.Text = barcodeView.CameraLocation == CameraLocation.Rear
            ? "请对准二维码..."
            : "当前为前置/虚拟摄像头，请对准二维码...";
        SubscribeBarcodeHandler();
        barcodeView.IsDetecting = true;
    }

    /// <summary>执行 OnDisappearing 逻辑。</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        StopDetectingAndUnsubscribe();

        // Android 打开系统相册/文件选择器时页面可能触发 OnDisappearing；此时不能提前返回空扫码结果。
        if (_isPickingImage) return;

        if (!_returned)
        {
            _returned = true;
            _tcs.TrySetResult(string.Empty);
        }
    }

    private void SubscribeBarcodeHandler()
    {
        if (_isBarcodeHandlerAttached || barcodeView == null) return;

        barcodeView.BarcodesDetected += BarcodesDetected;
        _isBarcodeHandlerAttached = true;
    }

    private void StopDetectingAndUnsubscribe()
    {
        if (barcodeView == null) return;

        try { barcodeView.IsDetecting = false; } catch { }

        if (!_isBarcodeHandlerAttached) return;

        barcodeView.BarcodesDetected -= BarcodesDetected;
        _isBarcodeHandlerAttached = false;
    }

}
