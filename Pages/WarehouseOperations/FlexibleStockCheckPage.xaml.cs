using IndustrialControlMAUI.ViewModels;
using IndustrialControlMAUI.Services;
using System.Linq;

namespace IndustrialControlMAUI.Pages
{
    public partial class FlexibleStockCheckPage : ContentPage
    {
        private readonly FlexibleStockCheckViewModel _vm;

        /// <summary>执行 FlexibleStockCheckPage 初始化逻辑。</summary>
        public FlexibleStockCheckPage(FlexibleStockCheckViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        /// <summary>执行 OnAppearing 逻辑。</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 初始化：加载盘点单与库位列表
            await _vm.InitialLoadAsync();

            LocationEntry?.Focus();
        }

        // ====== Entry Completed 触发查询 ======

        /// <summary>执行 OnLocationCompleted 逻辑。</summary>
        private void OnLocationCompleted(object sender, EventArgs e)
        {
            _vm.ScanLocationSubmitCommand.Execute(null);
        }

        /// <summary>执行 OnMaterialCompleted 逻辑。</summary>
        private void OnMaterialCompleted(object sender, EventArgs e)
        {
            _vm.ScanMaterialSubmitCommand.Execute(null);
        }

        // ====== 扫码按钮 ======

        /// <summary>执行 OnLocationScanClicked 逻辑。</summary>
        private async void OnLocationScanClicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<string>();
            await Navigation.PushAsync(new QrScanPage(tcs));

            var result = await tcs.Task;
            if (string.IsNullOrWhiteSpace(result))
                return;

            var code = result.Trim();
            LocationEntry.Text = code;
            _vm.LocationCode = code;

            await _vm.QueryDetailsAsync(_vm.LocationCode, _vm.MaterialBarcode);

            if (_vm.Details.Count == 1)
                _vm.OpenEditDialogCommand.Execute(_vm.Details[0]);

            LocationEntry.Focus();
        }

        /// <summary>执行 OnMaterialScanClicked 逻辑。</summary>
        private async void OnMaterialScanClicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<string>();
            await Navigation.PushAsync(new QrScanPage(tcs));

            var result = await tcs.Task;
            if (string.IsNullOrWhiteSpace(result))
                return;

            var code = result.Trim();
            MaterialEntry.Text = code;
            _vm.MaterialBarcode = code;

            await _vm.QueryDetailsAsync(_vm.LocationCode, _vm.MaterialBarcode);

            if (_vm.Details.Count == 1)
                _vm.OpenEditDialogCommand.Execute(_vm.Details[0]);

            MaterialEntry.Focus();
        }
    }
}
