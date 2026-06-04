using IndustrialControlMAUI.ViewModels;

namespace IndustrialControlMAUI.Pages;

internal sealed class QualityExitPrompt
{
    private readonly Page _page;
    private readonly IQualityDetailExitGuard _viewModel;
    private bool _isPrompting;
    private bool _allowExit;

    public QualityExitPrompt(Page page, IQualityDetailExitGuard viewModel)
    {
        _page = page;
        _viewModel = viewModel;
    }

    public void Attach()
    {
        Shell.Current.Navigating -= OnShellNavigating;
        Shell.Current.Navigating += OnShellNavigating;
    }

    public void Detach()
    {
        Shell.Current.Navigating -= OnShellNavigating;
    }

    public bool HandleBackButtonPressed()
    {
        if (_allowExit)
        {
            _allowExit = false;
            return false;
        }

        if (!_viewModel.HasUnsavedChanges)
        {
            return false;
        }

        _ = PromptAndExitAsync();
        return true;
    }

    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_allowExit || _isPrompting || e.Source != ShellNavigationSource.Pop || !_viewModel.HasUnsavedChanges)
        {
            return;
        }

        if (e.CanCancel)
        {
            e.Cancel();
        }

        await PromptAndExitAsync();
    }

    private async Task PromptAndExitAsync()
    {
        if (_isPrompting)
        {
            return;
        }

        _isPrompting = true;
        try
        {
            var save = await _page.DisplayAlert("保存提醒", "当前页面有未保存的修改，是否保存？", "是", "否");
            if (save)
            {
                var saved = await _viewModel.SaveForExitAsync();
                if (!saved)
                {
                    return;
                }
            }

            _allowExit = true;
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            _isPrompting = false;
        }
    }
}
