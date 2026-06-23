using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class InspectDevicePickerPopup : Popup
{
    private readonly List<InspectDeviceOption> _allDevices;
    private readonly TaskCompletionSource<InspectDeviceOption?> _tcs = new();

    public ObservableCollection<InspectDeviceOption> FilteredDevices { get; } = new();

    public InspectDeviceOption? SelectedDevice { get; set; }

    public InspectDevicePickerPopup(IEnumerable<InspectDeviceOption> devices, InspectDeviceOption? selectedDevice)
    {
        InitializeComponent();
        _allDevices = devices.ToList();
        SelectedDevice = selectedDevice;
        BindingContext = this;
        ApplyFilter(string.Empty);
    }

    public static Task<InspectDeviceOption?> ShowAsync(IEnumerable<InspectDeviceOption> devices, InspectDeviceOption? selectedDevice)
    {
        var popup = new InspectDevicePickerPopup(devices, selectedDevice);
        Application.Current?.MainPage?.ShowPopup(popup);
        return popup._tcs.Task;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter(e.NewTextValue);
    }

    private void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        if (sender is SearchBar searchBar)
            ApplyFilter(searchBar.Text);
    }

    private void OnDeviceTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is InspectDeviceOption device)
        {
            _tcs.TrySetResult(device);
            Close(device);
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        Close();
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(SelectedDevice);
        Close(SelectedDevice);
    }

    private void ApplyFilter(string? keyword)
    {
        var text = keyword?.Trim();
        var query = string.IsNullOrWhiteSpace(text)
            ? _allDevices
            : _allDevices.Where(d => Contains(d.devName, text));

        FilteredDevices.Clear();
        foreach (var device in query)
            FilteredDevices.Add(device);
    }

    private static bool Contains(string? source, string keyword) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
