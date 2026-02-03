using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Popups;

public partial class InspectionDataPopup : Popup
{
    private readonly InspectionDataPopupViewModel _vm;

    /// <summary>执行 InspectionDataPopup 初始化逻辑。</summary>
    public InspectionDataPopup(IQualityApi api, InspectionDetailQuery query)
    {
        InitializeComponent();
        _vm = new InspectionDataPopupViewModel(api, query);
        BindingContext = _vm;
        Opened += async (_, _) => await _vm.LoadAsync();
    }

    /// <summary>执行 ShowAsync 逻辑。</summary>
    public static Task ShowAsync(IQualityApi api, InspectionDetailQuery query)
    {
        var popup = new InspectionDataPopup(api, query);
        Application.Current?.MainPage?.ShowPopup(popup);
        return Task.CompletedTask;
    }

    /// <summary>执行 OnClose 逻辑。</summary>
    private void OnClose(object? sender, EventArgs e) => Close();
}
