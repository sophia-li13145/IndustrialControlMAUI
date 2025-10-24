using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FinishedQualityDetailPage : ContentPage
{
    private readonly FinishedQualityDetailViewModel _vm;
    public FinishedQualityDetailPage() : this(ServiceHelper.GetService<FinishedQualityDetailViewModel>()) { }

    public FinishedQualityDetailPage(FinishedQualityDetailViewModel vm)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);           // 查看 Output 窗口
            System.Diagnostics.Debug.WriteLine(ex.InnerException);
            throw; // 保留原异常
        }

        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    private async void OnPickImagesClicked(object sender, EventArgs e)
    {
        await _vm.PickImagesAsync();
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        await _vm.PickFilesAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();


    }

    private void OnInspectorEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is not FinishedQualityDetailViewModel vm) return;

        var text = vm.InspectorText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            vm.IsInspectorDropdownOpen = false;
            return;
        }

        // 1) 优先：精确匹配（账号/姓名/"姓名(账号)"）
        var exact = vm.AllUsers.FirstOrDefault(u =>
            string.Equals(u.username, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.realname, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals($"{u.realname} ({u.username})", text, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            vm.PickInspectorCommand.Execute(exact);   // 写回 Detail.inspecter & 收起下拉
            return;
        }

        // 2) 只有一个候选时，直接选中
        if (vm.InspectorSuggestions.Count == 1)
        {
            vm.PickInspectorCommand.Execute(vm.InspectorSuggestions[0]);
            return;
        }

        // 3) 其余情况：展开下拉，交给用户点选
        vm.IsInspectorDropdownOpen = vm.InspectorSuggestions.Count > 0;
    }

}