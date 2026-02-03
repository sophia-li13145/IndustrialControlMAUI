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
        TaskMaterialOutput? presetMaterial = null)
    {
        var tcs = new TaskCompletionSource<OutputPopupResult?>();

        // 1) 取 ServiceProvider（优先全局，否则直接 new）
        var provider = sp ?? Application.Current?.Handler?.MauiContext?.Services;

        OutputPopupViewModel vm =
            provider is not null
                ? ActivatorUtilities.CreateInstance<OutputPopupViewModel>(provider)
                : new OutputPopupViewModel();

        // 2) 初始化 VM 并绑定
        vm.Init(materialOutputList ?? Enumerable.Empty<TaskMaterialOutput>(), presetMaterial);
        vm.SetResultTcs(tcs);

        // 3) 打开弹窗并等待结果
        var page = new OutputPopupPage(vm);
        if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PushModalAsync(page);

        // 4) 返回结果
        return await tcs.Task;
    }
}
