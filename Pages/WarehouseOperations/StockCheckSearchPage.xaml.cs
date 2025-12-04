using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class StockCheckSearchPage : ContentPage
{
    private readonly StockCheckSearchViewModel _vm;

    public StockCheckSearchPage(StockCheckSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        // 等待扫码结果
        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 同步到 ViewModel
        if (BindingContext is InventorySearchViewModel vm)
        {
            // 交给 VM 统一处理（第二个参数随意标记来源）
            await _vm.SearchAsync();

            // 清空并继续聚焦，方便下一次输入/扫码
            OrderEntry.Text = string.Empty;
            OrderEntry.Focus();
        }
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OrderEntry.Focus();
        await _vm.SearchAsync();

    }
}
