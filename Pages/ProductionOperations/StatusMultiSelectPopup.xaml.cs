using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.ViewModels;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class StatusMultiSelectPopup : Popup
{
    public ObservableCollection<StatusFilterOption> Options { get; }
    private readonly bool _cascadeSelectDownward;

    public StatusMultiSelectPopup(
        ObservableCollection<StatusFilterOption> options,
        string? title = null,
        bool cascadeSelectDownward = false)
    {

        InitializeComponent();
        Options = options ?? new ObservableCollection<StatusFilterOption>();
        _cascadeSelectDownward = cascadeSelectDownward;
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
            if (!_cascadeSelectDownward)
            {
                item.IsSelected = !item.IsSelected;
                return;
            }

            var index = Options.IndexOf(item);
            if (index < 0)
            {
                item.IsSelected = !item.IsSelected;
                return;
            }

            var nextValue = !item.IsSelected;
            for (var i = index; i < Options.Count; i++)
            {
                Options[i].IsSelected = nextValue;
            }
        }
    }
}
