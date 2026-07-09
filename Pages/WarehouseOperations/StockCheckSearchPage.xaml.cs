using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class StockCheckSearchPage : ContentPage
{
    private readonly StockCheckSearchViewModel _vm;

    /// <summary>执行 StockCheckSearchPage 初始化逻辑。</summary>
    public StockCheckSearchPage(StockCheckSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>执行 OnScanClicked 逻辑。</summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 扫码查询
        OrderEntry.Text = result.Trim();
        OrderEntry?.Focus();
        _vm.SearchCheckNo = result.Trim();

        // 重新加载第一页 
        await _vm.SearchAsync();

        // 扫码后保留结果并重新聚焦，方便连续扫码时输入框状态不丢失
        OrderEntry.Focus();
    }



    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OrderEntry.Focus();
        await _vm.SearchAsync();

    }
}
