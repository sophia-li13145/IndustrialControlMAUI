using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ReworkOrderPage : ContentPage
{
    public ReworkOrderPage() : this(ServiceHelper.GetService<ReworkOrderViewModel>()) { }

    public ReworkOrderPage(ReworkOrderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private void ReworkQtyEntry_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || BindingContext is not ReworkOrderViewModel vm) return;
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) return;
        if (!decimal.TryParse(e.NewTextValue, out var enteredQty)) return;
        if (!decimal.TryParse(vm.QuantityText, out var maxQty)) return;
        if (maxQty <= 0 || enteredQty <= maxQty) return;

        var maxQtyText = maxQty.ToString("G29");
        if (entry.Text == maxQtyText) return;

        entry.Text = maxQtyText;
        entry.CursorPosition = entry.Text.Length;
    }
}
