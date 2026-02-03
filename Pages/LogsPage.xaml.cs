namespace IndustrialControlMAUI.Pages
{
    public partial class LogsPage : ContentPage
    {
        /// <summary>执行 LogsPage 初始化逻辑。</summary>
        public LogsPage(ViewModels.LogsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        /// <summary>执行 OnAppearing 逻辑。</summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ViewModels.LogsViewModel vm)
                vm.OnAppearing();
        }

        /// <summary>执行 OnDisappearing 逻辑。</summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is ViewModels.LogsViewModel vm)
                vm.OnDisappearing();
        }

        // 点击时添加日志
        /// <summary>执行 OnAddLogButtonClicked 逻辑。</summary>
        private void OnAddLogButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is ViewModels.LogsViewModel vm)
            {
                vm.AddLog("一条日志");
            }
        }

        // 点击时添加异常日志
        /// <summary>执行 OnAddErrorLogButtonClicked 逻辑。</summary>
        private void OnAddErrorLogButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is ViewModels.LogsViewModel vm)
            {
                vm.AddErrorLog(new Exception("一条日志"));
            }
        }
    }
}
