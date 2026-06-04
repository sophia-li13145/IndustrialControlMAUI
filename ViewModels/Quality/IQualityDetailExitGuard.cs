namespace IndustrialControlMAUI.ViewModels;

public interface IQualityDetailExitGuard
{
    bool HasUnsavedChanges { get; }

    Task<bool> SaveForExitAsync();
}
