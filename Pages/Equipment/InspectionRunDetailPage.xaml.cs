using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class InspectionRunDetailPage : ContentPage
{
    private readonly InspectionRunDetailViewModel _vm;
    /// <summary>执行 InspectionRunDetailPage 初始化逻辑。</summary>
    public InspectionRunDetailPage() : this(ServiceHelper.GetService<InspectionRunDetailViewModel>()) { }

    /// <summary>执行 InspectionRunDetailPage 初始化逻辑。</summary>
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

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 保留：页面通过 Shell Query 调用 LoadAsync
    }

    //  Entry 完成时优先精确匹配，否则展开候选
    /// <summary>执行 OnInspectorEntryCompleted 逻辑。</summary>
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
