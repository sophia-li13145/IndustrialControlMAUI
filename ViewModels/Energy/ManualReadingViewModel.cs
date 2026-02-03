using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;

namespace IndustrialControl.ViewModels.Energy
{
    /// <summary>图2：手动抄表 VM</summary>
    public partial class ManualReadingViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEnergyApi _api;
        private readonly CancellationTokenSource _cts = new();
        private System.Timers.Timer? _searchTimer;
        private List<IdNameOption> _allUsers = new();
        private bool _suppressSearch = false; // true 表示暂时不触发下拉

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<MeterPointItem> MeterPoints { get; } = new();

        [ObservableProperty] private MeterPointItem? selectedMeterPoint;
        [ObservableProperty] private bool isBusy;

        /// <summary>执行 ManualReadingViewModel 初始化逻辑。</summary>
        public ManualReadingViewModel(IEnergyApi api)
        {
            _api = api;
        }

        [ObservableProperty] private ManualReadingForm form = new();

        /// <summary>执行 ToString 逻辑。</summary>
        public string ReadingTimeText => Form.ReadingTime.ToString("yyyy-MM-dd HH:mm:ss");

        // ===== 抄表人模糊搜索 =====
        /// <summary>执行 new 逻辑。</summary>
        [ObservableProperty] private string? readerQuery;
        public ObservableCollection<IdNameOption> ReaderSuggestions { get; } = new();
        [ObservableProperty] private bool isReaderDropdownOpen;

        /// <summary>执行 OnReaderQueryChanged 逻辑。</summary>
        partial void OnReaderQueryChanged(string? value)
        {
            // 如果当前是代码赋值（初始化阶段），直接跳过搜索
            if (_suppressSearch)
                return;

            _searchTimer?.Stop();
            _searchTimer ??= new System.Timers.Timer(300) { AutoReset = false };
            _searchTimer.Elapsed -= OnSearchTimerElapsed;
            _searchTimer.Elapsed += OnSearchTimerElapsed;
            _searchTimer.Start();
        }


        /// <summary>执行 OnSearchTimerElapsed 逻辑。</summary>
        private async void OnSearchTimerElapsed(object? s, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_allUsers.Count == 0)
                    _allUsers = (await _api.GetUsersAsync(_cts.Token)).ToList();

                var key = ReaderQuery?.Trim() ?? "";
                var q = _allUsers.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(key))
                    q = q.Where(x => x.Name.Contains(key, StringComparison.OrdinalIgnoreCase));

                var items = q.Take(20).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ReaderSuggestions.Clear();
                    foreach (var it in items) ReaderSuggestions.Add(it);
                    IsReaderDropdownOpen = ReaderSuggestions.Count > 0;
                });
            }
            catch { /* ignore */ }
        }

        /// <summary>执行 PickReader 逻辑。</summary>
        [RelayCommand]
        private void PickReader(IdNameOption option)
        {
            Form.ReaderName = option.Name;
            ReaderQuery = option.Name;
            IsReaderDropdownOpen = false;
            OnPropertyChanged(nameof(Form));
        }

        /// <summary>执行 CloseReaderDropdown 逻辑。</summary>
        [RelayCommand]
        private void CloseReaderDropdown() => IsReaderDropdownOpen = false;

        /// <summary>执行 EnsureUsersAsync 逻辑。</summary>
        public async Task EnsureUsersAsync()
        {
            if (_allUsers.Count == 0)
                _allUsers = (await _api.GetUsersAsync(_cts.Token)).ToList();

            var loginUserName = Preferences.Get("UserName", string.Empty);

            if (!string.IsNullOrWhiteSpace(loginUserName))
            {
                var hit = _allUsers.FirstOrDefault(u =>
                    string.Equals(u.UserName, loginUserName, StringComparison.OrdinalIgnoreCase));

                if (hit != null)
                {
                    var displayName = string.IsNullOrWhiteSpace(hit.Name) ? hit.UserName : hit.Name;

                    _suppressSearch = true; // ← 临时屏蔽搜索
                    Form.ReaderName = displayName;
                    ReaderQuery = displayName;
                    _suppressSearch = false; // ← 恢复监听

                    OnPropertyChanged(nameof(Form));
                }
            }
        }



        // ===== 接收图1的选中行并回填 =====
        /// <summary>执行 ApplyQueryAttributes 逻辑。</summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("meter", out var obj) && obj is EnergyMeterUiRow m)
            {
                Form = new ManualReadingForm
                {
                    MeterCode = m.MeterCode,
                    EnergyType = m.EnergyType, // 显示时可用映射
                    IndicatorName = "",
                    LastReading = 0,
                    Unit = m.EnergyType == "water" ? "t" : "kWh",
                    PointName = "",//默认点位
                    WorkshopName = m.WorkshopName,
                    LineName = m.LineName,
                    ReaderName = ""//默认当前登录人
                };
                OnPropertyChanged(nameof(ReadingTimeText));
                // ←—— 根据仪表编码拉点位
                _ = LoadMeterPointsAsync(m.MeterCode, _cts.Token);
            }
        }

        /// <summary>执行 OnSelectedMeterPointChanged 逻辑。</summary>
        partial void OnSelectedMeterPointChanged(MeterPointItem? value)
        {
            if (value == null) return;

            // 指标名称随点位带出
            Form.IndicatorName = string.IsNullOrWhiteSpace(value.indicatorName)
                ? (value.meterPointCode ?? "")
                : value.indicatorName!;

            // 同时回填点位名/单位（可选）
            Form.PointName = Form.IndicatorName;
            if (!string.IsNullOrWhiteSpace(value.unit))
                Form.Unit = value.unit!;

            _ = LoadLastReadingAsync(Form.MeterCode, value.meterPointCode ?? "");
        }

        /// <summary>执行 LoadMeterPointsAsync 逻辑。</summary>
        private async Task LoadMeterPointsAsync(string meterCode, CancellationToken ct)
        {
            var list = await _api.GetMeterPointsByMeterCodeAsync(meterCode, ct);

            if (MainThread.IsMainThread)
            {
                ApplyMeterPoints(list);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => ApplyMeterPoints(list));
            }
        }

        /// <summary>执行 ApplyMeterPoints 逻辑。</summary>
        private void ApplyMeterPoints(IReadOnlyList<MeterPointItem> list)
        {
            MeterPoints.Clear();
            foreach (var p in list) MeterPoints.Add(p);

            // 默认选主点位；没有主点位选第一条
            SelectedMeterPoint = MeterPoints.FirstOrDefault(x => x.mainPoint == true)
                                 ?? MeterPoints.FirstOrDefault();
        }



        /// <summary>执行 LoadLastReadingAsync 逻辑。</summary>
        private async Task LoadLastReadingAsync(string meterCode, string meterPointCode)
        {
            var data = await _api.GetLastReadingAsync(meterCode, meterPointCode, _cts.Token);
                Form.LastReading = data?.lastMeterReading; // 触发 Form 的 Recalc()
                Form.LastReadingTime = data.lastMeterReadingTime ?? "";
        }


        /// <summary>执行 Save 逻辑。</summary>
        [RelayCommand]
        private async Task Save()
        {
            if (SelectedMeterPoint is null)
            {
                await App.Current!.MainPage!.DisplayAlert("提示", "请先选择仪表点位", "OK");
                return;
            }
            if (Form.CurrentReading < Form.LastReading)
            {
                await App.Current!.MainPage!.DisplayAlert("提示", "本次抄表数不能小于上次抄表数", "OK");
                return;
            }

            var req = new MeterSaveReq
            {
                consumption = Form.Consumption,
                energyType = Form.EnergyType,
                lastMeterReading = Form.LastReading,
                lastMeterReadingTime = Form.LastReadingTime, // 上次抄表时间
                memo = Form.Remark,
                meterCode = Form.MeterCode,
                meterPointCode = SelectedMeterPoint.meterPointCode,
                meterReader = string.IsNullOrWhiteSpace(Form.ReaderName) ? "匿名" : Form.ReaderName,
                meterReading = Form.CurrentReading,
                meterReadingTime = Form.ReadingTime.ToString("yyyy-MM-dd HH:mm:ss")
            };

            try
            {
                IsBusy = true;
                var ok = await _api.SaveMeterReadingAsync(req, _cts.Token);
                if (ok)
                {
                    // 显示提示并等待用户关闭
                    await App.Current!.MainPage!.DisplayAlert("成功", "保存成功", "OK");

                    // 关闭当前页面（返回上一页）
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await App.Current!.MainPage!.DisplayAlert("失败", "保存失败，请稍后重试", "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }



    }
}
