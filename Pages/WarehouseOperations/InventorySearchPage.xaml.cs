using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages
{
    public partial class InventorySearchPage : ContentPage
    {
        private readonly InventorySearchViewModel _vm;
        private readonly IServiceProvider _sp;

        /// <summary>执行 InventorySearchPage 初始化逻辑。</summary>
        public InventorySearchPage(IServiceProvider sp, InventorySearchViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _sp = sp;
        }

        /// <summary>执行 OnAppearing 逻辑。</summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            ScanEntry?.Focus();
        }

        /// <summary>执行 OnDisappearing 逻辑。</summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }


        /// <summary>执行 OnScanCompleted 逻辑。</summary>
        private void OnScanCompleted(object sender, EventArgs e)
        {
            _vm.ScanSubmitCommand.Execute(null);
        }

        private bool _isScanning;

        /// <summary>执行 OnScanClicked 逻辑。</summary>
        private async void OnScanClicked(object sender, EventArgs e)
        {
            if (_isScanning) return;   // 防止连点
            _isScanning = true;

            try
            {
                var tcs = new TaskCompletionSource<string>();
                await Navigation.PushAsync(new QrScanPage(tcs));

                string result = null;

                try
                {
                    // 如果用户取消、扫码页中途异常，不会卡死
                    result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
                catch (TimeoutException)
                {
                    await DisplayAlert("提示", "扫码超时，请重新扫描。", "确定");
                    return;
                }
                catch (TaskCanceledException)
                {
                    // 扫码页主动取消，不作为错误处理
                    return;
                }

                if (string.IsNullOrWhiteSpace(result))
                    return;

                // 回填到输入框
                ScanEntry.Text = result.Trim();

                // 交给 VM 查询第一页
                if (BindingContext is InventorySearchViewModel vm)
                {
                    await vm.QueryInventoryAsync(result.Trim());
                }

                // 清空并聚焦
                ScanEntry.Text = string.Empty;
                ScanEntry.Focus();
            }
            finally
            {
                _isScanning = false;
            }
        }

    }
}
