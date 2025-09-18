namespace IndustrialControlMAUI.Pages;
public partial class AdminPage : ContentPage
{
    public AdminPage(ViewModels.AdminViewModel vm)
    { InitializeComponent(); BindingContext = vm; }
}
