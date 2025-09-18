namespace IndustrialControlMAUI.Pages
{
    public partial class LogsPage : ContentPage
    {
        public LogsPage(ViewModels.LogsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ViewModels.LogsViewModel vm)
                vm.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is ViewModels.LogsViewModel vm)
                vm.OnDisappearing();
        }

        // 点击按钮时添加日志
        private void OnAddLogButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is ViewModels.LogsViewModel vm)
            {
                vm.AddLog("这是一条新增的日志");
            }
        }

        // 点击按钮时添加错误日志
        private void OnAddErrorLogButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is ViewModels.LogsViewModel vm)
            {
                vm.AddErrorLog(new Exception("这是一条错误日志"));
            }
        }
    }
}
