using CommunityToolkit.Maui.Views;

namespace IndustrialControlMAUI.Pages;

public class DeviceBindTimeEditResult
{
    public bool Confirmed { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
}

public partial class DeviceBindTimeEditPopup : Popup
{
    public DateTime StartDate { get; set; }
    public TimeSpan StartTimeOfDay { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan EndTimeOfDay { get; set; }

    public DeviceBindTimeEditPopup(DateTime? startTime, DateTime? endTime)
    {
        InitializeComponent();

        var start = startTime ?? DateTime.Now;
        var end = endTime ?? start;

        StartDate = start.Date;
        StartTimeOfDay = start.TimeOfDay;
        EndDate = end.Date;
        EndTimeOfDay = end.TimeOfDay;

        BindingContext = this;
    }

    private void OnCancelClicked(object sender, EventArgs e) =>
        Close(new DeviceBindTimeEditResult { Confirmed = false });

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var start = StartDate.Date + StartTimeOfDay;
        var end = EndDate.Date + EndTimeOfDay;

        if (start > end)
        {
            await Application.Current!.MainPage!.DisplayAlert("提示", "开始时间不能晚于结束时间", "确定");
            return;
        }

        Close(new DeviceBindTimeEditResult
        {
            Confirmed = true,
            StartTime = start,
            EndTime = end
        });
    }
}
