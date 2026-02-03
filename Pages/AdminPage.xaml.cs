namespace IndustrialControlMAUI.Pages;
public partial class AdminPage : ContentPage
{
    /// <summary>执行 AdminPage 初始化逻辑。</summary>
    public AdminPage(ViewModels.AdminViewModel vm)
    { InitializeComponent(); BindingContext = vm; }
}
