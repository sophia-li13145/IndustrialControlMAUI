namespace IndustrialControlMAUI.Pages
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        // 点“文字”也能切换复选框
        void OnTapToggle(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is CheckBox cb)
                cb.IsChecked = !cb.IsChecked;
        }

        // 勾选即跳转，返回后复位（保持你原有页面名）
        private async void OnInMat(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(InboundMaterialSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }

        private async void OnInProd(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(InboundProductionSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }

        private async void OnOutMat(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(OutboundMaterialSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }

        private async void OnOutFinished(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(OutboundFinishedSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }
        private async void OnMoldIn(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(InboundMoldPage));
            ((CheckBox)sender).IsChecked = false;
        }
        private async void OnMoldOut(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(OutboundMoldSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }
        private async void OnOrderQry(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;
            await Shell.Current.GoToAsync(nameof(WorkOrderSearchPage));
            ((CheckBox)sender).IsChecked = false;
        }

        // 新增：退出登录
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
