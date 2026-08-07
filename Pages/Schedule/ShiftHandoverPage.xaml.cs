namespace IndustrialControlMAUI.Pages;

public partial class ShiftHandoverPage : ContentPage
{
    private readonly ViewModels.ShiftHandoverViewModel _viewModel;

    public ShiftHandoverPage(ViewModels.ShiftHandoverViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.SubmissionFailed += OnSubmissionFailed;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private void OnSubmissionFailed(string message)
        => MainThread.BeginInvokeOnMainThread(async () => await DisplayAlert("提醒", message, "知道了"));
}
