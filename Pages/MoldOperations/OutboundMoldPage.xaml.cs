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
    public OutboundMoldPage(OutboundMoldViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        BindingContext = _vm;
    }

    // 删掉 _lifecycleCts?.Cancel(); 和 Dispose(); —— 不要在 OnDisappearing 里动 CTS
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // 不要 Cancel/Dispose 页面级 CTS（避免返回瞬间崩溃）
    }



    bool _submitting = false;

    private async void ScanEntry_Completed(object? sender, EventArgs e)
    {
        if (_submitting) return;
        _submitting = true;
        try
        {
            if (_vm.ScanSubmitCommand.CanExecute(null))
                await _vm.ScanSubmitCommand.ExecuteAsync(null); // ★ await 异步命令
        }
        finally
        {
            _submitting = false;
            await Task.Delay(30);
            ScanEntry.Text = string.Empty;
            ScanEntry?.Focus(); // 连续扫描更顺手
        }
    }
    // 新增：扫码按钮事件
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
            await DisplayAlert("错误", $"扫码时出现异常：{ex.Message}", "知道了");
        }
    }

}
