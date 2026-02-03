using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using SharedLocationVM = IndustrialControlMAUI.ViewModels.LocationVM;
namespace IndustrialControlMAUI.Pages;

public partial class WarehouseLocationPickerPage : ContentPage
{
    private static TaskCompletionSource<SharedLocationVM>? _tcs;
    private bool _expanderBusy;

    /// <summary>执行 WarehouseLocationPickerPage 初始化逻辑。</summary>
    public WarehouseLocationPickerPage(IWarehouseService svc)
    {
        var vm = new WarehouseLocationPickerViewModel(svc);
        BindingContext = vm;
        InitializeComponent();
        vm.RequestReveal = async (warehouseVm, locationVm) =>
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // 滚动到对应仓库（让该仓库出现在顶部）
                    var list = vm.Warehouses;
                    var index = list.IndexOf(warehouseVm);
                    if (index >= 0)
                        PickerRoot.ScrollTo(index, position: ScrollToPosition.Start, animate: true);
                });
            }
            catch { /* ignore */ }
        };
    }
    
    public Task<SharedLocationVM> WaitForResultAsync()
       => (_tcs = new TaskCompletionSource<SharedLocationVM>()).Task;

    /// <summary>执行 CloseWithResultAsync 逻辑。</summary>
    public async Task CloseWithResultAsync(SharedLocationVM? vm)
    {
        if (Navigation?.ModalStack?.Any() == true)
            await Navigation.PopModalAsync();
        _tcs?.TrySetResult(vm);
    }

    /// <summary>执行 OnBackButtonPressed 逻辑。</summary>
    protected override bool OnBackButtonPressed()
    {
        _tcs?.TrySetResult(null);
        return base.OnBackButtonPressed();
    }
    /// <summary>弹出并等待用户选择，返回所选库位；取消返回 null。</summary>
    public static async Task<LocationVM?> ShowAsync(IServiceProvider sp, Page host)
    {
        var svc = sp.GetRequiredService<IWarehouseService>();
        var page = new WarehouseLocationPickerPage(svc);

        var tcs = new TaskCompletionSource<LocationVM?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // 把“回传结果并关闭”的逻辑挂到 VM 回调
        if (page.BindingContext is WarehouseLocationPickerViewModel vm)
        {
            vm.RequestCloseWithResult = async (loc) =>
            {
                try
                {
                    tcs.TrySetResult(loc);
                    // 关闭页面（选其一：Modal 或 Shell）
                    if (Shell.Current?.Navigation is not null)
                        await Shell.Current.Navigation.PopModalAsync();
                    else
                        await host.Navigation.PopModalAsync();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };
        }

        // 打开为模态页
        if (Shell.Current?.Navigation is not null)
            await Shell.Current.Navigation.PushModalAsync(page);
        else
            await host.Navigation.PushModalAsync(page);

        // 等待结果（Select 时完成；或外部关闭可自行在外层 TrySetResult(null)）
        return await tcs.Task.ConfigureAwait(false);
    }
    /// <summary>执行 OnExpanderExpanded 逻辑。</summary>
    private async void OnExpanderExpanded(object sender, EventArgs e)
    {
        if (_expanderBusy) return;
        _expanderBusy = true;
        try
        {
            // 拿到当前项的 VM
            if (sender is CommunityToolkit.Maui.Views.Expander exp &&
                exp.BindingContext is WarehouseVM rowVm)
            {
                // 等待页面 VM 的初始化完成（如果你用了 Initialization）
                if (BindingContext is WarehouseLocationPickerViewModel pageVm)
                {
                    if (pageVm.Initialization is Task initTask) await initTask.ConfigureAwait(false);

                    // 直接调用“只加载一次”的方法 —— 稳
                    await rowVm.EnsureLoadedAsync().ConfigureAwait(false);
                }
                else
                {
                    await rowVm.EnsureLoadedAsync().ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage!.DisplayAlert("错误", ex.Message, "确定"));
        }
        finally
        {
            _expanderBusy = false;
        }
    }
}
