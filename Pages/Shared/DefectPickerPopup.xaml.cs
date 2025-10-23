using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

public partial class DefectPickerPopup : CommunityToolkit.Maui.Views.Popup
{
    private readonly DefectPickerViewModel _vm;
    private readonly TaskCompletionSource<List<DefectRecord>?> _tcs = new();

    public DefectPickerPopup(IQualityApi api, IEnumerable<string>? preselectedCodes)
    {
        InitializeComponent();
        _vm = new DefectPickerViewModel(api, preselectedCodes);
        BindingContext = _vm;

        // Popup 打开时加载数据（代替 OnOpened）
        this.Opened += async (s, e) => await _vm.LoadAsync();
    }

    public static Task<List<DefectRecord>?> ShowAsync(IQualityApi api, IEnumerable<string>? preselectedCodes)
    {
        var popup = new DefectPickerPopup(api, preselectedCodes);
        Application.Current?.MainPage?.ShowPopup(popup);
        return popup._tcs.Task;
    }


    private void OnCancel(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        Close();
    }

    private void OnConfirm(object? sender, EventArgs e)
    {
        var picked = _vm.Rows
            .Where(r => r.IsChecked)
            .Select(r => new DefectRecord
            {
                Id = r.Id,
                DefectName = r.Name,
                DefectCode = r.Code,
                Status = r.Status,
                LevelCode = r.LevelCode,
                LevelName = r.Level,
                DefectDescription = r.Description,
                EvaluationStandard = r.Standard,
                Creator = r.Creator,
                CreatedTime = r.CreatedAt,
                ModifiedTime = r.UpdatedAt
            }).ToList();

        _tcs.TrySetResult(picked);
        Close();
    }
}


