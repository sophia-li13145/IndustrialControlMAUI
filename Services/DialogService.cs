using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;

namespace IndustrialControlMAUI.Services;

public class DialogService : IDialogService
{
    private static Page? CurrentPage => Application.Current?.MainPage;

    public Task AlertAsync(string title, string message, string accept = "确定") =>
        MainThread.InvokeOnMainThreadAsync(() =>
            CurrentPage?.DisplayAlert(title, message, accept) ?? Task.CompletedTask);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "确定", string cancel = "取消") =>
        MainThread.InvokeOnMainThreadAsync(() =>
            CurrentPage?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false));

    public Task<string?> PromptAsync(string title, string message, string? initial = null, string? placeholder = null,
        int? maxLength = null, Keyboard? keyboard = null) =>
        MainThread.InvokeOnMainThreadAsync(() =>
            CurrentPage?.DisplayPromptAsync(title, message, "确定", "取消", placeholder,
                maxLength ?? -1, keyboard ?? Keyboard.Text, initial ?? string.Empty) ?? Task.FromResult<string?>(null));

    // ★ 新增：库位选择弹窗
    public Task<BinInfo?> SelectBinAsync(string? preselectBinCode = null)
        => BinPickerPage.ShowAsync(preselectBinCode);
}
