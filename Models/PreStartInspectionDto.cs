using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Models;

public class PmsPreStartInspectionQueryResourceParam
{
    public string? factoryCode { get; set; }
    public string? workOrderNo { get; set; }
    public string? processCode { get; set; }
    public string? resourceType { get; set; }
    public string? resourceCode { get; set; }
}

public class PmsPreStartInspectionQueryMaterialParam
{
    public string? factoryCode { get; set; }
    public string? workOrderNo { get; set; }
    public string? processCode { get; set; }
    public string? materialCode { get; set; }
}

public class PreStartInspectionScanResourceDto : INotifyPropertyChanged
{
    private bool _isConfirmed = true;
    private string? _maintenanceStatus;
    private string _maintenanceStatusText = "未保养";

    public bool isConfirmed
    {
        get => _isConfirmed;
        set
        {
            if (_isConfirmed == value) return;
            _isConfirmed = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public decimal? demandQty { get; set; }

    public string? maintenanceStatus
    {
        get => _maintenanceStatus;
        set
        {
            if (_maintenanceStatus == value) return;
            _maintenanceStatus = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string MaintenanceStatusText
    {
        get => _maintenanceStatusText;
        set
        {
            if (_maintenanceStatusText == value) return;
            _maintenanceStatusText = value;
            OnPropertyChanged();
        }
    }

    public string? model { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public string? resourceCode { get; set; }
    public string? resourceDemandId { get; set; }
    public string? resourceName { get; set; }
    public string? resourceType { get; set; }
}

public class PreStartInspectionScanMaterialDto : INotifyPropertyChanged
{
    private bool _isConfirmed = true;

    public bool isConfirmed
    {
        get => _isConfirmed;
        set
        {
            if (_isConfirmed == value) return;
            _isConfirmed = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [JsonIgnore]
    public string DemandQtyText => demandQty.HasValue ? $"{demandQty:g}{unit}" : string.Empty;

    public decimal? demandQty { get; set; }
    public string? matReqNo { get; set; }
    public string? materialClassName { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? platPlanNo { get; set; }
    public string? schemeNo { get; set; }
    public string? spec { get; set; }
    public string? unit { get; set; }
    public string? workOrderNo { get; set; }
}

public class PmsPreStartInspectionConfirmScansParam
{
    public List<PreStartInspectionConfirmMaterialItem> materialList { get; set; } = new();
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public string? schemeNo { get; set; }
    public List<PreStartInspectionConfirmToolingItem> toolingList { get; set; } = new();
    public string? workOrderNo { get; set; }
}

public class PreStartInspectionConfirmMaterialItem
{
    public bool isConfirmed { get; set; }
    public string? matReqNo { get; set; }
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? memo { get; set; }
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public decimal? scanQty { get; set; }
    public string? schemeNo { get; set; }
    public string? unit { get; set; }
    public string? workOrderNo { get; set; }
}

public class PreStartInspectionConfirmToolingItem
{
    public bool isConfirmed { get; set; }
    public string? maintenanceStatus { get; set; }
    public string? memo { get; set; }
    public string? model { get; set; }
    public string? platPlanNo { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public string? resourceDemandId { get; set; }
    public string? resourceType { get; set; }
    public string? schemeNo { get; set; }
    public string? toolingCode { get; set; }
    public string? toolingName { get; set; }
    public string? workOrderNo { get; set; }
}
