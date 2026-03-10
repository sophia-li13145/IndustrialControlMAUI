using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Popups;

public partial class InspectionDataPopup : Popup
{
    private readonly InspectionDataPopupViewModel _vm;

    public InspectionDataPopup(InspectionDataPopupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    public static async Task ShowAsync(IQualityApi api, InspectionDetailQuery query)
    {
        var vm = new InspectionDataPopupViewModel(api, query);
        var popup = new InspectionDataPopup(vm);

        await vm.LoadAsync();
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        Close();
    }
}