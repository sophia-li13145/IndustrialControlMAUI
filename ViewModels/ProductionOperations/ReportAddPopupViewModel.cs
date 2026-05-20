using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class ReportAddPopupViewModel : ObservableObject
{
    private readonly IWorkOrderApi _api;
    private readonly IAuthApi _authApi;
    private TaskCompletionSource<bool>? _tcs;
    private WorkProcessTaskDetail? _detail;

    public ObservableCollection<StatusOption> DeviceOptions { get; } = new();
    public ObservableCollection<StatusOption> ShiftOptions { get; } = new();
    public ObservableCollection<UserInfoDto> UserOptions { get; } = new();

    [ObservableProperty] private StatusOption? selectedDevice;
    [ObservableProperty] private StatusOption? selectedShift;
    [ObservableProperty] private UserInfoDto? selectedUser;
    [ObservableProperty] private string? workHoursText;
    [ObservableProperty] private string? reportQtyText;
    [ObservableProperty] private string? operateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    [ObservableProperty] private string? memo;

    public ReportAddPopupViewModel(IWorkOrderApi api, IAuthApi authApi)
    {
        _api = api;
        _authApi = authApi;
    }

    public async Task InitAsync(WorkProcessTaskDetail detail)
    {
        _detail = detail;
        DeviceOptions.Clear();
        ShiftOptions.Clear();
        UserOptions.Clear();

        var deviceResp = await _api.GetDeviceOptionsAsync(detail.factoryCode!, detail.processCode!, workshopsCode: detail.workShop);
        foreach (var d in deviceResp.result ?? new List<DevicesInfo>())
            DeviceOptions.Add(new StatusOption { Text = d.deviceName ?? d.deviceCode, Value = d.deviceCode });

        var shiftResp = await _api.GetShiftOptionsAsync(detail.factoryCode!, detail.workShop!);
        foreach (var s in shiftResp.result ?? new List<ShiftInfo>())
            ShiftOptions.Add(new StatusOption { Text = s.workshopsName ?? s.workshopsCode, Value = s.workshopsCode });

        var users = await _authApi.GetAllUsersAsync();
        foreach (var u in users)
            UserOptions.Add(u);

        var current = UserOptions.FirstOrDefault(x => string.Equals(x.username, Preferences.Get("UserName", string.Empty), StringComparison.OrdinalIgnoreCase));
        SelectedUser = current ?? UserOptions.FirstOrDefault();
    }

    public void SetResultTcs(TaskCompletionSource<bool> tcs) => _tcs = tcs;

    [RelayCommand]
    private async Task Confirm()
    {
        if (_detail is null || string.IsNullOrWhiteSpace(_detail.processCode) || string.IsNullOrWhiteSpace(_detail.workOrderNo))
        {
            await Application.Current.MainPage.DisplayAlert("提示", "工单信息缺失，无法新增报工", "确定");
            return;
        }
        if (!decimal.TryParse(ReportQtyText, out var qty) || qty <= 0)
        {
            await Application.Current.MainPage.DisplayAlert("提示", "请输入大于0的报工数量", "确定");
            return;
        }

        var req = new AddWorkProcessTaskReportReq
        {
            memo = Memo,
            operateTime = string.IsNullOrWhiteSpace(OperateTimeText) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : OperateTimeText,
            @operator = SelectedUser?.realname ?? string.Empty,
            processCode = _detail.processCode!,
            productionMachine = SelectedDevice?.Value,
            productionMachineName = SelectedDevice?.Text,
            reportQty = qty,
            teamCode = SelectedShift?.Value,
            teamName = SelectedShift?.Text,
            unqualifiedQty = 0,
            workHours = decimal.TryParse(WorkHoursText, out var hours) ? hours : null,
            workOrderNo = _detail.workOrderNo!
        };

        var resp = await _api.AddWorkProcessTaskReportAsync(req);
        if (!resp.success)
        {
            await Application.Current.MainPage.DisplayAlert("提示", resp.message ?? "新增报工失败", "确定");
            return;
        }

        _tcs?.TrySetResult(true);
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}
