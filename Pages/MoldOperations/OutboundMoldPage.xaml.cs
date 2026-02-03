using IndustrialControlMAUI.ViewModels;
using ZXing.Net.Maui.Controls;

namespace IndustrialControlMAUI.Pages;
[QueryProperty(nameof(WorkOrderNo), "workOrderNo")]
public partial class OutboundMoldPage : ContentPage
{
    public readonly OutboundMoldViewModel _vm;
    public string? WorkOrderNo { get; set; }
    CancellationTokenSource? _lifecycleCts;
    private bool _loadedOnce = false;
    /// <summary>执行 OutboundMoldPage 初始化逻辑。</summary>
    public OutboundMoldPage(OutboundMoldViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        BindingContext = _vm;
    }

    // 不在此处取消 CTS，交给 OnDisappearing 处理
    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loadedOnce) return;
        _loadedOnce = true;

        _lifecycleCts = new CancellationTokenSource();
        _vm.SetLifecycleToken(_lifecycleCts.Token);

        if (!string.IsNullOrWhiteSpace(WorkOrderNo))
            await _vm.LoadAsync(WorkOrderNo);

        ScanEntry?.Focus();
    }

    /// <summary>执行 OnDisappearing 逻辑。</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // 需要取消并释放页面级 CTS
    }



    bool _submitting = false;

    /// <summary>执行 ScanEntry_Completed 逻辑。</summary>
    private async void ScanEntry_Completed(object? sender, EventArgs e)
    {
        if (_submitting) return;
        _submitting = true;
        try
        {
            if (_vm.ScanSubmitCommand.CanExecute(null))
                await _vm.ScanSubmitCommand.ExecuteAsync(null); //  await 异步执行
        }
        finally
        {
            _submitting = false;
            await Task.Delay(30);
            ScanEntry.Text = string.Empty;
            ScanEntry?.Focus(); // 扫码后重新聚焦
        }
    }
    // 扫码按钮点击
    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        try
        {
            var tcs = new TaskCompletionSource<string>();
            await Navigation.PushAsync(new QrScanPage(tcs));

            var result = await tcs.Task;
            if (string.IsNullOrWhiteSpace(result))
                return;

            _vm.ScanCode = result.Trim();

            if (_vm.ScanSubmitCommand.CanExecute(null))
                await _vm.ScanSubmitCommand.ExecuteAsync(null);

            ScanEntry.Text = string.Empty;
            ScanEntry.Focus();
        }
        catch (Exception ex)
        {
            await DisplayAlert("", $"扫码异常{ex.Message}", "知道了");
        }
    }

}
