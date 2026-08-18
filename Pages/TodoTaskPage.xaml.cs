using IndustrialControlMAUI.Tools;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class TodoTaskPage : ContentPage
{
    private readonly TodoTaskViewModel _viewModel;
    private readonly AuthState _authState;
    private bool _loaded;

    public TodoTaskPage(TodoTaskViewModel viewModel, AuthState authState)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authState = authState;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await _viewModel.RefreshAsync();
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
        => await _authState.LogoutAsync("您已退出登录");
}
