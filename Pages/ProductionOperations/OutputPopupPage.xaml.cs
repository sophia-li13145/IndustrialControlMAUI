using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class OutputPopupPage : ContentPage
{
    private readonly OutputPopupViewModel _vm;

    /// <summary>执行 OutputPopupPage 初始化逻辑。</summary>
    public OutputPopupPage(OutputPopupViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>
    /// 打开弹窗
    /// - presetMaterial != null 时预选，仅用于查看/编辑
    /// - presetMaterial == null 时 materialInputList 未选择
    /// </summary>
    public static async Task<OutputPopupResult?> ShowAsync(
        IServiceProvider? sp,
        IEnumerable<TaskMaterialOutput> materialOutputList,
        TaskMaterialOutput? presetMaterial = null,
        WorkProcessTaskDetail? detail = null)
    {
        var tcs = new TaskCompletionSource<OutputPopupResult?>();

        // 1) 取 ServiceProvider（优先全局，否则直接 new）
        var provider = sp ?? Application.Current?.Handler?.MauiContext?.Services;

        OutputPopupViewModel vm =
            provider is not null
                ? ActivatorUtilities.CreateInstance<OutputPopupViewModel>(provider)
                : new OutputPopupViewModel();

        // 2) 初始化 VM 并绑定
        vm.Init(materialOutputList ?? Enumerable.Empty<TaskMaterialOutput>(), presetMaterial, detail);
        vm.SetResultTcs(tcs);

        // 3) 打开弹窗并等待结果
        var page = new OutputPopupPage(vm);
        page.Disappearing += async (_, _) =>
        {
            // 跳转到扫码页时，新增产出页也会触发 Disappearing；返回/系统回退真正移除页面时也会触发。
            // 等一帧让导航栈完成更新后再判断，避免页面还未从栈中移除时漏掉取消结果，导致调用方命令一直处于执行中。
            await Task.Delay(100);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var navigation = Application.Current?.MainPage?.Navigation;
                var isStillOpen = navigation?.NavigationStack.Contains(page) == true
                                  || navigation?.ModalStack.Contains(page) == true;

                if (!isStillOpen)
                    tcs.TrySetResult(null);
            });
        };

        if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PushAsync(page);

        // 4) 返回结果
        return await tcs.Task;
    }
}
