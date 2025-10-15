using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ProcessQualitySearchPage : ContentPage
{
    private readonly ProcessQualitySearchViewModel _vm;

    public ProcessQualitySearchPage(ProcessQualitySearchViewModel vm, ScanService scanSvc)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        QualityNoEntry.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }


}
