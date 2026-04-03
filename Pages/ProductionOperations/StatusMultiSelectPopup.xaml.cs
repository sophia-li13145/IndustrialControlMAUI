using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.ViewModels;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class StatusMultiSelectPopup : Popup
{
    public ObservableCollection<StatusFilterOption> Options { get; }

    public StatusMultiSelectPopup(ObservableCollection<StatusFilterOption> options, string? title = null)
    {

        InitializeComponent();
        Options = options ?? new ObservableCollection<StatusFilterOption>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            TitleLabel.Text = title;
        }
        BindingContext = this;

    }

    private void OnDoneClicked(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnOptionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is StatusFilterOption item)
        {
            item.IsSelected = !item.IsSelected;
        }
    }
}
