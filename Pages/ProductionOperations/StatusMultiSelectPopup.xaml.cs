using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.ViewModels;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class StatusMultiSelectPopup : Popup
{
    public ObservableCollection<StatusFilterOption> Options { get; }
    private readonly bool _cascadeSelectDownward;
    private readonly bool _enforceContinuousSelection;

    public StatusMultiSelectPopup(
        ObservableCollection<StatusFilterOption> options,
        string? title = null,
        bool cascadeSelectDownward = false,
        bool enforceContinuousSelection = false)
    {

        InitializeComponent();
        Options = options ?? new ObservableCollection<StatusFilterOption>();
        _cascadeSelectDownward = cascadeSelectDownward;
        _enforceContinuousSelection = enforceContinuousSelection;
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

    private async void OnOptionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is StatusFilterOption item)
        {
            if (!_cascadeSelectDownward)
            {
                var index = Options.IndexOf(item);
                var nextValue = !item.IsSelected;
                if (_enforceContinuousSelection && !IsContinuousSelectionAfterToggle(index, nextValue, cascade: false))
                {
                    if (Shell.Current?.CurrentPage != null)
                    {
                        await Shell.Current.CurrentPage.DisplayAlert("提示", "返修工序必须连续选择，不能跳过中间工序。", "确定");
                    }
                    return;
                }

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
            if (nextValue)
            {
                for (var i = index; i < Options.Count; i++)
                {
                    Options[i].IsSelected = true;
                }
                return;
            }

            for (var i = index; i < Options.Count; i++)
            {
                Options[i].IsSelected = false;
            }
            for (var i = 0; i < index; i++)
            {
                Options[i].IsSelected = false;
            }
        }
    }

    private bool IsContinuousSelectionAfterToggle(int toggledIndex, bool toggledValue, bool cascade)
    {
        if (toggledIndex < 0 || toggledIndex >= Options.Count) return true;

        var states = Options.Select(x => x.IsSelected).ToArray();
        if (cascade)
        {
            for (var i = toggledIndex; i < states.Length; i++)
            {
                states[i] = toggledValue;
            }
        }
        else
        {
            states[toggledIndex] = toggledValue;
        }

        var firstSelected = Array.IndexOf(states, true);
        if (firstSelected < 0) return true;
        var lastSelected = Array.LastIndexOf(states, true);

        for (var i = firstSelected; i <= lastSelected; i++)
        {
            if (!states[i]) return false;
        }
        return true;
    }
}
