using System.Windows.Input;

namespace IndustrialControlMAUI.Controls;

public partial class PasswordEntryView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(PasswordEntryView),
        default(string),
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(PasswordEntryView),
        default(string));

    public static readonly BindableProperty ShowPasswordProperty = BindableProperty.Create(
        nameof(ShowPassword),
        typeof(bool),
        typeof(PasswordEntryView),
        false,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty TogglePasswordCommandProperty = BindableProperty.Create(
        nameof(TogglePasswordCommand),
        typeof(ICommand),
        typeof(PasswordEntryView));

    public PasswordEntryView()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool ShowPassword
    {
        get => (bool)GetValue(ShowPasswordProperty);
        set => SetValue(ShowPasswordProperty, value);
    }

    public ICommand? TogglePasswordCommand
    {
        get => (ICommand?)GetValue(TogglePasswordCommandProperty);
        set => SetValue(TogglePasswordCommandProperty, value);
    }
}
