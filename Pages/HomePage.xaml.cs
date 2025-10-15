using Microsoft.Maui.Controls;

namespace IndustrialControlMAUI.Pages
{
    public partial class HomePage : ContentPage
    {
        public HomePage() => InitializeComponent();

        // —— 最近使用 / 生产作业 示例 —— 
        async void GotoTaskExecute(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(InboundProductionSearchPage));
        async void GotoProcParam(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(InboundProductionPage));
        async void GotoOrderQuery(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(WorkOrderSearchPage));
        private async void OnInProcess(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(ProcessTaskSearchPage));             //工序任务执行

        // —— 仓储作业（与你发来的函数一一对应）——
        private async void OnInMat(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(InboundMaterialSearchPage));   // 物料入库
        private async void OnInProd(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(InboundProductionSearchPage)); // 生产入库
        private async void OnOutMat(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(OutboundMaterialSearchPage));  // 物料出库
        private async void OnOutFinished(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(OutboundFinishedSearchPage));  // 成品出库接货
        private async void OnMoldOut(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(OutboundMoldSearchPage));      // 模具出库管理
        private async void OnMoldIn(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(InboundMoldPage));             // 模具入库管理

        private async void OnIPQC(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(ProcessQualitySearchPage));      // 模具出库管理
        //private async void OnFQC(object? s, TappedEventArgs e)
        //    => await Shell.Current.GoToAsync(nameof(ProcessQualityDetailPage));             // 模具入库管理

        // —— 质检作业（按需替换为真实页面）——
        private async void GotoIQC(object? s, TappedEventArgs e)
            => await DisplayAlert("IQC", "待接入页面", "确定");
        private async void GotoIPQC(object? s, TappedEventArgs e)
            => await DisplayAlert("IPQC", "待接入页面", "确定");
        private async void GotoFQC(object? s, TappedEventArgs e)
            => await DisplayAlert("FQC", "待接入页面", "确定");
        private async void GotoOQC(object? s, TappedEventArgs e)
            => await DisplayAlert("OQC", "待接入页面", "确定");
        // ✅ 退出登录
        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            await TokenStorage.ClearAsync();   // 清除 token
            ApiClient.SetBearer(null);         // 清空请求头

            // 切换到未登录的 Shell：显示 登录｜日志｜管理员
            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.SwitchToLoggedOutShell();
            });
        }
    }
}
