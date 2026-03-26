using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.ViewModels;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class StatusMultiSelectPopup : Popup
{
    public StatusMultiSelectPopup(ObservableCollection<StatusFilterOption> options)
    {
        InitializeComponent();
        BindingContext = options;
    }

    private void OnDoneClicked(object? sender, EventArgs e)
        => Close();
}
