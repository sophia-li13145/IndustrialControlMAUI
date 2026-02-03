using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    /// <summary>执行 LoginPage 初始化逻辑。</summary>
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // 页面出现时注册事件
        this.Appearing += OnPageAppearing;
    }

    /// <summary>执行 OnPageAppearing 逻辑。</summary>
    private void OnPageAppearing(object? sender, EventArgs e)
    {
        // 页面出现时默认聚焦用户名
        if (this.FindByName<Entry>("UserNameEntry") is Entry entry)
        {
            entry.Focus();
        }
    }

    // 不使用 XAML Command，改用 C# 绑定
    /// <summary>执行 OnLoginClicked 逻辑。</summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_vm.LoginCommand.CanExecute(null))
            _vm.LoginCommand.Execute(null);
    }

    /// <summary>执行 OnClearHistoryTapped 逻辑。</summary>
    private void OnClearHistoryTapped(object sender, TappedEventArgs e)
    {
        if (_vm.ClearHistoryCommand.CanExecute(null))
            _vm.ClearHistoryCommand.Execute(null);
    }
}
