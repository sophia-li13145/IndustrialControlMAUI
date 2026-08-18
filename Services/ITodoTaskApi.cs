using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Services;

public interface ITodoTaskApi
{
    Task<PageResponeResult<TodoTaskItem>> PageTodoTasksAsync(
        int pageNo,
        int pageSize,
        string? taskNo = null,
        string? taskType = null,
        DateTime? createdTimeStart = null,
        DateTime? createdTimeEnd = null,
        bool searchCount = true,
        CancellationToken ct = default);
}
