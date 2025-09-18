using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // 可选：监听页面出现事件
        this.Appearing += OnPageAppearing;
    }

    private void OnPageAppearing(object? sender, EventArgs e)
    {
        // 页面出现时，比如自动聚焦用户名输入框
        if (this.FindByName<Entry>("UserNameEntry") is Entry entry)
        {
            entry.Focus();
        }
    }

    // 如果你不使用 XAML 的 Command，而是要在 C# 里写事件，可以这样：
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_vm.LoginCommand.CanExecute(null))
            _vm.LoginCommand.Execute(null);
    }

    private void OnClearHistoryTapped(object sender, TappedEventArgs e)
    {
        if (_vm.ClearHistoryCommand.CanExecute(null))
            _vm.ClearHistoryCommand.Execute(null);
    }
}
