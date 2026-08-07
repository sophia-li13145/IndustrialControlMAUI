using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class ShiftHandoverViewModel : ObservableObject
{
    private readonly IShiftHandoverApi _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool hasPendingHandover;
    [ObservableProperty] private bool hasRecords;
    [ObservableProperty] private bool hasNoRecords = true;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string pendingMessage = string.Empty;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isStartSheetVisible;
    [ObservableProperty] private bool isFormBusy;
    [ObservableProperty] private bool isSubmitting;
    [ObservableProperty] private string handoverDate = "--";
    [ObservableProperty] private string handoverUserName = "--";
    [ObservableProperty] private string handoverTeamName = "--";
    [ObservableProperty] private ReceiverTeamOption? selectedReceiverTeam;
    [ObservableProperty] private string? handoverMemo;
    [ObservableProperty] private string? pendingHandoverId;
    [ObservableProperty] private bool isConfirmSheetVisible;
    [ObservableProperty] private bool isConfirmFormBusy;
    [ObservableProperty] private bool isConfirming;
    [ObservableProperty] private string confirmHandoverTeamName = "--";
    [ObservableProperty] private string confirmHandoverUserName = "--";
    [ObservableProperty] private string confirmMemo = "--";
    [ObservableProperty] private bool isDetailSheetVisible;
    [ObservableProperty] private bool isDetailBusy;
    [ObservableProperty] private string detailRecordTime = "--";
    [ObservableProperty] private string detailStatusText = "--";
    [ObservableProperty] private string detailHandoverTeamName = "--";
    [ObservableProperty] private string detailHandoverShiftName = "--";
    [ObservableProperty] private string detailHandoverUserName = "--";
    [ObservableProperty] private string detailReceiverTeamName = "--";
    [ObservableProperty] private string detailReceiverShiftName = "--";
    [ObservableProperty] private string detailReceiverUserName = "--";
    [ObservableProperty] private string detailMemo = "--";
    [ObservableProperty] private Color detailStatusColor = Color.FromArgb("#16A65A");
    [ObservableProperty] private Color detailStatusBackground = Color.FromArgb("#E9FFF2");
    [ObservableProperty] private string currentUserName = "--";

    public ObservableCollection<ShiftHandoverRecordItem> Records { get; } = new();
    public ObservableCollection<ReceiverTeamOption> ReceiverTeams { get; } = new();
    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand OpenStartSheetCommand { get; }
    public IRelayCommand CloseStartSheetCommand { get; }
    public IAsyncRelayCommand SubmitHandoverCommand { get; }
    public IAsyncRelayCommand OpenConfirmSheetCommand { get; }
    public IRelayCommand CloseConfirmSheetCommand { get; }
    public IAsyncRelayCommand ConfirmHandoverCommand { get; }
    public IAsyncRelayCommand<ShiftHandoverRecordItem> OpenDetailSheetCommand { get; }
    public IRelayCommand CloseDetailSheetCommand { get; }
    public event Action<string>? SubmissionFailed;

    public ShiftHandoverViewModel(IShiftHandoverApi api)
    {
        _api = api;
        var loginUserName = Preferences.Get("UserName", string.Empty)?.Trim();
        CurrentUserName = string.IsNullOrWhiteSpace(loginUserName) ? "--" : loginUserName.Split('@')[0];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        OpenStartSheetCommand = new AsyncRelayCommand(OpenStartSheetAsync);
        CloseStartSheetCommand = new RelayCommand(() => IsStartSheetVisible = false);
        SubmitHandoverCommand = new AsyncRelayCommand(SubmitHandoverAsync);
        OpenConfirmSheetCommand = new AsyncRelayCommand(OpenConfirmSheetAsync);
        CloseConfirmSheetCommand = new RelayCommand(() => IsConfirmSheetVisible = false);
        ConfirmHandoverCommand = new AsyncRelayCommand(ConfirmHandoverAsync);
        OpenDetailSheetCommand = new AsyncRelayCommand<ShiftHandoverRecordItem>(OpenDetailSheetAsync);
        CloseDetailSheetCommand = new RelayCommand(() => IsDetailSheetVisible = false);
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            HasError = false;
            var pendingTask = _api.GetPendingHandoverAsync();
            var recordsTask = _api.GetRecordsAsync(1, 20);
            var dictionariesTask = _api.GetDictListAsync();
            await Task.WhenAll(pendingTask, recordsTask, dictionariesTask);

            var pending = await pendingTask;
            HasPendingHandover = pending.success == true && pending.result is not null;
            PendingHandoverId = HasPendingHandover ? pending.result!.Id : null;
            PendingMessage = HasPendingHandover
                ? $"来自 {pending.result!.HandoverTeamName}（当前班次）的 {pending.result.HandoverUserName} 发起了交接班，指定 {pending.result.ReceiverTeamName} 接班。"
                : string.Empty;

            Records.Clear();
            var page = await recordsTask;
            var dictionaries = await dictionariesTask;
            var statusNames = BuildStatusNames(dictionaries);
            if (page.success == true && page.result?.Records is not null)
                foreach (var record in page.result.Records)
                {
                    statusNames.TryGetValue(record.HandoverStatus.ToString(), out var statusName);
                    Records.Add(new ShiftHandoverRecordItem(record, statusName));
                }
            HasRecords = Records.Count > 0;
            HasNoRecords = !HasRecords;

            if (pending.success != true && page.success != true)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(page.message) ? "交接班数据加载失败" : page.message;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"交接班数据加载异常：{ex.Message}";
            HasError = true;
            HasPendingHandover = false;
            HasRecords = Records.Count > 0;
            HasNoRecords = !HasRecords;
        }
        finally { IsBusy = false; }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task OpenStartSheetAsync()
    {
        if (IsFormBusy) return;
        IsStartSheetVisible = true;
        IsFormBusy = true;
        HandoverMemo = null;
        HandoverDate = "--";
        HandoverUserName = "--";
        HandoverTeamName = "--";
        SelectedReceiverTeam = null;
        ReceiverTeams.Clear();
        try
        {
            var infoTask = _api.GetHandoverInfoAsync();
            var teamsTask = _api.GetReceiverTeamOptionsAsync();
            await Task.WhenAll(infoTask, teamsTask);

            var infoResponse = await infoTask;
            var teamsResponse = await teamsTask;
            if (infoResponse.success != true || infoResponse.result is null)
            {
                SubmissionFailed?.Invoke(infoResponse.message ?? "交班信息加载失败");
                return;
            }

            HandoverDate = FormatHandoverDate(infoResponse.result.HandoverDate);
            HandoverUserName = string.IsNullOrWhiteSpace(infoResponse.result.HandoverUserName) ? "--" : infoResponse.result.HandoverUserName;
            HandoverTeamName = string.IsNullOrWhiteSpace(infoResponse.result.TeamName) ? "--" : infoResponse.result.TeamName;

            ReceiverTeams.Clear();
            if (teamsResponse.success == true && teamsResponse.result is not null)
                foreach (var team in teamsResponse.result)
                    ReceiverTeams.Add(team);
            else
                SubmissionFailed?.Invoke(teamsResponse.message ?? "接班班组加载失败");

            SelectedReceiverTeam = ReceiverTeams.FirstOrDefault();
        }
        catch (Exception ex)
        {
            SubmissionFailed?.Invoke($"发起交接班数据加载异常：{ex.Message}");
        }
        finally { IsFormBusy = false; }
    }

    private async Task SubmitHandoverAsync()
    {
        if (IsSubmitting) return;
        if (SelectedReceiverTeam is null || string.IsNullOrWhiteSpace(SelectedReceiverTeam.TeamCode))
        {
            SubmissionFailed?.Invoke("请选择接班班组");
            return;
        }
        if (string.IsNullOrWhiteSpace(HandoverMemo))
        {
            SubmissionFailed?.Invoke("请填写现场情况/交班纪要");
            return;
        }

        try
        {
            IsSubmitting = true;
            var response = await _api.AddAsync(new AddShiftHandoverRequest
            {
                Memo = HandoverMemo?.Trim(),
                ReceiverTeamCode = SelectedReceiverTeam.TeamCode,
                ReceiverTeamName = SelectedReceiverTeam.TeamName
            });

            if (response.success != true)
            {
                SubmissionFailed?.Invoke(response.message ?? "交接班提交失败");
                return;
            }

            IsStartSheetVisible = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SubmissionFailed?.Invoke($"交接班提交异常：{ex.Message}");
        }
        finally { IsSubmitting = false; }
    }

    private static string FormatHandoverDate(string? value)
        => DateTime.TryParse(value, out var date) ? date.ToString("yyyy-MM-dd") : value ?? "--";

    private async Task OpenConfirmSheetAsync()
    {
        if (IsConfirmFormBusy) return;
        if (string.IsNullOrWhiteSpace(PendingHandoverId))
        {
            SubmissionFailed?.Invoke("未获取到待接班记录ID");
            return;
        }

        IsConfirmSheetVisible = true;
        IsConfirmFormBusy = true;
        ConfirmHandoverTeamName = "--";
        ConfirmHandoverUserName = "--";
        ConfirmMemo = "--";
        try
        {
            var response = await _api.GetDetailAsync(PendingHandoverId);
            if (response.success != true || response.result is null)
            {
                SubmissionFailed?.Invoke(response.message ?? "交接班详情加载失败");
                return;
            }

            PendingHandoverId = string.IsNullOrWhiteSpace(response.result.Id) ? PendingHandoverId : response.result.Id;
            ConfirmHandoverTeamName = string.IsNullOrWhiteSpace(response.result.HandoverTeamName) ? "--" : response.result.HandoverTeamName;
            ConfirmHandoverUserName = string.IsNullOrWhiteSpace(response.result.HandoverUserName) ? "--" : response.result.HandoverUserName;
            ConfirmMemo = string.IsNullOrWhiteSpace(response.result.Memo) ? "无" : response.result.Memo;
        }
        catch (Exception ex)
        {
            SubmissionFailed?.Invoke($"交接班详情加载异常：{ex.Message}");
        }
        finally { IsConfirmFormBusy = false; }
    }

    private async Task ConfirmHandoverAsync()
    {
        if (IsConfirming || IsConfirmFormBusy) return;
        if (string.IsNullOrWhiteSpace(PendingHandoverId))
        {
            SubmissionFailed?.Invoke("未获取到待接班记录ID");
            return;
        }

        try
        {
            IsConfirming = true;
            var response = await _api.ConfirmHandoverAsync(new ConfirmShiftHandoverRequest
            {
                HandoverId = PendingHandoverId
            });
            if (response.success != true)
            {
                SubmissionFailed?.Invoke(response.message ?? "确认接班失败");
                return;
            }

            IsConfirmSheetVisible = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SubmissionFailed?.Invoke($"确认接班异常：{ex.Message}");
        }
        finally { IsConfirming = false; }
    }

    private async Task OpenDetailSheetAsync(ShiftHandoverRecordItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Id) || IsDetailBusy) return;
        IsDetailSheetVisible = true;
        IsDetailBusy = true;
        ResetDetail();
        DetailStatusText = item.StatusText;
        DetailStatusColor = item.StatusTextColor;
        DetailStatusBackground = item.StatusBackground;
        try
        {
            var response = await _api.GetDetailAsync(item.Id);
            if (response.success != true || response.result is null)
            {
                SubmissionFailed?.Invoke(response.message ?? "交接班详情加载失败");
                return;
            }

            var detail = response.result;
            DetailRecordTime = FormatRecordTime(detail.RecordTime);
            DetailHandoverTeamName = ValueOrPlaceholder(detail.HandoverTeamName);
            DetailHandoverShiftName = ValueOrPlaceholder(detail.HandoverShiftName);
            DetailHandoverUserName = ValueOrPlaceholder(detail.HandoverUserName);
            DetailReceiverTeamName = ValueOrPlaceholder(detail.ReceiverTeamName);
            DetailReceiverShiftName = ValueOrPlaceholder(detail.ReceiverShiftName);
            DetailReceiverUserName = ValueOrPlaceholder(detail.ReceiverUserName);
            DetailMemo = string.IsNullOrWhiteSpace(detail.Memo) ? "无" : detail.Memo;
        }
        catch (Exception ex)
        {
            SubmissionFailed?.Invoke($"交接班详情加载异常：{ex.Message}");
        }
        finally { IsDetailBusy = false; }
    }

    private void ResetDetail()
    {
        DetailRecordTime = "--";
        DetailHandoverTeamName = "--";
        DetailHandoverShiftName = "--";
        DetailHandoverUserName = "--";
        DetailReceiverTeamName = "--";
        DetailReceiverShiftName = "--";
        DetailReceiverUserName = "--";
        DetailMemo = "--";
    }

    private static string ValueOrPlaceholder(string? value) => string.IsNullOrWhiteSpace(value) ? "--" : value;

    private static string FormatRecordTime(string? value)
        => DateTime.TryParse(value, out var time) ? time.ToString("yyyy-MM-dd HH:mm") : ValueOrPlaceholder(value);

    private static Dictionary<string, string> BuildStatusNames(ApiResp<List<ShiftHandoverDictionary>> response)
    {
        if (response.success != true || response.result is null)
            return new Dictionary<string, string>();

        var statusDictionary = response.result.FirstOrDefault(item =>
            string.Equals(item.Field, "handoverStatus", StringComparison.OrdinalIgnoreCase));

        return statusDictionary?.DictItems
            .Where(item => !string.IsNullOrWhiteSpace(item.DictItemValue) && !string.IsNullOrWhiteSpace(item.DictItemName))
            .GroupBy(item => item.DictItemValue!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().DictItemName!, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>();
    }
}

public class ShiftHandoverRecordItem
{
    public ShiftHandoverRecordItem(ShiftHandoverRecord record, string? statusName)
    {
        Id = record.Id;
        HandoverTeamName = record.HandoverTeamName ?? "--";
        ReceiverTeamName = record.ReceiverTeamName ?? "--";
        HandoverUserName = record.HandoverUserName ?? "--";
        ReceiverUserName = record.ReceiverUserName ?? "--";
        Memo = record.Memo;
        IsCompleted = record.HandoverStatus == 1;
        StatusText = string.IsNullOrWhiteSpace(statusName) ? record.HandoverStatus.ToString() : statusName;
        if (DateTime.TryParse(record.CreatedTime, out var time))
        {
            DateText = time.ToString("yyyy-MM-dd");
            TimeText = time.ToString("HH:mm");
        }
        else
        {
            DateText = record.CreatedTime ?? "--";
            TimeText = string.Empty;
        }
    }

    public string DateText { get; }
    public string? Id { get; }
    public string TimeText { get; }
    public string HandoverTeamName { get; }
    public string ReceiverTeamName { get; }
    public string HandoverUserName { get; }
    public string ReceiverUserName { get; }
    public string? Memo { get; }
    public bool IsCompleted { get; }
    public string StatusText { get; }
    public Color AccentColor => Color.FromArgb(IsCompleted ? "#31D67B" : "#FFC400");
    public Color StatusBackground => Color.FromArgb(IsCompleted ? "#E9FFF2" : "#FFF9DF");
    public Color StatusTextColor => Color.FromArgb(IsCompleted ? "#16A65A" : "#D49B00");
    public string DirectionIcon => "➜";
    public Color DirectionBackground => Color.FromArgb(IsCompleted ? "#00FFFFFF" : "#F5B800");
    public Color DirectionTextColor => Color.FromArgb(IsCompleted ? "#31D67B" : "#FFFFFF");
    public Color DirectionStroke => Color.FromArgb(IsCompleted ? "#00FFFFFF" : "#F5B800");
}
