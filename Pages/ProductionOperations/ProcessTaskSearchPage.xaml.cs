using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class ProcessTaskSearchPage : ContentPage, IQueryAttributable
{
    private readonly ProcessTaskSearchViewModel _vm;
    private string? _entryMode;
    private bool _isStatusPopupOpening;

    public ProcessTaskSearchPage(ProcessTaskSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.SetEntryMode(_entryMode);
        OrderEntry.Focus();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _entryMode = null;
        if (query.TryGetValue("entryMode", out var mode))
        {
            _entryMode = mode?.ToString();
        }

        _vm.SetEntryMode(_entryMode);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        OrderEntry.Text = result.Trim();

        if (BindingContext is ProcessTaskSearchViewModel vm)
        {
            vm.Keyword = result.Trim();

            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);
        }
    }

    private async void OnStatusFilterClicked(object sender, EventArgs e)
    {
        if (_isStatusPopupOpening)
            return;
        System.Diagnostics.Debug.WriteLine("A1: 点击状态按钮");

        try
        {
            _isStatusPopupOpening = true;

            if (BindingContext is ProcessTaskSearchViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"A2: StatusOptions.Count={vm.StatusOptions?.Count}");
                await this.ShowPopupAsync(new StatusMultiSelectPopup(vm.StatusOptions));
                System.Diagnostics.Debug.WriteLine("A3: Popup 已关闭");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"A_ERR: {ex}");
            await DisplayAlert("错误", ex.ToString(), "确定");
        }
        finally
        {
            _isStatusPopupOpening = false;
        }
    }
}