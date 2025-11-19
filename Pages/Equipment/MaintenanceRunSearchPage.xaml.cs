using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MaintenanceRunSearchPage : ContentPage
{
    private readonly MaintenanceRunSearchViewModel _vm;

    public MaintenanceRunSearchPage(MaintenanceRunSearchViewModel vm, ScanService scanSvc)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        QualityNoEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    // 新增：扫码按钮事件
    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        // 等待扫码结果
        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 回填到输入框
        QualityNoEntry.Text = result.Trim();

        // 同步到 ViewModel
        if (BindingContext is MaintenanceRunSearchViewModel vm)
        {
            vm.Keyword = result.Trim();

            // 可选：扫码后自动触发查询
            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);
        }
    }
}
