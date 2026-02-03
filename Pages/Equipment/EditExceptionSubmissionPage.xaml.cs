using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class EditExceptionSubmissionPage : ContentPage
{
    private readonly EditExceptionSubmissionViewModel _vm;
    /// <summary>执行 EditExceptionSubmissionPage 初始化逻辑。</summary>
    public EditExceptionSubmissionPage() : this(ServiceHelper.GetService<EditExceptionSubmissionViewModel>()) { }

    /// <summary>执行 EditExceptionSubmissionPage 初始化逻辑。</summary>
    public EditExceptionSubmissionPage(EditExceptionSubmissionViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();


    }


    private async void OnPickImagesClicked(object sender, EventArgs e)
    => await _vm.PickImagesAsync();
}