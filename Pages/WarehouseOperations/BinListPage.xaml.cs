using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class BinListPage : ContentPage
{
    public ObservableCollection<BinInfo> Bins { get; } = new();
    private readonly TaskCompletionSource<BinInfo?> _tcs = new();
    private readonly bool _closeParent;

    public BinListPage(IEnumerable<BinInfo> bins, bool closeParent)
    {
        InitializeComponent();
        BindingContext = this;
        _closeParent = closeParent;
        foreach (var b in bins) Bins.Add(b);
    }

    public static async Task<BinInfo?> ShowAsync(IEnumerable<BinInfo> bins, bool closeParent = false)
    {
        var page = new BinListPage(bins, closeParent);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation
                  ?? throw new InvalidOperationException("Navigation not ready");

        await nav.PushModalAsync(page);
        return await page._tcs.Task;
    }


    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav != null) await nav.PopModalAsync(); // 仅关自己
    }

    private async void OnPickClicked(object? sender, EventArgs e)
    {
        // ✅ 优先从 CommandParameter 取
        var bin = (sender as Button)?.CommandParameter as BinInfo
                  ?? (sender as BindableObject)?.BindingContext as BinInfo;

        if (bin == null) return;

        // 先把选择结果回传，解除 ShowAsync 的 await
        _tcs.TrySetResult(bin);

        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return;

        // 1) 关闭自己（BinListPage）
        await nav.PopModalAsync();

        // 2) 需要时再关闭父页（BinPickerPage）
        if (_closeParent && nav.ModalStack.Count > 0 && nav.ModalStack.Last() is BinPickerPage)
            await nav.PopModalAsync();
    }



}
