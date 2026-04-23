using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.Pages;

public partial class MoldUnloadConfirmPopup : Popup
{
    private readonly DeviceMoldRelationDto _detail;

    public string MoldCodeText => $"模具编码: {_detail.moldCode ?? string.Empty}";
    public string MoldModelText => $"模具型号: {_detail.moldModel ?? string.Empty}";
    public string UsageStatusText => $"使用状态: {GetUsageStatus()}";
    public string BoundDeviceText => $"当前绑定设备: {_detail.deviceName ?? _detail.deviceCode ?? string.Empty}";
    public string InstallTimeText => $"装模时间: {_detail.moldInstallTime ?? string.Empty}";
    public string ServiceLifeText => (_detail.serviceLife ?? 0).ToString("0.####");
    public string UsageCountText => (_detail.usageCount ?? 0).ToString();

    public MoldUnloadConfirmPopup(DeviceMoldRelationDto detail)
    {
        InitializeComponent();
        _detail = detail;
        BindingContext = this;
    }

    private string GetUsageStatus()
    {
        if (!string.IsNullOrWhiteSpace(_detail.deviceCode) || !string.IsNullOrWhiteSpace(_detail.deviceName))
            return "使用中";

        return "未使用";
    }

    private void OnConfirmClicked(object sender, EventArgs e) => Close(true);
}
