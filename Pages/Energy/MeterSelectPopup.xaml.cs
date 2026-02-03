using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MeterSelectPopup : Popup
{
    /// <summary>执行 MeterSelectPopup 初始化逻辑。</summary>
    public MeterSelectPopup(MeterSelectViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        this.Opened += async (_, __) => await vm.EnsureInitAsync();
    }

    /// <summary>执行 OnConfirmClicked 逻辑。</summary>
    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (BindingContext is MeterSelectViewModel vm && vm.SelectedRow is EnergyMeterUiRow row)
        {
            Close(row); // 选中项
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("提示", "请选择一个", "OK");
        }
    }
}