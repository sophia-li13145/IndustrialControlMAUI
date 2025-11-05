using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MeterSelectPopup : Popup
{
    public MeterSelectPopup(MeterSelectViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        this.Opened += async (_, __) => await vm.EnsureInitAsync();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (BindingContext is MeterSelectViewModel vm && vm.SelectedRow is EnergyMeterUiRow row)
        {
            Close(row); // 将选中行作为结果返回
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("提示", "请先选择一行", "OK");
        }
    }
}