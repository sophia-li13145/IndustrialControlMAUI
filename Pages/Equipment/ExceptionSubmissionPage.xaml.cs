using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ExceptionSubmissionPage : ContentPage
{
    private readonly ExceptionSubmissionViewModel _vm;
    /// <summary>执行 ExceptionSubmissionPage 初始化逻辑。</summary>
    public ExceptionSubmissionPage() : this(ServiceHelper.GetService<ExceptionSubmissionViewModel>()) { }

    /// <summary>执行 ExceptionSubmissionPage 初始化逻辑。</summary>
    public ExceptionSubmissionPage(ExceptionSubmissionViewModel vm)
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

   

}