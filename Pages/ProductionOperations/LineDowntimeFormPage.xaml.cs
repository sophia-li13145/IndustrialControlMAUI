using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class LineDowntimeFormPage : ContentPage, IQueryAttributable
{
    private readonly LineDowntimeFormViewModel _vm;

    public LineDowntimeFormPage(LineDowntimeFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var mode = query.TryGetValue("mode", out var m) ? m?.ToString() : "add";
        var id = query.TryGetValue("id", out var i) ? Uri.UnescapeDataString(i?.ToString() ?? string.Empty) : null;
        _ = _vm.InitializeAsync(mode, id);
    }
}
