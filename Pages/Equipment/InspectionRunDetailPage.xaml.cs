using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class InspectionRunDetailPage : ContentPage
{
    private readonly InspectionRunDetailViewModel _vm;
    public InspectionRunDetailPage() : this(ServiceHelper.GetService<InspectionRunDetailViewModel>()) { }

    public InspectionRunDetailPage(InspectionRunDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    private async void OnPickImagesClicked(object sender, EventArgs e)
        => await _vm.PickImagesAsync();

    private async void OnPickFileClicked(object sender, EventArgs e)
        => await _vm.PickFilesAsync();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 这里保留空即可，页面通过 Shell Query 参数触发 LoadAsync
    }

    // 在 Entry 完成时做一次“精确匹配或唯一候选自动选择”，否则展开下拉
    private void OnInspectorEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is not InspectionRunDetailViewModel vm) return;

        var text = vm.InspectorText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            vm.IsInspectorDropdownOpen = false;
            return;
        }

        var exact = vm.AllUsers.FirstOrDefault(u =>
            string.Equals(u.username, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.realname, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals($"{u.realname} ({u.username})", text, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            vm.PickInspectorCommand.Execute(exact);
            return;
        }

        if (vm.InspectorSuggestions.Count == 1)
        {
            vm.PickInspectorCommand.Execute(vm.InspectorSuggestions[0]);
            return;
        }

        vm.IsInspectorDropdownOpen = vm.InspectorSuggestions.Count > 0;
    }
}
