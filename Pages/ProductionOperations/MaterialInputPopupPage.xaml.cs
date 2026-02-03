using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.ViewModels;
using Microsoft.Extensions.DependencyInjection; // 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Pages;

public partial class MaterialInputPopupPage : ContentPage
{
    private readonly MaterialInputPopupViewModel _vm;

    /// <summary>执行 MaterialInputPopupPage 初始化逻辑。</summary>
    public MaterialInputPopupPage(MaterialInputPopupViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>
    /// 打开弹窗
    /// - presetMaterial != null 时预选，仅用于查看/编辑
    /// - presetMaterial == null 时 materialInputList 未选择
    /// </summary>
    public static async Task<MaterialInputResult?> ShowAsync(
        IServiceProvider? sp,
        IEnumerable<TaskMaterialInput> materialInputList,
        TaskMaterialInput? presetMaterial = null)
    {
        var tcs = new TaskCompletionSource<MaterialInputResult?>();

        // 1) 取 ServiceProvider（优先全局，否则直接 new）
        var provider = sp ?? Application.Current?.Handler?.MauiContext?.Services;

        MaterialInputPopupViewModel vm =
            provider is not null
                ? ActivatorUtilities.CreateInstance<MaterialInputPopupViewModel>(provider)
                : new MaterialInputPopupViewModel();

        // 2) 初始化 VM 并绑定
        vm.Init(materialInputList ?? Enumerable.Empty<TaskMaterialInput>(), presetMaterial);
        vm.SetResultTcs(tcs);

        // 3) 打开弹窗并等待结果
        var page = new MaterialInputPopupPage(vm);
        if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PushModalAsync(page);

        // 4) 返回结果
        return await tcs.Task;
    }
}
