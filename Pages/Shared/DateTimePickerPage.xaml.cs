using System;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Pages;

public partial class DateTimePickerPage : ContentPage
{
    private TaskCompletionSource<DateTime?>? _tcs;

    /// <summary>执行 DateTimePickerPage 初始化逻辑。</summary>
    public DateTimePickerPage(DateTime? initial)
    {
        InitializeComponent();
        var dt = initial ?? DateTime.Now;
        Dp.Date = dt.Date;
        Tp.Time = dt.TimeOfDay;
    }

    // ✅ 不再需要 IServiceProvider
    /// <summary>执行 ShowAsync 逻辑。</summary>
    public static async Task<DateTime?> ShowAsync(DateTime? initial)
    {
        var page = new DateTimePickerPage(initial);
        var tcs = new TaskCompletionSource<DateTime?>();
        page._tcs = tcs;

        // 确保在主线程导航
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(page);
        });

        return await tcs.Task;
    }

    /// <summary>执行 OnCancel 逻辑。</summary>
    private async void OnCancel(object? sender, EventArgs e)
    {
        _tcs?.TrySetResult(null);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    /// <summary>执行 OnOk 逻辑。</summary>
    private async void OnOk(object? sender, EventArgs e)
    {
        var dt = Dp.Date + Tp.Time;
        _tcs?.TrySetResult(dt);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}
