using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

public partial class FinalProcessCompletePopupViewModel : ObservableObject
{
    [ObservableProperty] private string? actQtyText;
    [ObservableProperty] private string? memo;

    private TaskCompletionSource<FinalProcessCompletePopupResult?>? _tcs;

    public void SetResultTcs(TaskCompletionSource<FinalProcessCompletePopupResult?> tcs) => _tcs = tcs;

    [RelayCommand]
    private async Task Confirm()
    {
        if (string.IsNullOrWhiteSpace(ActQtyText) || !decimal.TryParse(ActQtyText, out var actQty) || actQty <= 0)
        {
            await Application.Current.MainPage.DisplayAlert("提示", "请输入大于0的完工数量。", "确定");
            return;
        }

        _tcs?.TrySetResult(new FinalProcessCompletePopupResult
        {
            ActQty = actQty,
            Memo = Memo
        });

        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        _tcs?.TrySetResult(null);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}
