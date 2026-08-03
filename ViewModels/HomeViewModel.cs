using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models.Permissions;
using IndustrialControlMAUI.Services.Permissions;

namespace IndustrialControlMAUI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly PdaPermissionState _permissionState;
    [ObservableProperty] private bool canDutyRoster;
    [ObservableProperty] private bool canShiftHandover;
    [ObservableProperty] private bool canMaterialInbound;
    [ObservableProperty] private bool showScheduleSection;
    [ObservableProperty] private bool showWarehouseSection;

    public HomeViewModel(PdaPermissionState permissionState) { _permissionState = permissionState; ApplyPermissions(); }

    public void ApplyPermissions()
    {
        CanDutyRoster = _permissionState.Has(PdaMenuCodes.DutyRoster);
        CanShiftHandover = _permissionState.Has(PdaMenuCodes.ShiftHandover);
        CanMaterialInbound = _permissionState.Has(PdaMenuCodes.MaterialInbound);
        ShowScheduleSection = CanDutyRoster || CanShiftHandover;
        ShowWarehouseSection = CanMaterialInbound;
    }
}
