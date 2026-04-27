using System.Collections.Generic;

namespace IndustrialControlMAUI.Models;

public class DeviceMoldRelationDto
{
    public string? id { get; set; }
    public string? deviceCode { get; set; }
    public string? deviceName { get; set; }
    public string? deviceModel { get; set; }
    public string? moldCode { get; set; }
    public string? moldModel { get; set; }
    public string? moldInstallTime { get; set; }
    public string? moldRemoveTime { get; set; }
    public decimal? serviceLife { get; set; }
    public int? usageCount { get; set; }
    public string? serviceLifeUsageCountStr { get; set; }
    public bool? status { get; set; }
}

public class AddDeviceMoldRelationReq
{
    public string? deviceCode { get; set; }
    public string? deviceModel { get; set; }
    public string? deviceName { get; set; }
    public string? memo { get; set; }
    public string? moldCode { get; set; }
    public string? moldModel { get; set; }
}

public class DeviceMoldInfoResultDto
{
    public List<DeviceMoldRelationDto>? pmsDeviceMoldRelationDTOList { get; set; }
    public DeviceInfoDto? pmsEqptPointDTO { get; set; }
}

public class DeviceInfoDto
{
    public string? deviceCode { get; set; }
    public string? deviceName { get; set; }
    public string? deviceModel { get; set; }
}
