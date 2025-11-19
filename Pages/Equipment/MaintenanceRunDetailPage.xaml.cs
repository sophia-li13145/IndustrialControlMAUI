using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MaintenanceRunDetailPage : ContentPage
{
    private readonly MaintenanceRunDetailViewModel _vm;
    public MaintenanceRunDetailPage() : this(ServiceHelper.GetService<MaintenanceRunDetailViewModel>()) { }

    public MaintenanceRunDetailPage(MaintenanceRunDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

   
    protected override void OnAppearing()
    {
        base.OnAppearing();


    }
    private async void OnPickImagesClicked(object sender, EventArgs e)
        => await _vm.PickImagesAsync();

    private async void OnPickFileClicked(object sender, EventArgs e)
        => await _vm.PickFilesAsync();

    // 在 Entry 完成时做一次“精确匹配或唯一候选自动选择”，否则展开下拉
    private void OnUpkeepOperatorEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is not MaintenanceRunDetailViewModel vm) return;

        var text = vm.UpkeepOperator?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            vm.IsUpkeepOperatorDropdownOpen = false;
            return;
        }

        var exact = vm.AllUsers.FirstOrDefault(u =>
            string.Equals(u.username, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.realname, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals($"{u.realname} ({u.username})", text, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            vm.PickUpkeepOperatorCommand.Execute(exact);
            return;
        }

        if (vm.UpkeepOperatorSuggestions.Count == 1)
        {
            vm.PickUpkeepOperatorCommand.Execute(vm.UpkeepOperatorSuggestions[0]);
            return;
        }

        vm.IsUpkeepOperatorDropdownOpen = vm.UpkeepOperatorSuggestions.Count > 0;
    }
}