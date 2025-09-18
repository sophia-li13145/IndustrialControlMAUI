using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages
{
    public partial class InboundMoldPage : ContentPage
    {
        private readonly ScanService _scanSvc;
        private readonly InboundMoldViewModel _vm;

        public InboundMoldPage(InboundMoldViewModel vm, ScanService scanSvc)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _scanSvc = scanSvc;

            // 扫描配置：与你现有页面保持一致（前后缀/去抖等）
            _scanSvc.Prefix = null;
            _scanSvc.Suffix = null;
            _scanSvc.DebounceMs = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // 开始监听扫描，并把输入焦点指到文本框
            _scanSvc.Scanned += OnScanned;
            _scanSvc.StartListening();
            _scanSvc.Attach(ScanEntry);
            ScanEntry?.Focus();
        }

        protected override void OnDisappearing()
        {
            // 退出页面即注销，避免多个页面抢占
            _scanSvc.Scanned -= OnScanned;
            _scanSvc.StopListening();
            base.OnDisappearing();
        }

        private void OnScanned(string data, string type)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _vm.ScanCode = data?.Trim();
                await _vm.HandleScannedAsync(data, type);  // 交给 VM：查询状态/入列表或提示在库
            });
        }
        private void OnRowCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return; // 只在勾上时选中
            if (sender is CheckBox cb && cb.BindingContext is MoldScanRow row)
            {
                _vm.SelectedRow = row;  // 触发 CollectionView 的选中高亮
            }
        }

    }
}
