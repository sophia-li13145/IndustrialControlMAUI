using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages
{
    public partial class InboundMoldPage : ContentPage
    {
        private readonly InboundMoldViewModel _vm;
        private readonly IServiceProvider _sp;

        /// <summary>执行 InboundMoldPage 初始化逻辑。</summary>
        public InboundMoldPage(IServiceProvider sp, InboundMoldViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _sp = sp;
            vm.PickLocationAsync = () => WarehouseLocationPickerPage.ShowAsync(sp, this);
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

        /// <summary>执行 OnRowCheckedChanged 逻辑。</summary>
        private void OnRowCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return; // 只在勾上时选中
            if (sender is CheckBox cb && cb.BindingContext is MoldScanRow row)
            {
                _vm.SelectedRow = row;  // 触发 CollectionView 的选中高亮
            }
        }

        /// <summary>执行 OnScanCompleted 逻辑。</summary>
        private void OnScanCompleted(object sender, EventArgs e)
        {
            _vm.ScanSubmitCommand.Execute(null);
        }

        /// <summary>执行 OnScanClicked 逻辑。</summary>
        private async void OnScanClicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<string>();
            await Navigation.PushAsync(new QrScanPage(tcs));

            // 等待扫码结果
            var result = await tcs.Task;
            if (string.IsNullOrWhiteSpace(result))
                return;

            // 回填到输入框
            ScanEntry.Text = result.Trim();

            // 同步到 ViewModel
            if (BindingContext is InboundMoldViewModel vm)
            {
                // 交给 VM 统一处理（第二个参数随意标记来源）
                await _vm.HandleScannedAsync(ScanEntry.Text!, "KEYBOARD");

                // 清空并继续聚焦，方便下一次输入/扫码
                ScanEntry.Text = string.Empty;
                ScanEntry.Focus();
            }
        }
    }
}
