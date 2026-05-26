using IndustrialControlMAUI.ViewModels;
namespace IndustrialControlMAUI.Pages;
public partial class FramePourAddPage : ContentPage { public FramePourAddPage(FramePourAddViewModel vm){ InitializeComponent(); BindingContext=vm; } }
