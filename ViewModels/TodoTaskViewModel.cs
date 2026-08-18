using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;

namespace IndustrialControlMAUI.ViewModels;

public partial class TodoTaskViewModel : ObservableObject
{
    private const int PageSize = 10;
    private readonly ITodoTaskApi _api;
    private int _pageNo = 1;
    private bool _hasMore = true;

    public ObservableCollection<TodoTaskItem> Tasks { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private string? taskNo;
    [ObservableProperty] private long total;
    [ObservableProperty] private string? errorMessage;

    public bool IsEmpty => !IsBusy && Tasks.Count == 0 && string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public TodoTaskViewModel(ITodoTaskApi api) => _api = api;

    partial void OnTotalChanged(long value)
    {
        if (Shell.Current is AppShell shell) shell.UpdateTodoCount(value);
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var response = await _api.PageTodoTasksAsync(1, PageSize, TaskNo, searchCount: true);
            if (!response.success || response.result is null)
            {
                ErrorMessage = response.message ?? "待办事项加载失败";
                return;
            }

            Tasks.Clear();
            foreach (var item in response.result.records) Tasks.Add(item);
            _pageNo = 1;
            Total = response.result.total;
            _hasMore = Tasks.Count < Total && response.result.records.Count >= PageSize;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"待办事项加载失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private Task SearchAsync() => RefreshAsync();

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || IsLoadingMore || !_hasMore) return;
        IsLoadingMore = true;
        try
        {
            var nextPage = _pageNo + 1;
            var response = await _api.PageTodoTasksAsync(nextPage, PageSize, TaskNo, searchCount: false);
            if (!response.success || response.result is null) return;
            foreach (var item in response.result.records) Tasks.Add(item);
            _pageNo = nextPage;
            _hasMore = response.result.records.Count >= PageSize && (Total == 0 || Tasks.Count < Total);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private static async Task OpenTaskAsync(TodoTaskItem? task)
    {
        if (task is null || string.IsNullOrWhiteSpace(task.id)) return;
        var route = task.taskType?.ToUpperInvariant() switch
        {
            "PROCESS" => nameof(WorkProcessTaskDetailPage),
            "INSPECTION" => nameof(InspectionRunDetailPage),
            "REPAIR" => nameof(RepairRunDetailPage),
            _ => null
        };
        if (route is null) return;
        await Shell.Current.GoToAsync(route, new Dictionary<string, object> { ["id"] = task.id });
    }
}
