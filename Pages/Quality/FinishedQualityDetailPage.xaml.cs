using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class FinishedQualityDetailPage : ContentPage
{
    private readonly FinishedQualityDetailViewModel _vm;
    /// <summary>执行 FinishedQualityDetailPage 初始化逻辑。</summary>
    public FinishedQualityDetailPage() : this(ServiceHelper.GetService<FinishedQualityDetailViewModel>()) { }

    /// <summary>执行 FinishedQualityDetailPage 初始化逻辑。</summary>
    public FinishedQualityDetailPage(FinishedQualityDetailViewModel vm)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);           // 鿴 Output 
            System.Diagnostics.Debug.WriteLine(ex.InnerException);
            throw; // 保留原异常
        }

        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = _vm;
    }

    /// <summary>执行 OnPickImagesClicked 逻辑。</summary>
    private async void OnPickImagesClicked(object sender, EventArgs e)
    {
        await _vm.PickImagesAsync();
    }

    /// <summary>执行 OnPickFileClicked 逻辑。</summary>
    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        await _vm.PickFilesAsync();
    }

    /// <summary>执行 OnAppearing 逻辑。</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();


    }

    /// <summary>执行 OnInspectorEntryCompleted 逻辑。</summary>
    private void OnInspectorEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is not FinishedQualityDetailViewModel vm) return;

        var text = vm.InspectorText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            vm.IsInspectorDropdownOpen = false;
            return;
        }

        // 1) 精确匹配（姓名/工号/姓名(工号)）
        var exact = vm.AllUsers.FirstOrDefault(u =>
            string.Equals(u.username, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.realname, text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals($"{u.realname} ({u.username})", text, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            vm.PickInspectorCommand.Execute(exact);   // 写入 Detail.inspector 等字段
            return;
        }

        // 2) 仅一个候选时直接选中
        if (vm.InspectorSuggestions.Count == 1)
        {
            vm.PickInspectorCommand.Execute(vm.InspectorSuggestions[0]);
            return;
        }

        // 3) 展开候选列表
        vm.IsInspectorDropdownOpen = vm.InspectorSuggestions.Count > 0;
    }

}