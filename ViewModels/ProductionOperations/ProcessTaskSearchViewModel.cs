using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;



namespace IndustrialControlMAUI.ViewModels
{
    public partial class ProcessTaskSearchViewModel : ObservableObject
    {
        private const string PrefKey_LastProcessValue = "ProcessTaskSearch.LastProcessValue";
        private const string PrefKey_LastProcessText = "ProcessTaskSearch.LastProcessText";

        // 进入页面前如果下拉尚未加载，先把“上一次的值”暂存，等列表准备好后再应用
        private string? _pendingLastProcessValue;
        private readonly IWorkOrderApi _workapi;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private string? selectedStatus = "全部";
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 50;
        public ObservableCollection<StatusOption> StatusOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedStatusOption;
        public ObservableCollection<StatusOption> ProcessOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedProcessOption;

        readonly Dictionary<string, string> _statusMap = new();      // 状态：值→中文
        readonly Dictionary<string, string> _orderstatusMap = new();    // 工序：code→name
        private bool _dictsLoaded = false;

        public ObservableCollection<ProcessTask> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        public ProcessTaskSearchViewModel(IWorkOrderApi workapi)
        {
            _workapi = workapi;
            // 读取上次选择（Value 优先，没有则用 Text）
            var lastVal = Preferences.Get(PrefKey_LastProcessValue, null);
            var lastText = Preferences.Get(PrefKey_LastProcessText, null);
            _pendingLastProcessValue = lastVal ?? lastText;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearFilters);
            _ = EnsureDictsLoadedAsync();   // fire-and-forget
           
        }
        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                // 1) 状态下拉：来自 getWorkProcessTaskDictList 的 auditStatus
                var bundle = await _workapi.GetWorkProcessTaskDictListAsync(); // ← 你已有的方法名若不同，替换为现有的
                var auditField = bundle?.result?.FirstOrDefault(x =>
                    string.Equals(x.field, "auditStatus", StringComparison.OrdinalIgnoreCase));

                StatusOptions.Clear();
                _statusMap.Clear();
                //StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
                if (auditField?.dictItems != null)
                {
                    foreach (var d in auditField.dictItems)
                    {
                        var val = d.dictItemValue?.Trim();
                        if (string.IsNullOrWhiteSpace(val)) continue;

                        var name = d.dictItemName ?? val;
                        _statusMap[val] = name; // ★ 建立码→名映射
                        if (name == "待执行" || name == "执行中") {
                            StatusOptions.Add(new StatusOption
                            {
                                Text = name,
                                Value = val
                            });
                        } 
                    }
                    
                }
                SelectedStatusOption ??= StatusOptions.FirstOrDefault();

                // 2) 工序下拉：来自 PmsProcessInfoList?status=1
                var proResp = await _workapi.GetProcessInfoListAsync();
                ProcessOptions.Clear();
                ProcessOptions.Add(new StatusOption { Text = "全部", Value = null }); // 或“不限”
                if (proResp.result != null)
                {
                    foreach (var p in proResp.result)
                    {
                        var code = p.processCode?.Trim();
                        if (string.IsNullOrWhiteSpace(code)) continue;

                        var name = p.processName ?? code;
                        ProcessOptions.Add(new StatusOption
                        {
                            Text = p.processName ?? p.processCode,
                            Value = p.processCode
                        });
                    }
                }
                //3)工单状态
                var orderstatus = await _workapi.GetWorkOrderDictsAsync();
                _orderstatusMap.Clear();
                foreach (var d in orderstatus.AuditStatus)
                    if (!string.IsNullOrWhiteSpace(d.dictItemValue))
                        _orderstatusMap[d.dictItemValue!] = d.dictItemName ?? d.dictItemValue!;
                // 3) ★ 应用“上一次的工序选择”
                ApplyLastProcessSelectionIfAny();
                _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                //if (StatusOptions.Count == 0)
                   // StatusOptions.Add(new StatusOption { Text = "全部", Value = null });
                if (ProcessOptions.Count == 0)
                    ProcessOptions.Add(new StatusOption { Text = "全部", Value = null });
                ApplyLastProcessSelectionIfAny();
                _dictsLoaded = true;
            }
        }
        private void ApplyLastProcessSelectionIfAny()
        {
            if (ProcessOptions.Count == 0) return;

            if (!string.IsNullOrWhiteSpace(_pendingLastProcessValue))
            {
                var hit = ProcessOptions.FirstOrDefault(x =>
                    string.Equals(x.Value, _pendingLastProcessValue, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Text, _pendingLastProcessValue, StringComparison.OrdinalIgnoreCase));

                if (hit != null)
                    SelectedProcessOption = hit;
            }

            // 若仍未命中，则默认选第一项
            SelectedProcessOption ??= ProcessOptions.FirstOrDefault();
        }

        // ★ 当用户改变工序下拉时，立刻持久化
        partial void OnSelectedProcessOptionChanged(StatusOption? oldValue, StatusOption? newValue)
        {
            if (newValue == null) return;
            Preferences.Set(PrefKey_LastProcessValue, newValue.Value ?? string.Empty);
            Preferences.Set(PrefKey_LastProcessText, newValue.Text ?? string.Empty);
        }

        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await EnsureDictsLoadedAsync();   // ★ 先确保字典到位

                var byOrderNo = !string.IsNullOrWhiteSpace(Keyword);
                var statusList = string.IsNullOrWhiteSpace(SelectedStatusOption?.Value)
                ? null
                : new[] { SelectedStatusOption.Value }; // ["0"] 或 ["1"] 等

                var page = await _workapi.PageWorkProcessTasksAsync(
                    workOrderNo: byOrderNo ? Keyword?.Trim() : null,
                    auditStatusList: statusList,
                    processCode: SelectedProcessOption?.Value,                      // 工序下拉 Value（processCode）
                    createdTimeStart: byOrderNo ? null : StartDate.Date,            // 可选
                    createdTimeEnd: byOrderNo ? null : EndDate.Date.AddDays(1).AddSeconds(-1),
                    pageNo: PageIndex,
                    pageSize: PageSize,
                    ct: CancellationToken.None);

                var records = page?.result?.records;
                if (records != null)
                {
                    foreach (var t in records)
                    {
                        if (!string.IsNullOrWhiteSpace(t.AuditStatus) &&
                        _statusMap.TryGetValue(t.AuditStatus, out var sName))
                            t.AuditStatusName = sName;
                        if (!string.IsNullOrWhiteSpace(t.WorkOrderAuditStatus) &&
                       _orderstatusMap.TryGetValue(t.WorkOrderAuditStatus, out var sName2))
                            t.WorkOrderAuditStatus = sName2;
                    }

                    Orders.Clear();
                    foreach (var t in records)
                        Orders.Add(t);
                }
                else {
                    await ShowTip("未查询到任何数据");
                }
            }
            finally { IsBusy = false; }
        }

        private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

        private void ClearFilters()
        {
            Keyword = string.Empty;
            SelectedStatus = "全部";
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
            PageIndex = 1;
            SelectedStatusOption = StatusOptions.FirstOrDefault();
            Orders.Clear();
        }


        // 点击一条工单进入执行页
        [RelayCommand]
        private async Task GoExecuteAsync(ProcessTask? item)
        {
            if (item is null) return;
            await Shell.Current.GoToAsync(nameof(WorkProcessTaskDetailPage) + $"?id={Uri.EscapeDataString(item.Id)}");
        }
    }

}
