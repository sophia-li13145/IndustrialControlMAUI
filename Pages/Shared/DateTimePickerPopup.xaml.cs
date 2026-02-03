using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static Android.Icu.Text.CaseMap;

namespace IndustrialControlMAUI.Popups;

public class DateTimePopupResult
{
    public bool IsCanceled { get; init; }
    public bool IsCleared { get; init; }
    public DateTime? Value { get; init; }
}

public partial class DateTimePickerPopup : Popup
{
    /// <summary>执行 DateTimePickerPopup 初始化逻辑。</summary>
    public DateTimePickerPopup(string title, DateTime? initial)
    {
        InitializeComponent();
        BindingContext = new Vm(title, initial, CloseWithResult);
    }

    /// <summary>执行 CloseWithResult 逻辑。</summary>
    private void CloseWithResult(DateTimePopupResult result) => Close(result);

    public partial class Vm : ObservableObject
    {
        private readonly Action<DateTimePopupResult> _close;

        [ObservableProperty] private string title = "选择时间";
        [ObservableProperty] private DateTime pickDate;
        [ObservableProperty] private TimeSpan pickTime;

        /// <summary>执行 Vm 初始化逻辑。</summary>
        public Vm(string title, DateTime? initial, Action<DateTimePopupResult> close)
        {
            _close = close;
            Title = title;

            var dt = initial ?? DateTime.Now;
            PickDate = dt.Date;
            PickTime = dt.TimeOfDay;
        }

        /// <summary>执行 Ok 逻辑。</summary>
        [RelayCommand]
        private void Ok()
        {
            var dt = PickDate.Date + PickTime;
            _close(new DateTimePopupResult { Value = dt });
        }

        /// <summary>执行 Cancel 逻辑。</summary>
        [RelayCommand]
        private void Cancel()
        {
            _close(new DateTimePopupResult { IsCanceled = true });
        }

        /// <summary>执行 Clear 逻辑。</summary>
        [RelayCommand]
        private void Clear()
        {
            _close(new DateTimePopupResult { IsCleared = true, Value = null });
        }
    }
}
