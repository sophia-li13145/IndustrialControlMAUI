using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ProcessQualityDetailPage : ContentPage
{
    private readonly WorkProcessTaskDetailViewModel _vm;
    public ProcessQualityDetailPage() : this(ServiceHelper.GetService<WorkProcessTaskDetailViewModel>()) { }

    public ProcessQualityDetailPage(WorkProcessTaskDetailViewModel vm)
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