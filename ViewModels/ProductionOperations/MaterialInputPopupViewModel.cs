using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

public partial class MaterialInputPopupViewModel : ObservableObject
{
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<TaskMaterialInput> MaterialOptions { get; } = new();

    [ObservableProperty] private TaskMaterialInput? selectedMaterial;
    [ObservableProperty] private string? quantityText;
    [ObservableProperty] private string? memo;
    [ObservableProperty] private bool isPickerEnabled = true;
    private TaskCompletionSource<MaterialInputResult?>? _tcs;

    /// <summary>执行 MaterialInputPopupViewModel 初始化逻辑。</summary>
    public MaterialInputPopupViewModel() { }

    // 你要求的签名
    /// <summary>执行 Init 逻辑。</summary>
    public void Init(IEnumerable<TaskMaterialInput> materialInputList, TaskMaterialInput? presetMaterialCode = null)
    {
        MaterialOptions.Clear();
        foreach (var m in materialInputList ?? Enumerable.Empty<TaskMaterialInput>())
        {
            if (m is not null) MaterialOptions.Add(m);
        }

        if (presetMaterialCode is not null)
        {
            // 若预设项不在列表中，临时加进去（只为展示，避免可选混淆也可以不加）
            var hit = MaterialOptions.FirstOrDefault(x => IsSame(x, presetMaterialCode));
            if (hit is null) MaterialOptions.Insert(0, presetMaterialCode);

            SelectedMaterial = MaterialOptions.FirstOrDefault(x => IsSame(x, presetMaterialCode)) ?? presetMaterialCode;
            IsPickerEnabled = false; // 有传值：锁定
        }
        else
        {
            // 无预设：若仅一个选项则自动选中；否则让用户自己选
            if (MaterialOptions.Count == 1)
                SelectedMaterial = MaterialOptions[0];

            IsPickerEnabled = true;
        }
    }

    private static bool IsSame(TaskMaterialInput a, TaskMaterialInput b)
        => string.Equals(a?.materialCode, b?.materialCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>执行 Confirm 逻辑。</summary>
    [RelayCommand]
    private async Task Confirm()
    {
        if (SelectedMaterial is null)
        {
            await Application.Current.MainPage.DisplayAlert("提示", "请先选择投入物料。", "我知道了");
            return;
        }
        if (string.IsNullOrWhiteSpace(QuantityText) || !decimal.TryParse(QuantityText, out var qty) || qty <= 0)
        {
            await Application.Current.MainPage.DisplayAlert("提示", "请输入大于0的投入数量。", "好的");
            return;
        }

        var result = new MaterialInputResult
        {
            materialClassName = SelectedMaterial.materialClassName,
            MaterialCode = SelectedMaterial.materialCode,
            MaterialName = SelectedMaterial.materialName,
            materialTypeName = SelectedMaterial.materialTypeName,
            Quantity = qty,
            Unit = SelectedMaterial.unit,
            OperationTime = DateTime.Now,
            Memo = Memo
        };

        // 把结果回传给 ShowAsync()
        ReturnResult(result);

        // 关闭弹窗（与 Cancel 同步）
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    public void SetResultTcs(TaskCompletionSource<MaterialInputResult?> tcs)
        => _tcs = tcs;

    // 在 Confirm/Cancel 时用：
    private void ReturnResult(MaterialInputResult? result)
        => _tcs?.TrySetResult(result);


    /// <summary>执行 Cancel 逻辑。</summary>
    [RelayCommand]
    private async Task Cancel()
    {
        ReturnResult(null);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }

}

