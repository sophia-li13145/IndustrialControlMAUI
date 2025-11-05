using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
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

        public ObservableCollection<MeterPointItem> MeterPoints { get; } = new();

        [ObservableProperty] private MeterPointItem? selectedMeterPoint;

        public ManualReadingViewModel(IEnergyApi api)
        {
            _api = api;
        }

        [ObservableProperty] private ManualReadingForm form = new();

        public string ReadingTimeText => Form.ReadingTime.ToString("yyyy-MM-dd HH:mm:ss");

        // ===== 抄表人模糊搜索 =====
        [ObservableProperty] private string? readerQuery;
        public ObservableCollection<IdNameOption> ReaderSuggestions { get; } = new();
        [ObservableProperty] private bool isReaderDropdownOpen;

        partial void OnReaderQueryChanged(string? value)
        {
            _searchTimer?.Stop();
            _searchTimer ??= new System.Timers.Timer(300) { AutoReset = false };
            _searchTimer.Elapsed -= OnSearchTimerElapsed;
            _searchTimer.Elapsed += OnSearchTimerElapsed;
            _searchTimer.Start();
        }

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

        [RelayCommand]
        private void PickReader(IdNameOption option)
        {
            Form.ReaderName = option.Name;
            ReaderQuery = option.Name;
            IsReaderDropdownOpen = false;
            OnPropertyChanged(nameof(Form));
        }

        [RelayCommand]
        private void CloseReaderDropdown() => IsReaderDropdownOpen = false;

        public async Task EnsureUsersAsync()
        {
            if (_allUsers.Count == 0)
                _allUsers = (await _api.GetUsersAsync(_cts.Token)).ToList();
        }

        // ===== 接收图1的选中行并回填 =====
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("meter", out var obj) && obj is EnergyMeterUiRow m)
            {
                Form = new ManualReadingForm
                {
                    MeterCode = m.MeterCode,
                    EnergyType = m.EnergyType, // 显示时可用映射
                    IndicatorName = "用量",
                    LastReading = "0",
                    Unit = m.EnergyType == "water" ? "t" : "kWh",
                    PointName = "默认点位",
                    WorkshopName = m.WorkshopName,
                    LineName = m.LineName,
                    ReaderName = "默认当前登录人"
                };
                OnPropertyChanged(nameof(ReadingTimeText));
                // ←—— 根据仪表编码拉点位
                _ = LoadMeterPointsAsync(m.MeterCode, _cts.Token);
            }
        }

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

        private async Task LoadMeterPointsAsync(string meterCode, CancellationToken ct)
        {
            var list = await _api.GetMeterPointsByMeterCodeAsync(meterCode, ct);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MeterPoints.Clear();
                foreach (var p in list) MeterPoints.Add(p);

                // 默认选主点位；没有主点位时选第一条
                SelectedMeterPoint = MeterPoints.FirstOrDefault(x => x.mainPoint == true)
                                     ?? MeterPoints.FirstOrDefault();
            });
        }



        private async Task LoadLastReadingAsync(string meterCode, string meterPointCode)
        {
            var data = await _api.GetLastReadingAsync(meterCode, meterPointCode, _cts.Token);
            if (data?.lastMeterReading is decimal d)
                Form.LastReading = d.ToString("G29"); // 触发 Form 的 Recalc()
                                                      // 若需要显示时间，可另加一个只读字段绑定 data.lastMeterReadingTime
        }


        [RelayCommand]
        private Task Save()
        {
            if (string.IsNullOrWhiteSpace(Form.CurrentReading))
            {
                Application.Current?.MainPage?.DisplayAlert("提示", "请填写本次抄表数", "OK");
                return Task.CompletedTask;
            }

            if (decimal.TryParse(Form.CurrentReading, out var cur) &&
                decimal.TryParse(Form.LastReading, out var last))
            {
                Form.Consumption = (cur - last).ToString("G29");
                OnPropertyChanged(nameof(Form));
            }

            Application.Current?.MainPage?.DisplayAlert("成功", "已保存（假接口）", "OK");
            return Task.CompletedTask;
        }
    }
}
