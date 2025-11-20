using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ExceptionSubmissionPage : ContentPage
{
    private readonly ExceptionSubmissionViewModel _vm;
    public ExceptionSubmissionPage() : this(ServiceHelper.GetService<ExceptionSubmissionViewModel>()) { }

    public ExceptionSubmissionPage(ExceptionSubmissionViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();


    }

   

}