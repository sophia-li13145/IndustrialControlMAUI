using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class MaterialFrameQueryPage : ContentPage
{
    private readonly MaterialFrameQueryViewModel _vm;
    private bool _isScanning;

    public MaterialFrameQueryPage(MaterialFrameQueryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
        ScanEntry?.Focus();
    }

    private async void OnSearchCompleted(object sender, EventArgs e)
    {
        await _vm.SearchAsync();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        if (_isScanning) return;
        _isScanning = true;
        try
        {
            var tcs = new TaskCompletionSource<string>();
            await Navigation.PushAsync(new QrScanPage(tcs));
            var result = await tcs.Task;
            if (string.IsNullOrWhiteSpace(result)) return;
            _vm.FrameNo = result.Trim();
            await _vm.SearchAsync();
        }
        finally { _isScanning = false; }
    }
}
