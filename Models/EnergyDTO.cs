using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Models
{
    public class MeterRecordDto
    {
        public string? meterCode { get; set; }
        public string? energyType { get; set; }     // electric / water / gas / compressed_air
        public string? workshopName { get; set; }
        public string? workshopId { get; set; }
        public string? lineName { get; set; }
        public string? lineId { get; set; }
        public string? devCode { get; set; }
        public string? devName { get; set; }
    }

    // —— 字典（能源类型） ——
    public class EnergyDictResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<EnergyDictField>? result { get; set; }
    }
    public class EnergyDictField
    {
        public string? field { get; set; }
        public List<EnergyDictItem>? dictItems { get; set; }
    }
    public class EnergyDictItem
    {
        public string? dictItemValue { get; set; }   // electric / water / gas / compressed_air
        public string? dictItemName { get; set; }    // 电能 / 水能 / 天然气 / 压缩空气
    }

    // —— 车间列表 ——
    public class WorkShopResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<WorkShopItem> result { get; set; } = new();
    }
    public class WorkShopItem
    {
        public string? workShopId { get; set; }
        public string? workShopName { get; set; }
        public string? workShopsType { get; set; }
        public string? workShopsTypeName { get; set; }
        public string? parentCode { get; set; }
    }

    // —— 用户列表 ——
    public class UserListResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<UserItem> result { get; set; } = new();
    }
    public class UserItem
    {
        public string? id { get; set; }
        public string? username { get; set; }
        public string? realname { get; set; }
        public string? mobile { get; set; }
        public bool? delstatus { get; set; }
        public string? email { get; set; }
    }

    // —— 通用下拉项（若你项目已有同名请用已有的） ——
    public class IdNameOption
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    /// <summary>图2表单模型</summary>
    public partial class ManualReadingForm : ObservableObject
    {
        [ObservableProperty] private string meterCode = "";
        [ObservableProperty] private string energyType = "";
        [ObservableProperty] private string indicatorName = "";
        [ObservableProperty] private string lastReading = "0";     // 上次
        [ObservableProperty] private string currentReading = "";   // 本次（用户输入）
        [ObservableProperty] private string consumption = "";      // 消耗量（只读）
        [ObservableProperty] private string unit = "kWh";
        [ObservableProperty] private string pointName = "";
        [ObservableProperty] private DateTime readingTime = DateTime.Now;
        [ObservableProperty] private string readerName = "";
        [ObservableProperty] private string remark = "";
        [ObservableProperty] private string workshopName = "";
        [ObservableProperty] private string lineName = "";

        partial void OnCurrentReadingChanged(string value) => Recalc();
        partial void OnLastReadingChanged(string value) => Recalc();

        private void Recalc()
        {
            if (decimal.TryParse(CurrentReading, NumberStyles.Any, CultureInfo.InvariantCulture, out var cur) &&
                decimal.TryParse(LastReading, NumberStyles.Any, CultureInfo.InvariantCulture, out var last))
            {
                Consumption = (cur - last).ToString("G29", CultureInfo.InvariantCulture);
            }
            else
            {
                Consumption = "";
            }
        }
    }



    /// <summary>选择列表 UI 行（由接口返回的 MeterRecordDto 映射而来）</summary>
    public partial class EnergyMeterUiRow : ObservableObject
    {
        public string MeterCode { get; set; } = "";
        public string EnergyType { get; set; } = "";   // electric/water/gas/compressed_air
        public string WorkshopName { get; set; } = "";
        public string WorkshopId { get; set; } = "";
        public string LineName { get; set; } = "";
        public string LineId { get; set; } = "";
        public string DevName { get; set; } = "";

        public string EnergyTypeText => EnergyType switch
        {
            "electric" => "电能",
            "water" => "水能",
            "gas" => "天然气",
            "compressed_air" => "压缩空气",
            _ => EnergyType
        };

        // 关键属性：单选绑定它
        [ObservableProperty]
        private bool isSelected;
    }

    /// <summary>能源类型字典项（显示用）</summary>
    public class OptionItem
    {
        public string? Value { get; set; }  // electric/water/gas/...
        public string Text { get; set; } = "";
        public override string ToString() => Text;
    }

    // 和文件末尾其它轻量 DTO 放一起
    public class ProductLineResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<ProductLineItem> result { get; set; } = new();
        public long costTime { get; set; }
    }

    public class ProductLineItem
    {
        public string? productLineId { get; set; }
        public string? productLineName { get; set; }
        public string? workshopsType { get; set; }      // "production_line"
        public string? workshopsTypeName { get; set; }  // "产线"
        public string? parentCode { get; set; }         // 若后端返回上级，可用来做联动
    }

    public class MeterPointResp
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public int code { get; set; }
        public List<MeterPointItem> result { get; set; } = new();
        public long costTime { get; set; }
    }

    public class MeterPointItem
    {
        public string? meterCode { get; set; }
        public string? meterPointCode { get; set; }   // 点位编码
        public string? indicatorType { get; set; }    // phase_voltage / instantaneous_power ...
        public string? indicatorName { get; set; }    // 显示名（如：22相电压）
        public string? unit { get; set; }             // 单位（可能为 null）
        public bool? mainPoint { get; set; }          // 是否主点位
        public string? magnification { get; set; }
        public string? formule { get; set; }
        public string? memo { get; set; }

        // 供 Picker 直接显示
        public override string ToString() => string.IsNullOrWhiteSpace(indicatorName)
            ? (meterPointCode ?? "")
            : indicatorName!;
    }

    public class LastReadingResult
    {
        public string? meterCode { get; set; }
        public string? meterPointCode { get; set; }
        public decimal? lastMeterReading { get; set; }
        public string? lastMeterReadingTime { get; set; }
    }
}

