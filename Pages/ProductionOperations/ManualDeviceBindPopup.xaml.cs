using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public class DeviceBindConfirmResult
{
    public StatusOption? SelectedDeviceOption { get; init; }
    public DateTime OperationTime { get; init; }
}

public partial class ManualDeviceBindPopup : Popup
{
    public ObservableCollection<StatusOption> DeviceOptions { get; } = new();

    public StatusOption? SelectedDeviceOption { get; set; }

    public string Title { get; }

    public bool AllowDeviceSelection { get; }

    public bool IsReadonlyDeviceVisible => !AllowDeviceSelection;

    public string SelectedDeviceName => SelectedDeviceOption?.Text ?? "";

    public DateTime OperationTime { get; }

    public string OperateTimeText => OperationTime.ToString("yyyy-MM-dd HH:mm:ss");

    public ManualDeviceBindPopup(
        IEnumerable<StatusOption> options,
        StatusOption? selected = null,
        bool allowDeviceSelection = true,
        string title = "确认绑定",
        DateTime? operationTime = null)
    {
        InitializeComponent();

        Title = title;
        AllowDeviceSelection = allowDeviceSelection;
        OperationTime = operationTime ?? DateTime.Now;

        foreach (var item in options)
        {
            if (!string.IsNullOrWhiteSpace(item.Value))
                DeviceOptions.Add(item);
        }

        SelectedDeviceOption = selected is not null && !string.IsNullOrWhiteSpace(selected.Value)
            ? DeviceOptions.FirstOrDefault(x => x.Value == selected.Value) ?? DeviceOptions.FirstOrDefault()
            : DeviceOptions.FirstOrDefault();

        BindingContext = this;
    }

    private void OnCancelClicked(object sender, EventArgs e) => Close(null);

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var selected = SelectedDeviceOption;
        if (selected is null || string.IsNullOrWhiteSpace(selected.Value))
        {
            await Application.Current!.MainPage!.DisplayAlert("提示", AllowDeviceSelection ? "请选择设备" : "未找到可绑定的设备", "确定");
            return;
        }

        Close(new DeviceBindConfirmResult
        {
            SelectedDeviceOption = selected,
            OperationTime = OperationTime
        });
    }
}
