using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages
{
    public partial class InboundMoldPage : ContentPage
    {
        private readonly InboundMoldViewModel _vm;
        private readonly IServiceProvider _sp;

        public InboundMoldPage(IServiceProvider sp, InboundMoldViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _sp = sp;
            vm.PickLocationAsync = () => WarehouseLocationPickerPage.ShowAsync(sp, this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ScanEntry?.Focus();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void OnRowCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return; // 只在勾上时选中
            if (sender is CheckBox cb && cb.BindingContext is MoldScanRow row)
            {
                _vm.SelectedRow = row;  // 触发 CollectionView 的选中高亮
            }
        }

        private void OnScanCompleted(object sender, EventArgs e)
        {
            _vm.ScanSubmitCommand.Execute(null);
        }


    }
}
