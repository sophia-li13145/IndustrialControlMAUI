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
    public ObservableCollection<ReworkBomDetailFlattenItem> UnqualifiedMaterialOptions { get; } = new();


    [ObservableProperty] private StatusOption? selectedDevice;
    [ObservableProperty] private StatusOption? selectedShift;
    [ObservableProperty] private UserInfoDto? selectedUser;
    [ObservableProperty] private string? workHoursText;
    [ObservableProperty] private string? reportQtyText;
    [ObservableProperty] private string? unqualifiedQtyText;
    [ObservableProperty] private ReworkBomDetailFlattenItem? selectedUnqualifiedMaterial;
    [ObservableProperty] private string? operateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    [ObservableProperty] private string? memo;
    [ObservableProperty] private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    public string UnqualifiedMaterialLabel => RequiresUnqualifiedMaterial ? "*不合格物料：" : "不合格物料：";

    private bool RequiresUnqualifiedMaterial
    {
        get
        {
            if (string.IsNullOrWhiteSpace(UnqualifiedQtyText))
                return false;
            return decimal.TryParse(UnqualifiedQtyText, out var qty) && qty > 0;
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }

    partial void OnUnqualifiedQtyTextChanged(string? value)
    {
        OnPropertyChanged(nameof(UnqualifiedMaterialLabel));
    }

    public ReportAddPopupViewModel(IWorkOrderApi api, IAuthApi authApi)
    {
        _api = api;
        _authApi = authApi;
    }

    public async Task InitAsync(WorkProcessTaskDetail detail)
    {
        try
        {
            _detail = detail;
            DeviceOptions.Clear();
            ShiftOptions.Clear();
            UserOptions.Clear();
            UnqualifiedMaterialOptions.Clear();
            SelectedDevice = null;
            SelectedShift = null;
            SelectedUnqualifiedMaterial = null;

            if (detail is null)
            {
                await AlertAsync("工单信息为空，无法初始化报工弹框");
                return;
            }

            if (!string.IsNullOrWhiteSpace(detail.factoryCode) && !string.IsNullOrWhiteSpace(detail.processCode))
            {
                var deviceResp = await _api.GetDeviceOptionsAsync(detail.factoryCode, detail.processCode);
                foreach (var d in deviceResp?.result ?? new List<DevicesInfo>())
                {
                    DeviceOptions.Add(new StatusOption
                    {
                        Text = d.deviceName ?? d.deviceCode,
                        Value = d.deviceCode
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(detail.factoryCode) && !string.IsNullOrWhiteSpace(detail.workShop))
            {
                var shiftResp = await _api.GetShiftOptionsAsync(detail.factoryCode, detail.workShop);
                foreach (var s in shiftResp?.result ?? new List<ShiftInfo>())
                {
                    ShiftOptions.Add(new StatusOption
                    {
                        Text = s.workshopsName ?? s.workshopsCode,
                        Value = s.workshopsCode
                    });
                }
            }

            var users = await _authApi.GetAllUsersAsync();
            foreach (var u in users ?? new List<UserInfoDto>())
                UserOptions.Add(u);

            var currentUserName = Preferences.Get("UserName", string.Empty);
            var current = UserOptions.FirstOrDefault(x =>
                string.Equals(x.username, currentUserName, StringComparison.OrdinalIgnoreCase));
            SelectedUser = current ?? UserOptions.FirstOrDefault();


            if (!string.IsNullOrWhiteSpace(detail.workOrderNo))
            {
                // 接口：/pda/pmsBom/queryPmsBomDetailFlattenByWorkOrder
                var bomResp = await _api.GetReworkBomFlattenDetailsAsync(detail.workOrderNo);
                if (bomResp?.success == true)
                {
                    foreach (var item in (bomResp.result ?? new List<ReworkBomDetailFlattenItem>()))
                    {
                        UnqualifiedMaterialOptions.Add(item);
                    }

                    SelectedUnqualifiedMaterial = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(detail.id))
            {
                var qtyResp = await _api.GetWorkProcessTaskFrameOutputQtyAsync(detail.id);
                if (qtyResp?.success == true && qtyResp.result.HasValue)
                    ReportQtyText = qtyResp.result.Value.ToString("G29");
            }
        }
        catch (Exception ex)
        {
            await AlertAsync($"初始化报工弹框失败：{ex.Message}");
        }
    }

    public void SetResultTcs(TaskCompletionSource<bool> tcs) => _tcs = tcs;

    public void Cancel()
    {
        _tcs?.TrySetResult(false);
    }

    [RelayCommand]
    private async Task Confirm()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            if (_detail is null)
            {
                await AlertAsync("工单信息为空，无法新增报工");
                return;
            }

            if (string.IsNullOrWhiteSpace(_detail.processCode) || string.IsNullOrWhiteSpace(_detail.workOrderNo))
            {
                await AlertAsync("工单信息缺失，无法新增报工");
                return;
            }

            if (SelectedUser is null)
            {
                await AlertAsync("请选择操作人");
                return;
            }

            if (!decimal.TryParse(ReportQtyText, out var qty) || qty <= 0)
            {
                await AlertAsync("请输入大于0的报工数量");
                return;
            }

            if (!string.IsNullOrWhiteSpace(UnqualifiedQtyText)
                && (!decimal.TryParse(UnqualifiedQtyText, out var tempUnqualifiedQty) || tempUnqualifiedQty < 0))
            {
                await AlertAsync("不合格数量不能小于0");
                return;
            }

            if (!string.IsNullOrWhiteSpace(WorkHoursText)
                && (!decimal.TryParse(WorkHoursText, out var tempHours) || tempHours < 0))
            {
                await AlertAsync("工时不能小于0");
                return;
            }

            decimal.TryParse(UnqualifiedQtyText, out var unqualifiedQty);
            decimal.TryParse(WorkHoursText, out var hours);

            if (unqualifiedQty > 0 && (SelectedUnqualifiedMaterial is null
                || string.IsNullOrWhiteSpace(SelectedUnqualifiedMaterial.materialCode)
                || string.IsNullOrWhiteSpace(SelectedUnqualifiedMaterial.materialName)))
            {
                await AlertAsync("不合格数量大于0时，请选择不合格物料");
                return;
            }

            var req = new AddWorkProcessTaskReportReq
            {
                memo = Memo,
                operateTime = string.IsNullOrWhiteSpace(OperateTimeText)
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    : OperateTimeText,
                @operator = SelectedUser.realname ?? SelectedUser.username ?? string.Empty,
                operatorName = SelectedUser.realname ?? SelectedUser.username ?? string.Empty,
                processCode = _detail.processCode!,
                productionMachine = SelectedDevice?.Value,
                productionMachineName = SelectedDevice?.Text,
                reportQty = qty,
                teamCode = SelectedShift?.Value,
                teamName = SelectedShift?.Text,
                unqualifiedQty = unqualifiedQty,
                unqualifiedMaterialCode = unqualifiedQty > 0 ? SelectedUnqualifiedMaterial?.materialCode : null,
                unqualifiedMaterialName = unqualifiedQty > 0 ? SelectedUnqualifiedMaterial?.materialName : null,
                workHours = string.IsNullOrWhiteSpace(WorkHoursText) ? null : hours,
                workOrderNo = _detail.workOrderNo!
            };

            var resp = await _api.AddWorkProcessTaskReportAsync(req);
            if (resp is null)
            {
                await AlertAsync("新增报工失败：接口无返回");
                return;
            }

            if (!resp.success)
            {
                await AlertAsync(resp.message ?? "新增报工失败");
                return;
            }

            _tcs?.TrySetResult(true);
            await CloseModalAsync();
        }
        catch (Exception ex)
        {
            await AlertAsync($"新增报工异常：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    private static bool IsUnqualifiedMaterialCandidate(ReworkBomDetailFlattenItem item)
    {
        var type = item?.materialType?.Trim().ToLowerInvariant();
        if (type == "semi_product" || type == "product")
            return true;

        var typeName = item?.materialTypeName ?? string.Empty;
        return typeName.Contains("半成品", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("产品", StringComparison.OrdinalIgnoreCase);
    }

    private static Task AlertAsync(string message)
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
            Application.Current?.MainPage?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask);
    }

    private static Task CloseModalAsync()
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation?.ModalStack?.Count > 0)
                await navigation.PopModalAsync();
        });
    }
}
