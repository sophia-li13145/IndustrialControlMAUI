using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class DutyRosterViewModel : ObservableObject
{
    private readonly IScheduleApi _scheduleApi;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string todayText = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private bool hasShifts;
    [ObservableProperty] private bool hasNoShifts;

    public ObservableCollection<DutyDateItem> Dates { get; } = new();
    public ObservableCollection<ScheduleShiftItem> Shifts { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand<DutyDateItem> SelectDateCommand { get; }

    public DutyRosterViewModel(IScheduleApi scheduleApi)
    {
        _scheduleApi = scheduleApi;
        LoadCommand = new AsyncRelayCommand(() => LoadAsync(SelectedDate));
        SelectDateCommand = new AsyncRelayCommand<DutyDateItem>(SelectDateAsync);
    }

    public async Task LoadAsync(DateTime date)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            SelectedDate = date.Date;

            var resp = await _scheduleApi.QuerySchedulePlanDetailAsync(SelectedDate);
            if (resp.success != true || resp.result is null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(resp.message) ? "排班数据加载失败" : resp.message;
                Shifts.Clear();
                HasShifts = false;
                HasNoShifts = true;
                EnsureFallbackDates();
                return;
            }

            ApplyResult(resp.result);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"排班数据加载异常：{ex.Message}";
            Shifts.Clear();
            HasShifts = false;
            HasNoShifts = true;
            EnsureFallbackDates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectDateAsync(DutyDateItem? item)
    {
        if (item is null) return;
        await LoadAsync(item.DateValue);
    }

    private void ApplyResult(SchedulePlanDetailResult result)
    {
        var selected = ParseDate(result.SelectedDate) ?? SelectedDate;
        SelectedDate = selected.Date;
        TodayText = SelectedDate.ToString("yyyy-MM-dd");

        Dates.Clear();
        var sourceDates = result.DateList.Count > 0
            ? result.DateList
            : Enumerable.Range(-3, 7).Select(offset => new ScheduleDateItem
            {
                Date = SelectedDate.AddDays(offset).ToString("yyyy-MM-dd"),
                Day = SelectedDate.AddDays(offset).Day,
                WeekName = GetWeekName(SelectedDate.AddDays(offset)),
                IsToday = SelectedDate.AddDays(offset).Date == DateTime.Today,
                IsSelected = offset == 0
            });

        foreach (var item in sourceDates)
        {
            var value = ParseDate(item.Date) ?? SelectedDate;
            Dates.Add(new DutyDateItem
            {
                DateValue = value.Date,
                Day = item.Day > 0 ? item.Day : value.Day,
                WeekName = string.IsNullOrWhiteSpace(item.WeekName) ? GetWeekName(value) : item.WeekName!,
                IsToday = item.IsToday || value.Date == DateTime.Today,
                IsSelected = item.IsSelected || value.Date == SelectedDate.Date
            });
        }

        Shifts.Clear();
        foreach (var shift in result.ShiftList)
            Shifts.Add(shift);

        HasShifts = Shifts.Count > 0;
        HasNoShifts = !HasShifts;
    }

    private void EnsureFallbackDates()
    {
        if (Dates.Count > 0) return;
        foreach (var offset in Enumerable.Range(-3, 7))
        {
            var date = SelectedDate.AddDays(offset);
            Dates.Add(new DutyDateItem
            {
                DateValue = date,
                Day = date.Day,
                WeekName = GetWeekName(date),
                IsToday = date.Date == DateTime.Today,
                IsSelected = date.Date == SelectedDate.Date
            });
        }
    }

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var date) ? date : null;


    private static string GetWeekName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        DayOfWeek.Saturday => "周六",
        _ => "周日"
    };
}

public partial class DutyDateItem : ObservableObject
{
    public DateTime DateValue { get; set; }
    public int Day { get; set; }
    public string WeekName { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    [ObservableProperty] private bool isSelected;
}
