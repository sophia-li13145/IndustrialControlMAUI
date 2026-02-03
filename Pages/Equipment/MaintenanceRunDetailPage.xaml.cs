using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MaintenanceRunDetailPage : ContentPage
{
    private readonly MaintenanceRunDetailViewModel _vm;
    /// <summary>执行 MaintenanceRunDetailPage 初始化逻辑。</summary>
    public MaintenanceRunDetailPage() : this(ServiceHelper.GetService<MaintenanceRunDetailViewModel>()) { }

    /// <summary>执行 MaintenanceRunDetailPage 初始化逻辑。</summary>
    public MaintenanceRunDetailPage(MaintenanceRunDetailViewModel vm)
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

    private async void OnPickFileClicked(object sender, EventArgs e)
        => await _vm.PickFilesAsync();

    //  Entry 完成时优先精确匹配，否则展开候选
    /// <summary>执行 OnUpkeepOperatorEntryCompleted 逻辑。</summary>
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