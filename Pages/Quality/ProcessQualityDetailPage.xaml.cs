using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ProcessQualityDetailPage : ContentPage
{
    private readonly ProcessQualityDetailViewModel _vm;
    /// <summary>执行 ProcessQualityDetailPage 初始化逻辑。</summary>
    public ProcessQualityDetailPage() : this(ServiceHelper.GetService<ProcessQualityDetailViewModel>()) { }

    /// <summary>执行 ProcessQualityDetailPage 初始化逻辑。</summary>
    public ProcessQualityDetailPage(ProcessQualityDetailViewModel vm)
    {
        InitializeComponent();
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
        AttachRealtimeSaveHandlers(this);
    }

    private void AttachRealtimeSaveHandlers(Element root)
    {
        if (root is Entry entry)
        {
            entry.TextChanged -= OnRealtimeSaveControlChanged;
            entry.TextChanged += OnRealtimeSaveControlChanged;
        }
        else if (root is Editor editor)
        {
            editor.TextChanged -= OnRealtimeSaveControlChanged;
            editor.TextChanged += OnRealtimeSaveControlChanged;
        }
        else if (root is Picker picker)
        {
            picker.SelectedIndexChanged -= OnRealtimeSaveControlChanged;
            picker.SelectedIndexChanged += OnRealtimeSaveControlChanged;
        }
        else if (root is DatePicker datePicker)
        {
            datePicker.DateSelected -= OnRealtimeSaveControlChanged;
            datePicker.DateSelected += OnRealtimeSaveControlChanged;
        }
        else if (root is Switch switchControl)
        {
            switchControl.Toggled -= OnRealtimeSaveControlChanged;
            switchControl.Toggled += OnRealtimeSaveControlChanged;
        }
        else if (root is CheckBox checkBox)
        {
            checkBox.CheckedChanged -= OnRealtimeSaveControlChanged;
            checkBox.CheckedChanged += OnRealtimeSaveControlChanged;
        }
        else if (root is Slider slider)
        {
            slider.ValueChanged -= OnRealtimeSaveControlChanged;
            slider.ValueChanged += OnRealtimeSaveControlChanged;
        }
        else if (root is Stepper stepper)
        {
            stepper.ValueChanged -= OnRealtimeSaveControlChanged;
            stepper.ValueChanged += OnRealtimeSaveControlChanged;
        }

        foreach (var child in GetChildElements(root))
        {
            AttachRealtimeSaveHandlers(child);
        }
    }

    private static IEnumerable<Element> GetChildElements(Element element)
    {
        switch (element)
        {
            case ContentPage page when page.Content is Element pageContent:
                yield return pageContent;
                break;
            case ScrollView scrollView when scrollView.Content is Element scrollContent:
                yield return scrollContent;
                break;
            case ContentView contentView when contentView.Content is Element content:
                yield return content;
                break;
            case Border border when border.Content is Element borderContent:
                yield return borderContent;
                break;
            case Layout layout:
                foreach (var child in layout.Children.OfType<Element>())
                {
                    yield return child;
                }
                break;
        }
    }

    private void OnRealtimeSaveControlChanged(object? sender, EventArgs e)
    {
        if (BindingContext is ProcessQualityDetailViewModel vm)
        {
            vm.QueueRealtimeSaveFromUi();
        }
    }

    /// <summary>执行 OnInspectorEntryCompleted 逻辑。</summary>
    private void OnInspectorEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is not ProcessQualityDetailViewModel vm) return;

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
