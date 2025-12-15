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
    public DateTimePickerPopup(string title, DateTime? initial)
    {
        InitializeComponent();
        BindingContext = new Vm(title, initial, CloseWithResult);
    }

    private void CloseWithResult(DateTimePopupResult result) => Close(result);

    public partial class Vm : ObservableObject
    {
        private readonly Action<DateTimePopupResult> _close;

        [ObservableProperty] private string title = "—°‘Ò ±º‰";
        [ObservableProperty] private DateTime pickDate;
        [ObservableProperty] private TimeSpan pickTime;

        public Vm(string title, DateTime? initial, Action<DateTimePopupResult> close)
        {
            _close = close;
            Title = title;

            var dt = initial ?? DateTime.Now;
            PickDate = dt.Date;
            PickTime = dt.TimeOfDay;
        }

        [RelayCommand]
        private void Ok()
        {
            var dt = PickDate.Date + PickTime;
            _close(new DateTimePopupResult { Value = dt });
        }

        [RelayCommand]
        private void Cancel()
        {
            _close(new DateTimePopupResult { IsCanceled = true });
        }

        [RelayCommand]
        private void Clear()
        {
            _close(new DateTimePopupResult { IsCleared = true, Value = null });
        }
    }
}
