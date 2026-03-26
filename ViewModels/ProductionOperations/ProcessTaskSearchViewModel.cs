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

        /// <summary>执行 new 逻辑。</summary>
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? keyword;
        [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime endDate = DateTime.Today;
        [ObservableProperty] private int pageIndex = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasMore = true;
        public ObservableCollection<StatusFilterOption> StatusOptions { get; } = new();
        [ObservableProperty] private string selectedStatusSummary = "待执行";
        [ObservableProperty] private bool isStatusDropdownOpen;
        public ObservableCollection<StatusOption> ProcessOptions { get; } = new();
        [ObservableProperty] private StatusOption? selectedProcessOption;

        readonly Dictionary<string, string> _statusMap = new();      // 状态：值→中文
        readonly Dictionary<string, string> _orderstatusMap = new();    // 工序：code→name
        private bool _dictsLoaded = false;
        private bool _navigateToDeviceBind;
        private bool _isNavigating;

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<ProcessTask> Orders { get; } = new();

        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearCommand { get; }

        /// <summary>执行 ProcessTaskSearchViewModel 初始化逻辑。</summary>
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
        /// <summary>执行 EnsureDictsLoadedAsync 逻辑。</summary>
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
                        var option = new StatusFilterOption
                        {
                            Text = name,
                            Value = val,
                            IsSelected = name == "待执行" || name == "执行中"
                        };
                        option.PropertyChanged += OnStatusOptionPropertyChanged;
                        StatusOptions.Add(option);
                    }
                }
                UpdateSelectedStatusSummary();

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
                foreach (var d in orderstatus.AuditStatus ?? Enumerable.Empty<DictItem>())
                    if (!string.IsNullOrWhiteSpace(d.dictItemValue))
                        _orderstatusMap[d.dictItemValue!.Trim()] = d.dictItemName ?? d.dictItemValue!;
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
        /// <summary>执行 ApplyLastProcessSelectionIfAny 逻辑。</summary>
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
        /// <summary>执行 OnSelectedProcessOptionChanged 逻辑。</summary>
        partial void OnSelectedProcessOptionChanged(StatusOption? oldValue, StatusOption? newValue)
        {
            if (newValue == null) return;
            Preferences.Set(PrefKey_LastProcessValue, newValue.Value ?? string.Empty);
            Preferences.Set(PrefKey_LastProcessText, newValue.Text ?? string.Empty);
        }

        /// <summary>执行 SearchAsync 逻辑。</summary>
        public async Task SearchAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await EnsureDictsLoadedAsync();   // ★ 先确保字典到位

                PageIndex = 1;
                Orders.Clear();
                var records = await LoadPageAsync(PageIndex);
                if (records.Count == 0)
                {
                    await ShowTip("未查询到任何数据");
                }
                else
                {
                    foreach (var t in records)
                        Orders.Add(t);
                }

                HasMore = records.Count >= PageSize;
            }
            finally { IsBusy = false; }
        }

        /// <summary>执行 LoadMoreAsync 逻辑。</summary>
        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsBusy || IsLoadingMore || !HasMore) return;

            try
            {
                IsLoadingMore = true;
                PageIndex++;
                var records = await LoadPageAsync(PageIndex);
                foreach (var t in records)
                    Orders.Add(t);
                HasMore = records.Count >= PageSize;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>执行 LoadPageAsync 逻辑。</summary>
        private async Task<List<ProcessTask>> LoadPageAsync(int pageNo)
        {
            var byOrderNo = !string.IsNullOrWhiteSpace(Keyword);
            var statusList = StatusOptions
                .Where(x => x.IsSelected && !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Value!)
                .ToArray();

            var page = await _workapi.PageWorkProcessTasksAsync(
                workOrderNo: byOrderNo ? Keyword?.Trim() : null,
                auditStatusList: statusList.Length == 0 ? null : statusList,
                processCode: SelectedProcessOption?.Value,
                createdTimeStart: byOrderNo ? null : StartDate.Date,
                createdTimeEnd: byOrderNo ? null : EndDate.Date.AddDays(1).AddSeconds(-1),
                pageNo: pageNo,
                pageSize: PageSize,
                ct: CancellationToken.None);

            var records = page?.result?.records ?? new List<ProcessTask>();
            foreach (var t in records)
            {
                if (!string.IsNullOrWhiteSpace(t.AuditStatus) &&
                _statusMap.TryGetValue(t.AuditStatus, out var sName))
                    t.AuditStatusName = sName;

                t.WorkOrderAuditStatusName = GetWorkOrderAuditStatusName(t.WorkOrderAuditStatus);
            }

            return records;
        }

        private string? GetWorkOrderAuditStatusName(string? statusCode)
        {
            var code = statusCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                return code;

            if (_orderstatusMap.TryGetValue(code, out var statusName) && !string.IsNullOrWhiteSpace(statusName))
                return statusName;

            return code switch
            {
                "0" => "待执行",
                "1" => "执行中",
                "2" => "入库中",
                "3" => "已完成",
                _ => code
            };
        }
        /// <summary>执行 ShowTip 逻辑。</summary>
        private Task ShowTip(string message) =>
           Shell.Current?.DisplayAlert("提示", message, "确定") ?? Task.CompletedTask;

        /// <summary>执行 ClearFilters 逻辑。</summary>
        private void ClearFilters()
        {
            Keyword = string.Empty;
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
            PageIndex = 1;
            HasMore = true;
            foreach (var opt in StatusOptions)
                opt.IsSelected = opt.Text == "待执行" || opt.Text == "执行中";
            UpdateSelectedStatusSummary();
            Orders.Clear();
        }

        private void OnStatusOptionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StatusFilterOption.IsSelected))
                UpdateSelectedStatusSummary();
        }

        private void UpdateSelectedStatusSummary()
        {
            var selected = StatusOptions.Where(x => x.IsSelected).Select(x => x.Text).ToList();
            SelectedStatusSummary = selected.Count == 0 ? "请选择状态" : selected[0];
        }


        // 点击一条工单进入执行页
        /// <summary>执行 GoExecuteAsync 逻辑。</summary>
        [RelayCommand]
        private async Task GoExecuteAsync(ProcessTask? item)
        {
            if (item is null) return;
            if (_isNavigating) return;

            try
            {
                _isNavigating = true;

                if (_navigateToDeviceBind)
                {
                    await Shell.Current.GoToAsync(nameof(DeviceScanBindPage), new Dictionary<string, object>
                    {
                        ["taskId"] = item.Id ?? string.Empty,
                        ["workOrderNo"] = item.WorkOrderNo ?? string.Empty,
                        ["workOrderName"] = item.WorkOrderName ?? string.Empty,
                        ["materialName"] = item.MaterialName ?? string.Empty,
                        ["processName"] = item.ProcessName ?? string.Empty,
                        ["scheQty"] = item.ScheQty?.ToString("G29") ?? string.Empty,
                        ["factoryCode"] = item.FactoryCode ?? string.Empty,
                        ["processCode"] = item.ProcessCode ?? string.Empty,
                        ["schemeNo"] = item.SchemeNo ?? string.Empty,
                        ["platPlanNo"] = item.PlatPlanNo ?? string.Empty,
                        ["lineCode"] = item.Line ?? string.Empty
                    });
                    return;
                }

                await Shell.Current.GoToAsync(nameof(WorkProcessTaskDetailPage) + $"?id={Uri.EscapeDataString(item.Id ?? string.Empty)}");
            }
            catch (Exception ex)
            {
                await ShowTip($"页面跳转失败：{ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        public void SetEntryMode(string? mode)
        {
            _navigateToDeviceBind = string.Equals(mode, "deviceBinding", StringComparison.OrdinalIgnoreCase);
        }
    }

    public partial class StatusFilterOption : ObservableObject
    {
        public string Text { get; set; } = "";
        public string? Value { get; set; }
        [ObservableProperty] private bool isSelected;
    }

}
