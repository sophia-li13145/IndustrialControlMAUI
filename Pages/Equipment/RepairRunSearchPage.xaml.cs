using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class RepairRunSearchPage : ContentPage
{
    private readonly RepairRunSearchViewModel _vm;

    /// <summary>执行 RepairRunSearchPage 初始化逻辑。</summary>
    public RepairRunSearchPage(RepairRunSearchViewModel vm, ScanService scanSvc)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        QualityNoEntry.Focus();
        await _vm.SearchAsync();
    }

    /// <summary>执行 OnDisappearing 逻辑。</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    // 扫码按钮点击
    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        // 等待扫码结果
        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 回填扫码结果
        QualityNoEntry.Text = result.Trim();

        // 同步 ViewModel
        if (BindingContext is RepairRunSearchViewModel vm)
        {
            vm.Keyword = result.Trim();

            // 使用扫码结果查询
            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);
        }
    }
}
