using IndustrialControlMAUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

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

    protected MaterialFrameQueryViewModel ViewModel => _vm;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    private async void OnSearchCompleted(object sender, EventArgs e)
    {
        await _vm.SearchAsync();
    }
    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not MaterialFrameItemVm item)
            return;

        var navKey = MaterialFrameNavigationStore.Put(item.Source);

        await Shell.Current.GoToAsync($"{nameof(MaterialFrameDetailPage)}?frameNo={navKey}");
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

    private async void OnAddRecordClicked(object sender, EventArgs e)
    {
        await HandleAddRecordAsync();
    }

    protected virtual async Task HandleAddRecordAsync()
    {
        await DisplayAlert("料框管理", "新增记录页面待接入", "确定");
    }

    protected void ConfigureAsOperationPage(string displayName, string operationType)
    {
        _vm.ApplyOperation(displayName, operationType);
        SearchBarGrid.IsVisible = false;
        ActionButtonGrid.IsVisible = true;
    }
}
