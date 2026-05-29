using Android.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;


namespace IndustrialControlMAUI.ViewModels;
public partial class WorkProcessTaskDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkOrderApi _api;
    private readonly IServiceProvider _sp;

    // 状态字典（值→名），用于将 auditStatus 映射为中文
    private readonly Dictionary<string, string> _auditMap = new();
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isPaused;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(CanPauseResume))]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    [NotifyPropertyChangedFor(nameof(CanRework))]
    [NotifyPropertyChangedFor(nameof(PauseResumeText))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(CanAddReport))]
    private TaskRunState state = TaskRunState.NotStarted;
    // ★ 只有 Running（开工/复工后）为 true，其它状态为 false
    public bool IsEditing => State == TaskRunState.Running;

    public bool CanStart => !IsBusy && State == TaskRunState.NotStarted;
    public bool CanPauseResume => !IsBusy && (State == TaskRunState.Running || State == TaskRunState.Paused);
    public bool CanFinish => !IsBusy && State == TaskRunState.Running;
    public bool CanRework => !IsBusy && IsReworkVisible;
    public bool CanAddReport => !IsBusy && State == TaskRunState.Running;

    public string PauseResumeText => State == TaskRunState.Running ? "暂停" : "复工";

    [ObservableProperty] private DetailTab activeTab = DetailTab.Report;
    [ObservableProperty] private bool isInputVisible = false;   // 默认隐藏投料
    [ObservableProperty] private bool isOutputVisible = false; // 默认隐藏产出
    [ObservableProperty] private bool isFrameVisible = false; // 默认隐藏料框
    [ObservableProperty] private bool isReportVisible = true;  // 默认显示报工

    public bool IsReportTab => ActiveTab == DetailTab.Report;
    public bool IsInputTab => ActiveTab == DetailTab.Input;
    public bool IsOutputTab => ActiveTab == DetailTab.Output;
    public bool IsFrameTab => ActiveTab == DetailTab.Frame;


    [ObservableProperty] private WorkProcessTaskDetail? detail;
    [ObservableProperty] private string? queryWorkOrderAuditStatus;
    // 返修按钮显示规则：仅工单状态 1-执行中、2-入库中、4-待入库 时显示
    public bool IsReworkVisible
    {
        get
        {
            var isAuditStatusMatched = (QueryWorkOrderAuditStatus ?? Detail?.workOrderAuditStatus ?? Detail?.auditStatus) is "1" or "2" or "4";
            var userNameForCheck = string.IsNullOrWhiteSpace(CurrentLoginUserName) ? CurrentUserName : CurrentLoginUserName;
            var isBlockedUser = !string.IsNullOrWhiteSpace(userNameForCheck)
                && userNameForCheck.EndsWith("lzyrcy", StringComparison.OrdinalIgnoreCase);
            return isAuditStatusMatched && !isBlockedUser;
        }
    }

    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<TaskMaterialInput> Inputs { get; } = new();
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<TaskMaterialOutput> Outputs { get; } = new();

    // 班次/设备下拉
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<StatusOption> ShiftOptions { get; } = new();
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<StatusOption> DeviceOptions { get; } = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReworkVisible))]
    [NotifyPropertyChangedFor(nameof(CanRework))]
    private string? currentUserName; // 进入页面时赋值实际登录人
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReworkVisible))]
    [NotifyPropertyChangedFor(nameof(CanRework))]
    private string? currentLoginUserName; // 原始登录用户名（用于后缀判断）
    // 投料记录列表（表格2的数据源）
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<MaterialAuRecord> MaterialInputRecords { get; } = new();
    // —— 产出记录列表（表2数据源）
    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<OutputAuRecord> OutputRecords { get; } = new();
    public ObservableCollection<WorkProcessTaskReportRecord> ReportRecords { get; } = new();
    [ObservableProperty] private IReadOnlyList<OutputFrameRecord> outputFrameRecords = Array.Empty<OutputFrameRecord>();

    private int _outputFrameCount;
    private int _selectableOutputFrameCount;
    private int _selectedOutputFrameCount;
    private bool _isBulkUpdatingOutputFrameSelection;
    private bool _isLoadingOutputFrameRecords;

    public int SelectedOutputFrameCount
    {
        get => _selectedOutputFrameCount;
        private set => SetProperty(ref _selectedOutputFrameCount, value);
    }

    public int OutputFrameCount
    {
        get => _outputFrameCount;
        private set => SetProperty(ref _outputFrameCount, value);
    }

    public bool CanBatchApplyOutputFrameInstock => !IsBusy && SelectedOutputFrameCount > 0;

    public bool IsAllOutputFramesSelected
    {
        get => _selectableOutputFrameCount > 0 && SelectedOutputFrameCount == _selectableOutputFrameCount;
        set
        {
            if (_selectableOutputFrameCount == 0 || value == IsAllOutputFramesSelected)
                return;

            _isBulkUpdatingOutputFrameSelection = true;
            try
            {
                foreach (var row in OutputFrameRecords)
                {
                    if (row.CanApplyInstock)
                        row.IsSelected = value;
                }

                SelectedOutputFrameCount = value ? _selectableOutputFrameCount : 0;
            }
            finally
            {
                _isBulkUpdatingOutputFrameSelection = false;
            }

            NotifyOutputFrameSelectionChanged();
        }
    }
    public event EventHandler? TabChanged;

    private TaskMaterialInput? _selectedMaterialItem;
    public TaskMaterialInput? SelectedMaterialItem
    {
        get => _selectedMaterialItem;
        set => SetProperty(ref _selectedMaterialItem, value);
    }
    private TaskMaterialOutput? _selectedOutputItem;
    public TaskMaterialOutput? SelectedOutputItem
    {
        get => _selectedOutputItem;
        set => SetProperty(ref _selectedOutputItem, value);
    }
    public string? ReportQtyText
    {
        get => Detail?.taskReportedQty?.ToString("G29");   // 显示：避免 0 尾
        set
        {
            if (Detail == null) return;
            if (decimal.TryParse(value, out var d))
                Detail.taskReportedQty = d;
            else
                Detail.taskReportedQty = null;
            OnPropertyChanged();                // 刷新自身
            OnPropertyChanged(nameof(Detail));  // 若其他地方也用到了 Detail
        }
    }
    // 上表选中项（应产出计划行）
    [ObservableProperty] private OutputPlanItem? selectedOutputPlanItem;

    private bool _suppressRemoteUpdate = false;

    /// <summary>执行 WorkProcessTaskDetailViewModel 初始化逻辑。</summary>
    public WorkProcessTaskDetailViewModel(IServiceProvider sp, IWorkOrderApi api)
    {
        _api = api;
        _sp = sp;
    }
    private StatusOption? _selectedShift;
    public StatusOption? SelectedShift
    {
        get => _selectedShift;
        set
        {
            if (_selectedShift != value)
            {
                _selectedShift = value;
                OnPropertyChanged();

                // 只有在非抑制阶段，才调用后端更新
                if (!_suppressRemoteUpdate)
                    _ = UpdateShiftAsync(value);
            }
        }
    }

    private StatusOption? _selectedDevice;
    public StatusOption? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_selectedDevice != value)
            {
                _selectedDevice = value;
                OnPropertyChanged();

                if (!_suppressRemoteUpdate)
                    _ = UpdateDeviceAsync(value);
            }
        }
    }
    /// <summary>执行 OnIsBusyChanged 逻辑。</summary>
    partial void OnIsBusyChanged(bool value) => NotifyAllCanExec();
    /// <summary>执行 OnStateChanged 逻辑。</summary>
    partial void OnStateChanged(TaskRunState value) => NotifyAllCanExec();
    partial void OnDetailChanged(WorkProcessTaskDetail? value)
    {
        OnPropertyChanged(nameof(IsReworkVisible));
        OnPropertyChanged(nameof(CanRework));
        (ReworkCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (AddReportCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (BatchApplyOutputFrameInstockCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanBatchApplyOutputFrameInstock));
    }
    partial void OnQueryWorkOrderAuditStatusChanged(string? value)
    {
        OnPropertyChanged(nameof(IsReworkVisible));
        OnPropertyChanged(nameof(CanRework));
        (ReworkCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (AddReportCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (BatchApplyOutputFrameInstockCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanBatchApplyOutputFrameInstock));
    }
    /// <summary>执行 NotifyAllCanExec 逻辑。</summary>
    private void NotifyAllCanExec()
    {
        (StartWorkCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (PauseResumeCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (FinishCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (ReworkCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (AddReportCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (BatchApplyOutputFrameInstockCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanBatchApplyOutputFrameInstock));
    }
    /// <summary>执行 OnActiveTabChanged 逻辑。</summary>
    partial void OnActiveTabChanged(DetailTab value)
    {
        IsReportVisible = (value == DetailTab.Report);
        IsInputVisible = (value == DetailTab.Input);
        IsOutputVisible = (value == DetailTab.Output);
        IsFrameVisible = (value == DetailTab.Frame);

        OnPropertyChanged(nameof(IsReportTab));
        OnPropertyChanged(nameof(IsInputTab));
        OnPropertyChanged(nameof(IsOutputTab));
        OnPropertyChanged(nameof(IsFrameTab));
        TabChanged?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>执行 ApplyQueryAttributes 逻辑。</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workOrderAuditStatus", out var statusObj))
        {
            QueryWorkOrderAuditStatus = statusObj?.ToString();
        }

        if (query.TryGetValue("id", out var v) && v is string id && !string.IsNullOrWhiteSpace(id))
        {
            await InitAsync(id);
        }
    }
    /// <summary>执行 NotifyCanExec 逻辑。</summary>
    private void NotifyCanExec()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanPauseResume));
        OnPropertyChanged(nameof(CanFinish));
        OnPropertyChanged(nameof(CanRework));
    }

    /// <summary>执行 StartWorkAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartWorkAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (Detail?.preStartInspectionEnabled == true)
            {
                var passed = await RunPreStartInspectionAsync();
                if (!passed) return;
            }

            var resp = await _api.StartWorkAsync(Detail.processCode, Detail.workOrderNo, null);
            if (resp.success)
            {
                State = TaskRunState.Running;
                if (!string.IsNullOrWhiteSpace(Detail?.id))
                    await LoadDetailAsync(Detail.id);
                await Shell.Current.DisplayAlert("提示", "开工成功！", "确定");
            }
            else
            {
                await Shell.Current.DisplayAlert("错误", resp.message ?? "开工失败！", "确定");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }



    private async Task<bool> RunPreStartInspectionAsync()
    {
        if (Detail is null) return false;
        return await PreStartInspectionPage.ShowAsync(_api, Detail);
    }

    /// <summary>执行 PauseResumeAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanPauseResume))]
    private async Task PauseResumeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (State == TaskRunState.Running)
            {
                // 当前为“运行中”，点击 => 执行“暂停”
                string title = "填写暂停原因";
                string message = "请填写暂停原因（必填）：";
                string accept = "提交";
                string cancel = "取消";

                // 系统弹窗输入（简洁稳妥）
                string? reason = await Application.Current.MainPage.DisplayPromptAsync(
                    title, message, accept, cancel, null, maxLength: 200, keyboard: Keyboard.Text);

                if (reason is null) return;                 // 点击取消
                reason = reason.Trim();
                if (reason.Length == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("提示", "请填写暂停原因。", "知道了");
                    return;
                }

                var resp = await _api.PauseWorkAsync(Detail.processCode, Detail.workOrderNo, reason);
                if (resp.success)
                {
                    State = TaskRunState.Paused;
                    await Application.Current.MainPage.DisplayAlert("成功", "已暂停。", "确定");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("失败", resp.message ?? "暂停失败", "确定");
                }
            }
            else if (State == TaskRunState.Paused)
            {
                // 当前为“已暂停”，点击 => 执行“恢复”
                bool go = await Application.Current.MainPage.DisplayAlert("确认", "确定恢复生产吗？", "恢复", "取消");
                if (!go) return;

                var resp = await _api.PauseWorkAsync(Detail.processCode, Detail.workOrderNo, null);
                if (resp.success)
                {
                    State = TaskRunState.Running;
                    await Application.Current.MainPage.DisplayAlert("成功", "已恢复生产。", "确定");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("失败", resp.message ?? "恢复失败", "确定");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>执行 FinishAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanFinish))]
    private async Task FinishAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            string? memo = null;
            decimal? actQty = null;

            if (Detail?.finalProcess == true)
            {
                var popupResult = await FinalProcessCompletePopupPage.ShowAsync(null);
                if (popupResult is null) return;
                memo = popupResult.Memo;
                actQty = popupResult.ActQty;
            }

            var resp = await _api.CompleteWorkAsync(Detail.processCode, Detail.workOrderNo, memo, actQty);
            if (resp.success)
            {
                State = TaskRunState.Finished;
                //await Task.CompletedTask;
                await Shell.Current.DisplayAlert("提示", "完工成功！", "确定");
            }
            else
            {
                await Shell.Current.DisplayAlert("错误", resp.message ?? "完工失败！", "确定");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("异常", ex.Message, "确定");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>执行 ReworkAsync 逻辑。</summary>
    [RelayCommand(CanExecute = nameof(CanRework))]
    private async Task ReworkAsync()
    {
        if (string.IsNullOrWhiteSpace(Detail?.workOrderNo))
        {
            await Shell.Current.DisplayAlert("提示", "缺少工单号，无法进入返修页面。", "确定");
            return;
        }

        await Shell.Current.GoToAsync(nameof(Pages.ReworkOrderPage), new Dictionary<string, object>
        {
            ["workOrderNo"] = Detail.workOrderNo
        });
    }


    [RelayCommand]
    public void ShowReport()
    {
        Debug.WriteLine("切换到报工");
        ActiveTab = DetailTab.Report;
        _ = LoadReportRecordsAsync();
    }

    /// <summary>执行 ShowInput 逻辑。</summary>
    [RelayCommand]
    public void ShowInput()
    {
        Debug.WriteLine("切换到投料");
        ActiveTab = DetailTab.Input;
    }

    /// <summary>执行 ShowOutput 逻辑。</summary>
    [RelayCommand]
    public void ShowOutput()
    {
        Debug.WriteLine("切换到产出");
        ActiveTab = DetailTab.Output;  // 同上
    }

    [RelayCommand]
    public void ShowFrame()
    {
        Debug.WriteLine("切换到料框");
        ActiveTab = DetailTab.Frame;

        if (OutputFrameCount == 0 && !_isLoadingOutputFrameRecords)
            _ = LoadOutputFrameRecordsAsync();
    }

    /// <summary>执行 InitAsync 逻辑。</summary>
    [RelayCommand]
    private async Task InitAsync(string id)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await LoadAuditDictAsync();
            await LoadDetailAsync(id);
            ActiveTab = DetailTab.Report; // 会同步设置各 Tab 可见性
            CurrentLoginUserName = Preferences.Get("UserName", string.Empty);
            CurrentUserName = CurrentLoginUserName.Split('@')[0]; // 页面显示名
            await LoadReportRecordsAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadReportRecordsAsync()
    {
        if (Detail is null || string.IsNullOrWhiteSpace(Detail.processCode) || string.IsNullOrWhiteSpace(Detail.workOrderNo))
            return;

        var resp = await _api.PageWorkProcessTaskReports(
            processCode: Detail.processCode!,
            workOrderNo: Detail.workOrderNo!);

        ReportRecords.Clear();
        if (resp?.result?.records != null)
        {
            foreach (var item in resp.result.records)
                ReportRecords.Add(item);
        }
    }

    /// <summary>执行 LoadAuditDictAsync 逻辑。</summary>
    private async Task LoadAuditDictAsync()
    {
        // 你已有：/normalService/pda/pmsWorkOrder/getWorkProcessTaskDictList
        var dict = await _api.GetWorkProcessTaskDictListAsync();
        _auditMap.Clear();
        var audit = dict.result?.FirstOrDefault(x => string.Equals(x.field, "auditStatus", StringComparison.OrdinalIgnoreCase));
        if (audit?.dictItems != null)
        {
            foreach (var d in audit.dictItems)
            {
                if (!string.IsNullOrWhiteSpace(d.dictItemValue))
                    _auditMap[d.dictItemValue!] = d.dictItemName ?? d.dictItemValue!;
            }
        }
    }

    private static TaskRunState MapStateFromPeriodExecute(string? periodExecute)
    {
        var raw = periodExecute?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return TaskRunState.NotStarted;

        var exec = raw!.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        return exec switch
        {
            "working" or "work" or "running" or "startwork" or "start" or "resume" or "resumework"
                => TaskRunState.Running,
            "pause" or "paused" or "suspend"
                => TaskRunState.Paused,
            "complete" or "completed" or "finish" or "finished" or "end"
                => TaskRunState.Finished,
            _ => TaskRunState.NotStarted
        };
    }

    /// <summary>执行 LoadDetailAsync 逻辑。</summary>
    private async Task LoadDetailAsync(string id)
    {
        var resp = await _api.GetWorkProcessTaskDetailAsync(id);
        if (resp.success && resp.result != null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Detail = resp.result;                 // 若 Detail 有 setter 里 OnPropertyChanged(); 更好
                OnPropertyChanged(nameof(Detail));    // 确保 Detail.* 绑定能刷新
                OnPropertyChanged(nameof(ReportQtyText)); // 让 Entry 立刻拿到最新文本
            });

            // 映射中文名
            if (!string.IsNullOrWhiteSpace(Detail.auditStatus) &&
                _auditMap.TryGetValue(Detail.auditStatus, out var s))
            {
                Detail.AuditStatusName = s;
            }
            State = MapStateFromPeriodExecute(resp.result.periodExecute);
                // 关键：加载下拉选项
            await LoadShiftsAsync();
            await LoadDevicesAsync();
            await LoadMaterialInputsAsync();
            await LoadOutputInputsAsync();
            await LoadOutputFrameRecordsAsync();

            // 关键：抑制更新 → 设定选中项
            _suppressRemoteUpdate = true;
            try
            {
                // 班次
                if (!string.IsNullOrWhiteSpace(Detail.teamCode))
                {
                    var shiftOpt = ShiftOptions.FirstOrDefault(x => x.Value == Detail.teamCode)
                                   ?? ShiftOptions.FirstOrDefault(); // 找不到就给“请选择”
                    SelectedShift = shiftOpt;
                }
                else
                {
                    SelectedShift = ShiftOptions.FirstOrDefault(); // “请选择”
                }

                // 设备
                if (!string.IsNullOrWhiteSpace(Detail.productionMachine))
                {
                    var devOpt = DeviceOptions.FirstOrDefault(x => x.Value == Detail.productionMachine)
                                 ?? DeviceOptions.FirstOrDefault();
                    SelectedDevice = devOpt;
                }
                else
                {
                    SelectedDevice = DeviceOptions.FirstOrDefault();
                }
            }
            finally
            {
                _suppressRemoteUpdate = false; // 解除抑制
            }
        }
        else
        {
            // 可视化提示由页面处理
        }
    }

    /// <summary>执行 LoadShiftsAsync 逻辑。</summary>
    private async Task LoadShiftsAsync()
    {
        ShiftOptions.Clear();
        if (Detail != null && Detail.factoryCode != null && Detail.factoryCode != null)
        {
            var resp = await _api.GetShiftOptionsAsync(Detail.factoryCode, Detail.workShop);
            // 默认加一个“请选择”
            ShiftOptions.Add(new StatusOption { Text = "请选择", Value = null });
            if (resp != null)
            {
                foreach (var o in resp.result ?? new())
                    ShiftOptions.Add(new StatusOption { Text = o.workshopsName ?? o.workshopsCode, Value = o.workshopsCode });
            }
        }
    }

    /// <summary>执行 LoadDevicesAsync 逻辑。</summary>
    private async Task LoadDevicesAsync()
    {
        DeviceOptions.Clear();
        if (Detail != null && Detail.factoryCode != null && Detail.processCode != null)
        {
            var resp = await _api.GetDeviceOptionsAsync(Detail.factoryCode, Detail.processCode);
            DeviceOptions.Add(new StatusOption { Text = "请选择", Value = null });
            if (resp != null)
            {
                foreach (var o in resp.result ?? new())
                    DeviceOptions.Add(new StatusOption { Text = o.deviceName ?? o.deviceCode, Value = o.deviceCode });
            }
        }
    }

    /// <summary>执行 LoadMaterialInputsAsync 逻辑。</summary>
    private async Task LoadMaterialInputsAsync()
    {
        // ② 调用接口
        var resp = await _api.PageWorkProcessTaskMaterialInputs(
            factoryCode: Detail.factoryCode,
            processCode: Detail.processCode!,
            workOrderNo: Detail.workOrderNo!,
            pageNo: 1,
            pageSize: 10
        );

        // ③ 判断返回是否成功并绑定
        MaterialInputRecords.Clear();

        if (resp?.result?.records != null)
        {
            foreach (var item in resp.result.records)
                MaterialInputRecords.Add(item);
        }
    }

    /// <summary>执行 LoadOutputInputsAsync 逻辑。</summary>
    private async Task LoadOutputInputsAsync()
    {
        // ② 调用接口
        var resp = await _api.PageWorkProcessTaskOutputs(
            factoryCode: Detail.factoryCode,
            processCode: Detail.processCode!,
            workOrderNo: Detail.workOrderNo!,
            pageNo: 1,
            pageSize: 10
        );

        // ③ 判断返回是否成功并绑定
        OutputRecords.Clear();

        if (resp?.result?.records != null)
        {
            foreach (var item in resp.result.records)
                OutputRecords.Add(item);
        }
    }
    private async Task LoadOutputFrameRecordsAsync()
    {
        if (_isLoadingOutputFrameRecords
            || Detail is null
            || string.IsNullOrWhiteSpace(Detail.processCode)
            || string.IsNullOrWhiteSpace(Detail.schemeNo)
            || string.IsNullOrWhiteSpace(Detail.workOrderNo))
            return;

        _isLoadingOutputFrameRecords = true;
        try
        {
            var resp = await _api.ListOutputFrameRecordsAsync(
                processCode: Detail.processCode!,
                schemeNo: Detail.schemeNo!,
                workOrderNo: Detail.workOrderNo!);

            foreach (var old in OutputFrameRecords)
                old.PropertyChanged -= OnOutputFramePropertyChanged;

            var records = new List<OutputFrameRecord>();
            var selectableCount = 0;
            if (resp?.result != null)
            {
                foreach (var item in resp.result)
                {
                    item.IsSelected = false;
                    item.PropertyChanged += OnOutputFramePropertyChanged;
                    records.Add(item);

                    if (item.CanApplyInstock)
                        selectableCount++;
                }
            }

            OutputFrameRecords = records;
            OutputFrameCount = records.Count;
            _selectableOutputFrameCount = selectableCount;
            SelectedOutputFrameCount = 0;
            NotifyOutputFrameSelectionChanged();
        }
        finally
        {
            _isLoadingOutputFrameRecords = false;
        }
    }

    private void OnOutputFramePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isBulkUpdatingOutputFrameSelection || e.PropertyName != nameof(OutputFrameRecord.IsSelected))
            return;

        if (sender is OutputFrameRecord row)
            SelectedOutputFrameCount += row.IsSelected ? 1 : -1;

        NotifyOutputFrameSelectionChanged();
    }

    private void NotifyOutputFrameSelectionChanged()
    {
        OnPropertyChanged(nameof(IsAllOutputFramesSelected));
        OnPropertyChanged(nameof(CanBatchApplyOutputFrameInstock));
        (BatchApplyOutputFrameInstockCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanBatchApplyOutputFrameInstock))]
    private async Task BatchApplyOutputFrameInstockAsync()
    {
        var ids = OutputFrameRecords
            .Where(x => x.IsSelected && x.CanApplyInstock && !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id!)
            .ToList();

        if (ids.Count == 0)
        {
            await ShowTip("请选择待申请的料框。");
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _api.BatchApplyOutputFrameInstockAsync(ids);
            if (!resp.success)
            {
                await ShowTip(string.IsNullOrWhiteSpace(resp.message) ? "批量申请入库失败" : resp.message!);
                return;
            }

            await ShowTip("批量申请入库成功");
            await LoadOutputFrameRecordsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>执行 UpdateShiftAsync 逻辑。</summary>
    private async Task UpdateShiftAsync(StatusOption? opt)
    {
        if (opt == null || string.IsNullOrWhiteSpace(Detail?.id))
            return;

        try
        {
            IsBusy = true;

            var r = await _api.UpdateWorkProcessTaskAsync(
            id: Detail.id, null, null, null, teamCode: opt.Value, teamName: opt.Text, null, null, null, default);

            if (!r.Succeeded)
                await ShowTip(string.IsNullOrWhiteSpace(r.Message) ? "更新班次失败" : r.Message);
            else
                await ShowTip("班次已更新");
        }
        catch (Exception ex)
        {
            await ShowTip($"更新班次异常：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>执行 UpdateDeviceAsync 逻辑。</summary>
    private async Task UpdateDeviceAsync(StatusOption? opt)
    {
        // 基本校验：必须有任务ID和选中项
        if (opt == null || string.IsNullOrWhiteSpace(Detail?.id))
            return;

        try
        {
            IsBusy = true;

            var r = await _api.UpdateWorkProcessTaskAsync(
            id: Detail.id, opt.Value, opt.Text, null, null, null, null, null, null, default);

            if (!r.Succeeded)
                await ShowTip(string.IsNullOrWhiteSpace(r.Message) ? "更新设备失败" : r.Message);
            else
                await ShowTip("设备已更新");
        }
        catch (Exception ex)
        {
            await ShowTip($"更新设备异常：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// <summary>执行 ShowTip 逻辑。</summary>
    private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

    /// <summary>执行 SubmitReportQtyAsync 逻辑。</summary>
    [RelayCommand]
    private async Task SubmitReportQtyAsync()
    {
        if (string.IsNullOrWhiteSpace(ReportQtyText))
        {
            await Shell.Current.DisplayAlert("提示", "请输入数量", "OK");
            return;
        }

        var qty = int.TryParse(ReportQtyText, out var value) ? value : 0;
        if (qty <= 0)
        {
            await Shell.Current.DisplayAlert("提示", "数量必须大于 0", "OK");
            return;
        }

        // 调用接口
        var r = await _api.UpdateWorkProcessTaskAsync(
            id: Detail.id, null, null, qty, null, null, null, null, null, default);

        if (!r.Succeeded)
            await ShowTip(string.IsNullOrWhiteSpace(r.Message) ? "更新报工数量失败" : r.Message);
        else
            await ShowTip("报工数量已更新");
    }

    [RelayCommand(CanExecute = nameof(CanAddReport))]
    private async Task AddReportAsync()
    {
        if (Detail is null) return;
        var ok = await ReportAddPopupPage.ShowAsync(null, Detail);
        if (ok)
        {
            await LoadReportRecordsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteReportAsync(WorkProcessTaskReportRecord? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.id)) return;
        var ok = await Shell.Current.DisplayAlert("确认", "确定删除该报工记录吗？", "确定", "取消");
        if (!ok) return;

        var resp = await _api.DeleteWorkProcessTaskReportAsync(new DeleteWorkProcessTaskReportReq { id = row.id });
        if (!resp.success)
        {
            await ShowTip(resp.message ?? "删除报工记录失败");
            return;
        }

        await ShowTip("删除成功");
        await LoadReportRecordsAsync();
    }

    // 点击“新增投料”
    /// <summary>执行 AddMaterialInputAsync 逻辑。</summary>
    [RelayCommand]
    private async Task AddMaterialInputAsync()
    {
        // 准备列表（用于“无预设”时给用户选择）
        var list = Detail?.materialInputList ?? Enumerable.Empty<TaskMaterialInput>();

        // 预设物料（有选中就作为预设；否则传 null 让用户自行选择）
        TaskMaterialInput? preset = SelectedMaterialItem is null
            ? null
            : new TaskMaterialInput
            {
                materialCode = SelectedMaterialItem.materialCode,
                materialName = SelectedMaterialItem.materialName
            };

        // 打开弹窗（新重载）：有预设则只输入数量/备注；无预设则先选物料再输入
        var picked = await MaterialInputPopupPage.ShowAsync(_sp, list, preset);
        if (picked is null) return;

        // 统一取“最终物料信息”（优先用预设；没有预设时用弹窗选择结果）
        var finalCode = preset?.materialCode ?? picked.MaterialCode;
        var finalName = preset?.materialName ?? picked.MaterialName;

        // 组装请求
        var req = new AddWorkProcessTaskMaterialInputReq
        {   materialClassName= picked.materialClassName,
            materialCode = finalCode,
            materialName = finalName,
            materialTypeName = picked.materialTypeName,
            qty = (double)picked.Quantity,                    // 从弹窗取
            memo = picked.Memo,
            unit = picked.Unit,
            workOrderNo = Detail.workOrderNo,
            processCode = Detail.processCode,
            processName = Detail.processName,
            schemeNo = Detail.schemeNo,
            platPlanNo = Detail.platPlanNo
        };

        var resp = await _api.AddWorkProcessTaskMaterialInputAsync(req);
        if (!resp.success)
        {
            await Shell.Current.DisplayAlert("失败", resp.message ?? "提交失败", "OK");
            return;
        }

        // 成功：插入下表顶部
        //var idx = (MaterialInputRecords.Count == 0) ? 1 : (MaterialInputRecords[0].Index + 1);
        //MaterialInputRecords.Insert(0, new MaterialAuRecord
        //{
        //    //Index = idx,
        //    MaterialName = finalName,
        //    Unit = picked.Unit,
        //    Qty =  picked.Quantity,
        //    OperateTime = picked.OperationTime?.ToString("yyyy-MM-dd HH:mm:ss"),
        //    Memo = picked.Memo
        //});
        SelectedMaterialItem = null;
        await LoadMaterialInputsAsync();
    }


    // 删除（仅前端）
    /// <summary>执行 DeleteMaterialInput 逻辑。</summary>
    [RelayCommand]
    private async void DeleteMaterialInput(MaterialAuRecord row)
    {
        if (row == null) return;
        MaterialInputRecords.Remove(row);
        // 如需后端删除，在此调用删除接口
        var resp = await _api.DeleteWorkProcessTaskMaterialInputAsync(row.Id);
        if (!resp.success)
        {
            await Shell.Current.DisplayAlert("失败", resp.message ?? "提交失败", "OK");
            return;
        }
    }

    // 新增产出：只用选中行 + 弹窗返回的数量/备注
    /// <summary>执行 AddOutputAsync 逻辑。</summary>
    [RelayCommand]
    private async Task AddOutputAsync()
    {
        // 准备列表（用于“无预设”时给用户选择）
        var list = Detail?.materialOutputList ?? Enumerable.Empty<TaskMaterialOutput>();

        // 预设物料（有选中就作为预设；否则传 null 让用户自行选择）
        TaskMaterialOutput? preset = SelectedOutputItem is null
            ? null
            : new TaskMaterialOutput
            {
                materialClassName = SelectedOutputItem.materialClassName,
                materialCode = SelectedOutputItem.materialCode,
                materialName = SelectedOutputItem.materialName,
                materialTypeName = SelectedOutputItem.materialTypeName,
                unit = SelectedOutputItem.unit
            };

        // 打开弹窗（新重载）：有预设则只输入数量/备注；无预设则先选物料再输入
        var picked = await OutputPopupPage.ShowAsync(_sp, list, preset);
        if (picked is null) return;

        // 统一取“最终物料信息”（优先用预设；没有预设时用弹窗选择结果）
        var finalCode = preset?.materialCode ?? picked.MaterialCode;
        var finalName = preset?.materialName ?? picked.MaterialName;

        if (Detail is null)
        {
            await Shell.Current.DisplayAlert("提示", "工序任务详情未加载，无法提交产出。", "OK");
            return;
        }

        // 组装请求：字段与 /pda/pmsWorkOrder/addWorkProcessTaskMaterialOutput 接口保持一致
        var req = new AddWorkProcessTaskProductOutputReq
        {
            materialClassName = picked.materialClassName,
            materialCode = finalCode,
            materialName = finalName,
            materialTypeName = picked.materialTypeName,
            qty = (double)picked.Quantity,                    // 从弹窗取
            memo = picked.Memo,
            unit = picked.Unit,
            workOrderNo = Detail.workOrderNo ?? string.Empty,
            processCode = Detail.processCode,
            processName = Detail.processName,
            schemeNo = Detail.schemeNo,
            platPlanNo = Detail.platPlanNo,
            outputFrameList = picked.frameNoList
        };

        ApiResp<bool> resp;
        try
        {
            resp = await _api.AddWorkProcessTaskProductOutputAsync(req);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("失败", $"提交产出异常：{ex.Message}", "OK");
            return;
        }

        if (!resp.success)
        {
            await Shell.Current.DisplayAlert("失败", resp.message ?? "提交失败", "OK");
            return;
        }

        // 成功：插入下表顶部
        //var idx = (OutputRecords.Count == 0) ? 1 : (OutputRecords[0].Index + 1);
        //OutputRecords.Insert(0, new OutputAuRecord
        //{
        //    //Index = idx,
        //    MaterialName = finalName,
        //    Unit = picked.Unit,
        //    Qty = picked.Quantity,
        //    OperateTime = picked.OperationTime?.ToString("yyyy-MM-dd HH:mm:ss"),
        //    Memo = picked.Memo
        //});
       
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ActiveTab = DetailTab.Output;
        });
        await LoadOutputInputsAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ActiveTab = DetailTab.Output;
        });
        SelectedOutputItem = null;
    }

    // 删除（前端移除；如需后端删除在此补接口）
    /// <summary>执行 DeleteOutput 逻辑。</summary>
    [RelayCommand]
    private async void DeleteOutput(OutputAuRecord row)
    {
        if (row == null) return;
        OutputRecords.Remove(row);
        //调用删除接口
        var resp = await _api.DeleteWorkProcessTaskOutputAsync(row.Id);
        if (!resp.success)
        {
            await Shell.Current.DisplayAlert("失败", resp.message ?? "提交失败", "OK");
            return;
        }
    }

    /// <summary>执行 MaterialItemSelected 逻辑。</summary>
    [RelayCommand]
    private void MaterialItemSelected(TaskMaterialInput? item)
    {
        if (item != null && !ReferenceEquals(SelectedMaterialItem, item))
            SelectedMaterialItem = item;
    }

    /// <summary>执行 OutputItemSelected 逻辑。</summary>
    [RelayCommand]
    private void OutputItemSelected(TaskMaterialOutput? item)
    {
        if (item != null && !ReferenceEquals(SelectedOutputItem, item))
            SelectedOutputItem = item;
    }

    /// <summary>执行 EditMaterialRawDate 逻辑。</summary>
    [RelayCommand]
    private async Task EditMaterialRawDate(MaterialAuRecord row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.Id))
        {
            await ShowTip("缺少记录主键，无法编辑。");
            return;
        }

        // 解析当前行已有日期作为默认值
        DateTime? init = null;
        if (!string.IsNullOrWhiteSpace(row.RawMaterialProductionDate)
            && DateTime.TryParse(row.RawMaterialProductionDate, out var parsed))
            init = parsed;

        // 打开日期时间选择弹窗
        var picked = await DateTimePickerPage.ShowAsync(init);
        if (picked is null) return; // 用户取消

        // 转为后端需要的格式
        var rawStr = picked.Value.ToString("yyyy-MM-dd HH:mm:ss");

        // 调用后端
        var resp = await _api.EditWorkProcessTaskMaterialInputAsync(
            id: row.Id!,
            qty: row.Qty,              // 不改数量就把现值带回去
            memo: row.Memo,
            rawMaterialProductionDate: rawStr
        );

        if (resp.success)
        {
            // 本地更新并通知UI
            row.RawMaterialProductionDate = rawStr;
            // 如果 MaterialRecord 未实现 INotifyPropertyChanged，可：
            var i = MaterialInputRecords.IndexOf(row);
            if (i >= 0) { MaterialInputRecords[i] = row; }
            await ShowTip("已更新原料生产日期。");
            await LoadMaterialInputsAsync();
        }
        else
        {
            await ShowTip(string.IsNullOrWhiteSpace(resp.message) ? "更新失败" : resp.message!);
        }
    }
}
