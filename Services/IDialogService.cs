using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string accept = "确定");
    Task<bool> ConfirmAsync(string title, string message, string accept = "确定", string cancel = "取消");
    Task<string?> PromptAsync(string title, string message, string? initial = null, string? placeholder = null,
                              int? maxLength = null, Keyboard? keyboard = null);

    /// 选择库位（返回选中的库位信息；取消返回 null）
    Task<BinInfo?> SelectBinAsync(string? preselectBinCode = null);
}

