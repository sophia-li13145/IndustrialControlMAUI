using System;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Pages;

public partial class DateTimePickerPage : ContentPage
{
    private TaskCompletionSource<DateTime?>? _tcs;

    public DateTimePickerPage(DateTime? initial)
    {
        InitializeComponent();
        var dt = initial ?? DateTime.Now;
        Dp.Date = dt.Date;
        Tp.Time = dt.TimeOfDay;
    }

    // ✅ 不再需要 IServiceProvider
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

    private async void OnCancel(object? sender, EventArgs e)
    {
        _tcs?.TrySetResult(null);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    private async void OnOk(object? sender, EventArgs e)
    {
        var dt = Dp.Date + Tp.Time;
        _tcs?.TrySetResult(dt);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}
