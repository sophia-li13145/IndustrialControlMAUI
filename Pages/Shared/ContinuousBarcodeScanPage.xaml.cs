using IndustrialControlMAUI.Models;
using ZXing.Net.Maui;

namespace IndustrialControlMAUI.Pages;

public partial class ContinuousBarcodeScanPage : ContentPage
{
    private readonly Func<string, Task<BarcodeScanFeedback>> _scanHandler;
    private bool _isBarcodeHandlerAttached;
    private bool _isProcessingBarcode;
    private bool _isClosed;
    private int _successCount;

    public ContinuousBarcodeScanPage(Func<string, Task<BarcodeScanFeedback>> scanHandler)
    {
        InitializeComponent();
        _scanHandler = scanHandler;

        barcodeView.CameraLocation = CameraLocation.Rear;
        barcodeView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isClosed || _isProcessingBarcode) return;

        var first = e.Results.FirstOrDefault();
        if (first is null || string.IsNullOrWhiteSpace(first.Value)) return;

        _ = MainThread.InvokeOnMainThreadAsync(() => HandleBarcodeAsync(first.Value.Trim()));
    }

    private async Task HandleBarcodeAsync(string barcode)
    {
        if (_isClosed || _isProcessingBarcode || string.IsNullOrWhiteSpace(barcode)) return;

        _isProcessingBarcode = true;
        try
        {
            try { barcodeView.IsDetecting = false; } catch { }
            ResultLabel.Text = "正在校验条码...";

            var feedback = await _scanHandler(barcode.Trim());
            if (feedback.Success)
            {
                _successCount++;
                CountLabel.Text = $"已通过：{_successCount}";
            }

            ResultLabel.Text = string.IsNullOrWhiteSpace(feedback.Message)
                ? (feedback.Success ? "校验通过，请继续扫码" : "校验失败，请继续扫码")
                : feedback.Message;
        }
        finally
        {
            _isProcessingBarcode = false;
            if (!_isClosed)
                try { barcodeView.IsDetecting = true; } catch { }
            HardwareScanEntry.Focus();
        }
    }

    private async void HardwareScanEntry_Completed(object? sender, EventArgs e)
    {
        if (_isProcessingBarcode) return;

        var barcode = HardwareScanEntry.Text?.Trim();
        HardwareScanEntry.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(barcode)) return;

        await HandleBarcodeAsync(barcode);
    }

    private async void DoneButton_Clicked(object sender, EventArgs e)
    {
        _isClosed = true;
        StopDetectingAndUnsubscribe();
        await Navigation.PopAsync();
    }

    private async void PickFromGalleryButton_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert("提示", "连续扫码页请使用摄像头或扫码枪扫描产出物料条码。", "好的");
    }

    private void SwitchCameraButton_Clicked(object sender, EventArgs e)
    {
        var wasDetecting = barcodeView.IsDetecting;
        barcodeView.IsDetecting = false;
        barcodeView.CameraLocation = barcodeView.CameraLocation == CameraLocation.Rear
            ? CameraLocation.Front
            : CameraLocation.Rear;
        ResultLabel.Text = barcodeView.CameraLocation == CameraLocation.Rear
            ? "已切换到后置摄像头"
            : "已切换到前置/虚拟摄像头";
        barcodeView.IsDetecting = wasDetecting && !_isProcessingBarcode;
    }

    private void TorchButton_Clicked(object sender, EventArgs e)
    {
        barcodeView.IsTorchOn = !barcodeView.IsTorchOn;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            ResultLabel.Text = "未授予相机权限，可使用扫码枪输入";
            try { barcodeView.IsDetecting = false; } catch { }
            HardwareScanEntry.Focus();
            return;
        }

        ResultLabel.Text = "请对准产出物料条码...";
        SubscribeBarcodeHandler();
        barcodeView.IsDetecting = true;
        HardwareScanEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopDetectingAndUnsubscribe();
    }

    private void SubscribeBarcodeHandler()
    {
        if (_isBarcodeHandlerAttached || barcodeView is null) return;

        barcodeView.BarcodesDetected += BarcodesDetected;
        _isBarcodeHandlerAttached = true;
    }

    private void StopDetectingAndUnsubscribe()
    {
        if (barcodeView is null) return;

        try { barcodeView.IsDetecting = false; } catch { }

        if (!_isBarcodeHandlerAttached) return;

        barcodeView.BarcodesDetected -= BarcodesDetected;
        _isBarcodeHandlerAttached = false;
    }
}
