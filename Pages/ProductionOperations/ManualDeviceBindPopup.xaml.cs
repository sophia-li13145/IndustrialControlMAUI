using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.Pages;

public partial class ManualDeviceBindPopup : Popup
{
    public ObservableCollection<StatusOption> DeviceOptions { get; } = new();

    public StatusOption? SelectedDeviceOption { get; set; }

    public string OperateTimeText => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public ManualDeviceBindPopup(IEnumerable<StatusOption> options, StatusOption? selected = null)
    {
        InitializeComponent();

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
            await Application.Current!.MainPage!.DisplayAlert("提示", "请选择设备", "确定");
            return;
        }

        Close(selected);
    }
}
