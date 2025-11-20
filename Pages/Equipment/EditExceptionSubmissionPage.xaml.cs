using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class EditExceptionSubmissionPage : ContentPage
{
    private readonly EditExceptionSubmissionViewModel _vm;
    public EditExceptionSubmissionPage() : this(ServiceHelper.GetService<EditExceptionSubmissionViewModel>()) { }

    public EditExceptionSubmissionPage(EditExceptionSubmissionViewModel vm)
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