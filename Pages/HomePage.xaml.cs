using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
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
            => await Shell.Current.GoToAsync(nameof(ProcessQualitySearchPage));      
        private async void OnFQC(object? s, TappedEventArgs e)
            => await Shell.Current.GoToAsync(nameof(FinishedQualitySearchPage));             

        // —— 质检作业（按需替换为真实页面）——
        private async void GotoIQC(object? s, TappedEventArgs e)
            => await DisplayAlert("IQC", "待接入页面", "确定");
        private async void GotoIPQC(object? s, TappedEventArgs e)
            => await DisplayAlert("IPQC", "待接入页面", "确定");
        private async void GotoFQC(object? s, TappedEventArgs e)
            => await DisplayAlert("FQC", "待接入页面", "确定");
        private async void GotoOQC(object? s, TappedEventArgs e)
            => await DisplayAlert("OQC", "待接入页面", "确定");

        private async void OnInSpectionSearch(object? s, TappedEventArgs e)
           => await Shell.Current.GoToAsync(nameof(InspectionSearchPage));

        private async void OnMaintenanceSearch(object? s, TappedEventArgs e)
          => await Shell.Current.GoToAsync(nameof(MaintenanceSearchPage));

        private async void OnRepairSearch(object? s, TappedEventArgs e)
          => await Shell.Current.GoToAsync(nameof(RepairSearchPage));

        // —— 能源：手动抄表 ——（与首页其它方法同风格）
        private async void OnEnergyManualRead(object? s, TappedEventArgs e)
        {
            // 弹出“仪表选择”弹窗
            var popup = new MeterSelectPopup(
                Handler!.MauiContext!.Services.GetRequiredService<MeterSelectViewModel>());

            var result = await this.ShowPopupAsync(popup);

            // 选中后跳转到抄表页并回填
            if (result is EnergyMeterUiRow row)
            {
                await Shell.Current.GoToAsync(nameof(ManualReadingPage),
                    new Dictionary<string, object> { ["meter"] = row });
            }
        }
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
