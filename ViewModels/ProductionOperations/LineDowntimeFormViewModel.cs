using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class LineDowntimeFormViewModel : ObservableObject
{
    private readonly IWorkOrderApi _workOrderApi;
    private readonly IAuthApi _authApi;
    private string? _id;
    private List<UserInfoDto> _allUsers = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string mode = "add";
    [ObservableProperty] private string title = "发起停机记录";
    [ObservableProperty] private bool isAddMode = true;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private bool isDetailMode;
    [ObservableProperty] private bool isNotAddMode;
    [ObservableProperty] private bool canSubmit = true;
    [ObservableProperty] private string submitText = "立即提交";
    [ObservableProperty] private Color submitColor = Color.FromArgb("#F2384A");

    [ObservableProperty] private LineDowntimeCardItem? card;
    [ObservableProperty] private LineDowntimeProductionLine? selectedLine;
    [ObservableProperty] private StatusOption? selectedCategory;
    [ObservableProperty] private DateTime occurDate = DateTime.Now.Date;
    [ObservableProperty] private TimeSpan occurTime = DateTime.Now.TimeOfDay;
    [ObservableProperty] private string? reason;
    [ObservableProperty] private string? solution;
    [ObservableProperty] private DateTime resumeDate = DateTime.Now.Date;
    [ObservableProperty] private TimeSpan resumeTime = DateTime.Now.TimeOfDay;
    [ObservableProperty] private string? responsibleText;
    [ObservableProperty] private UserInfoDto? selectedUser;
    [ObservableProperty] private bool isUserSuggestionsVisible;

    public ObservableCollection<LineDowntimeProductionLine> ProductionLines { get; } = new();
    public ObservableCollection<StatusOption> CategoryOptions { get; } = new();
    public ObservableCollection<UserInfoDto> UserSuggestions { get; } = new();

    public IAsyncRelayCommand SubmitCommand { get; }
    public IRelayCommand<UserInfoDto?> SelectUserCommand { get; }

    public LineDowntimeFormViewModel(IWorkOrderApi workOrderApi, IAuthApi authApi)
    {
        _workOrderApi = workOrderApi;
        _authApi = authApi;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        SelectUserCommand = new RelayCommand<UserInfoDto?>(SelectUser);
    }

    public async Task InitializeAsync(string? requestedMode, string? id)
    {
        _id = id;
        Mode = string.IsNullOrWhiteSpace(requestedMode) ? "add" : requestedMode.Trim().ToLowerInvariant();
        ApplyModeFlags();

        IsBusy = true;
        try
        {
            if (IsAddMode)
                await LoadAddOptionsAsync();
            else if (!string.IsNullOrWhiteSpace(_id))
                await LoadDetailAsync(_id);

            if (IsEditMode)
                await LoadUsersAsync(showSuggestions: false);
        }
        finally { IsBusy = false; }
    }

    private void ApplyModeFlags()
    {
        IsAddMode = Mode == "add";
        IsEditMode = Mode == "edit";
        IsDetailMode = Mode == "detail";
        IsNotAddMode = !IsAddMode;
        CanSubmit = !IsDetailMode;
        Title = IsAddMode ? "发起停机记录" : IsEditMode ? "复工确认" : "停机记录概览";
        SubmitText = IsEditMode ? "确认复工并归档" : "立即提交";
        SubmitColor = IsEditMode ? Color.FromArgb("#55A94A") : Color.FromArgb("#F2384A");
    }

    private async Task LoadAddOptionsAsync()
    {
        var linesResp = await _workOrderApi.ListLineDowntimeProductionLinesAsync();
        ProductionLines.Clear();
        foreach (var line in linesResp.result ?? new List<LineDowntimeProductionLine>())
            ProductionLines.Add(line);
        SelectedLine ??= ProductionLines.FirstOrDefault();

        var dictResp = await _workOrderApi.GetLineDowntimeDictAsync();
        var categoryDict = dictResp.result?.FirstOrDefault(x => string.Equals(x.field, "categoryName", StringComparison.OrdinalIgnoreCase))
                           ?? dictResp.result?.FirstOrDefault();
        CategoryOptions.Clear();
        foreach (var item in categoryDict?.dictItems ?? new List<DictItem>())
        {
            var text = item.dictItemName ?? item.dictItemValue;
            if (!string.IsNullOrWhiteSpace(text))
                CategoryOptions.Add(new StatusOption { Text = text!, Value = item.dictItemValue });
        }
        SelectedCategory ??= CategoryOptions.FirstOrDefault();
    }

    private async Task LoadDetailAsync(string id)
    {
        var detailResp = await _workOrderApi.GetLineDowntimeDetailAsync(id);
        if (!detailResp.success || detailResp.result is null)
        {
            await Shell.Current.DisplayAlert("提示", detailResp.message ?? "查询详情失败", "确定");
            return;
        }

        var record = detailResp.result;
        Card = new LineDowntimeCardItem(record, IsDetailMode ? "已复工" : "待处理");
        Reason = record.reason;
        Solution = record.solution;
        ResponsibleText = record.realname;
        if (DateTime.TryParse(record.occurTime, out var occurAt))
        {
            OccurDate = occurAt.Date;
            OccurTime = occurAt.TimeOfDay;
        }
        if (DateTime.TryParse(record.resumeTime, out var resumeAt))
        {
            ResumeDate = resumeAt.Date;
            ResumeTime = resumeAt.TimeOfDay;
        }
    }

    private async Task LoadUsersAsync(bool showSuggestions)
    {
        if (_allUsers.Count == 0)
            _allUsers = await _authApi.GetAllUsersAsync();

        if (showSuggestions)
            FilterUsers();
        else
            IsUserSuggestionsVisible = false;
    }

    public async Task ShowUserSuggestionsAsync()
    {
        if (!IsEditMode || IsDetailMode) return;
        await LoadUsersAsync(showSuggestions: true);
    }

    partial void OnResponsibleTextChanged(string? value)
    {
        if (!IsEditMode) return;
        if (SelectedUser is not null && string.Equals(value, SelectedUser.realname, StringComparison.Ordinal)) return;
        SelectedUser = null;
        if (_allUsers.Count > 0)
            FilterUsers(value);
    }

    private void FilterUsers(string? keyword = null)
    {
        UserSuggestions.Clear();
        var k = (keyword ?? ResponsibleText ?? string.Empty).Trim();
        var users = string.IsNullOrWhiteSpace(k)
            ? _allUsers.Take(20)
            : _allUsers.Where(u =>
                (!string.IsNullOrWhiteSpace(u.realname) && u.realname.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(u.username) && u.username.Contains(k, StringComparison.OrdinalIgnoreCase))).Take(20);
        foreach (var user in users) UserSuggestions.Add(user);
        IsUserSuggestionsVisible = IsEditMode && UserSuggestions.Count > 0;
    }

    private void SelectUser(UserInfoDto? user)
    {
        if (user is null) return;
        SelectedUser = user;
        ResponsibleText = user.realname ?? user.username;
        IsUserSuggestionsVisible = false;
    }

    private async Task SubmitAsync()
    {
        if (IsBusy || IsDetailMode) return;
        if (IsAddMode)
            await SubmitAddAsync();
        else if (IsEditMode)
            await SubmitEditAsync();
    }

    private async Task SubmitAddAsync()
    {
        if (SelectedLine is null)
        {
            await Shell.Current.DisplayAlert("提示", "请选择当前产线/设备", "确定");
            return;
        }
        if (SelectedCategory is null)
        {
            await Shell.Current.DisplayAlert("提示", "请选择异常类别", "确定");
            return;
        }
        if (string.IsNullOrWhiteSpace(Reason))
        {
            await Shell.Current.DisplayAlert("提示", "请填写停机原因和说明", "确定");
            return;
        }

        IsBusy = true;
        try
        {
            var occurAt = OccurDate.Date.Add(OccurTime);
            var resp = await _workOrderApi.AddLineDowntimeAsync(new LineDowntimeAddReq
            {
                categoryName = SelectedCategory.Value ?? SelectedCategory.Text,
                memo = null,
                occurTime = occurAt.ToString("yyyy-MM-dd HH:mm:ss"),
                reason = Reason.Trim(),
                workshopsCode = SelectedLine.workshopsCode,
                workshopsName = SelectedLine.workshopsName
            });
            if (resp.success)
            {
                await Shell.Current.DisplayAlert("提示", "提交成功", "确定");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("提交失败", resp.message ?? "请稍后重试", "确定");
            }
        }
        finally { IsBusy = false; }
    }

    private async Task SubmitEditAsync()
    {
        if (string.IsNullOrWhiteSpace(_id)) return;
        if (string.IsNullOrWhiteSpace(Solution))
        {
            await Shell.Current.DisplayAlert("提示", "请录入维修处理方案", "确定");
            return;
        }
        if (SelectedUser is null)
        {
            await Shell.Current.DisplayAlert("提示", "请检索并选中责任人", "确定");
            return;
        }

        IsBusy = true;
        try
        {
            var resumeAt = ResumeDate.Date.Add(ResumeTime);
            var resp = await _workOrderApi.EditLineDowntimeAsync(new LineDowntimeEditReq
            {
                id = _id,
                realname = SelectedUser.realname ?? SelectedUser.username,
                resumeTime = resumeAt.ToString("yyyy-MM-dd HH:mm:ss"),
                solution = Solution.Trim(),
                userId = SelectedUser.id ?? SelectedUser.username
            });
            if (resp.success)
            {
                await Shell.Current.DisplayAlert("提示", "复工确认成功", "确定");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("提交失败", resp.message ?? "请稍后重试", "确定");
            }
        }
        finally { IsBusy = false; }
    }
}
