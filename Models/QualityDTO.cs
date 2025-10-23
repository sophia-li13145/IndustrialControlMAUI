using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Models;

    public class QualityRecordDto
    {
        public string? id { get; set; }
        public string? factoryCode { get; set; }
        public string? qualityNo { get; set; }
        public string? inspectStatus { get; set; }
        public string? inspectStatusName { get; set; }
        public string? materialName { get; set; }
        public string? orderNumber { get; set; }
        public string? processName { get; set; }
        public string? createdTime { get; set; }
    }

/// <summary>
/// 质检单列表项，用于前端展示（从 QualityRecordDto 转换而来）
/// </summary>
public class QualityOrderItem
{
    public string? Id { get; set; }
    /// <summary>
    /// 质检单号
    /// </summary>
    public string? QualityNo { get; set; }

    /// <summary>
    /// 检验状态编码（0 新建；1 待检验；2 检验中；3 检验完成）
    /// </summary>
    public string? InspectStatus { get; set; }

    /// <summary>
    /// 检验状态名称（映射显示用）
    /// </summary>
    public string? InspectStatusText { get; set; }

    /// <summary>
    /// 产品/物料名称
    /// </summary>
    public string? MaterialName { get; set; }

    /// <summary>
    /// 关联工单号
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// 工序名称
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// 创建时间（用于显示 yyyy-MM-dd）
    /// </summary>
    public DateTime? CreatedTime { get; set; }
}

public class DictQuality
{
    public List<DictItem> InspectStatus { get; set; } = new();
}


public class QualityDetailDto : ObservableObject
{
    public string? id { get; set; }
    public string? qualityNo { get; set; }
    public string? qualityType { get; set; }
    public string? qualityTypeName { get; set; }
    public string? processCode { get; set; }
    public string? processName { get; set; }
    public string? orderNumber { get; set; }

    public string? inspectStatus { get; set; }     // 0/1/2/3
    public string? inspectResult { get; set; }     // 合格/不合格
    public string? inspectRemark { get; set; }

    public string? inspectTime { get; set; }
    public string? createdTime { get; set; }
    public string? modifiedTime { get; set; }
    public string? factoryName { get; set; }
    public string? factoryCode { get; set; }
    public string? inspecter { get; set; }         // 检验人

    // === 参与计算的字段（带通知 + 触发重算） ===
    private decimal? _totalQualified;
    public decimal? totalQualified
    {
        get => _totalQualified;
        set { if (SetProperty(ref _totalQualified, value)) RecalcRates(); }
    }

    private decimal? _totalUnqualified;
    public decimal? totalUnqualified
    {
        get => _totalUnqualified;
        set { if (SetProperty(ref _totalUnqualified, value)) RecalcRates(); }
    }

    private decimal? _totalBad;
    public decimal? totalBad
    {
        get => _totalBad;
        set { if (SetProperty(ref _totalBad, value)) RecalcRates(); }
    }

    private decimal? _totalSampling;
    public decimal? totalSampling
    {
        get => _totalSampling;
        set { if (SetProperty(ref _totalSampling, value)) RecalcRates(); }
    }

    // === 计算结果（公开可设，便于反序列化；内部重算时会通知 UI） ===
    private decimal? _samplingDefectRate;
    public decimal? samplingDefectRate
    {
        get => _samplingDefectRate;
        set => SetProperty(ref _samplingDefectRate, value);
    }

    private decimal? _passRate;
    public decimal? passRate
    {
        get => _passRate;
        set => SetProperty(ref _passRate, value);
    }

    // 物料/明细/附件
    public QualityMaterial? orderQualityMaterial { get; set; }
    public List<QualityItem>? orderQualityDetailList { get; set; } = new();
    public List<QualityAttachment>? orderQualityAttachmentList { get; set; } = new();

    // —— 重入保护标志 —— 
    private bool _inRecalc;

    // 外部（如 VM 的 LoadAsync）可手动触发一次
    public void Recalc() => RecalcRates();

    // === 统一计算入口 ===
    private void RecalcRates()
    {
        if (_inRecalc) return;
        _inRecalc = true;
        try
        {
            var s = totalSampling ?? 0m;
            var b = totalBad ?? 0m;
            var q = totalQualified ?? 0m;
            var u = totalUnqualified ?? 0m;

            // 抽样不良率 = 不良总数 / 抽样总数 * 100%
            var newDefect = s > 0 ? decimal.Round(b / s * 100m, 2) : (decimal?)null;

            // 总体合格率 =  合格数/ 生产总数 * 100%
            var denom = orderQualityMaterial?.qty ?? 0m;
            var newPass = denom > 0 ? decimal.Round(q / denom * 100m, 2) : (decimal?)null;

            // 只在值变化时写回，减少多余通知
            if (newDefect != _samplingDefectRate) samplingDefectRate = newDefect;
            if (newPass != _passRate) passRate = newPass;
        }
        finally { _inRecalc = false; }
    }
}



public class QualityMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? spec { get; set; }
    public decimal? qty { get; set; }               // 生产/到货数量
    public string? unit { get; set; }
}

public partial class QualityItem : ObservableObject
{
    public int? index { get; set; }
    public string? id { get; set; }
    public string? inspectionName { get; set; }
    public string? inspectionAttributeName { get; set; }
    public string? inspectionMode { get; set; }
    public string? standardValue { get; set; }
    public string? upperLimit { get; set; }
    public string? lowerLimit { get; set; }
    public string? badCause { get; set; }
    public string? defect { get; set; }
    public string? inspectResult { get; set; }
    // 已选缺陷（用于标签显示与保存）
    public ObservableCollection<DefectChip> SelectedDefects { get; set; } = new();

    // 便捷：把缺陷名称拼成逗号串，保存时可回写给 defect 字段
    public string SelectedDefectNames => string.Join(",", SelectedDefects.Select(x => x.Name));

    // === 自动计算部分 ===
    private decimal? _sampleQty;
    public decimal? sampleQty
    {
        get => _sampleQty;
        set
        {
            if (SetProperty(ref _sampleQty, value))
                RecalcBadRate();
        }
    }

    private decimal? _badQty;
    public decimal? badQty
    {
        get => _badQty;
        set
        {
            if (SetProperty(ref _badQty, value))
                RecalcBadRate();
        }
    }

    private decimal? _badRate;
    public decimal? badRate
    {
        get => _badRate;
        set => SetProperty(ref _badRate, value);
    }

    // 计算不良率 = 不良数 / 抽样数 * 100%
    private void RecalcBadRate()
    {
        var s = sampleQty ?? 0m;
        var b = badQty ?? 0m;

        if (s > 0)
            badRate = decimal.Round(b / s * 100m, 2);
        else
            badRate = null;
    }
}

public class QualityAttachment
{
    public string? id { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentUrl { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentLocation { get; set; } // main/table
    public long? attachmentSize { get; set; }     // KB
    public string? attachmentFolder { get; set; }
    public string? memo { get; set; }
}

public partial class AttachmentItem
{
    public string? Id { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";       // 上传后可访问的地址
    public long SizeBytes { get; set; }         // 大小（字节）
}

public class OrderQualitySaveReq
{
    public decimal arrivalQty { get; set; }
    public string id { get; set; } = "";
    public string inspectRemark { get; set; } = "";
    public string inspectResult { get; set; } = "";
    public string inspectTime { get; set; } = "";          // "yyyy-MM-dd HH:mm:ss"
    public string inspecter { get; set; } = "";
    public decimal inspectionLossQty { get; set; }
    public string inspectionObject { get; set; } = "";
    public string inspectionSchemeName { get; set; } = "";
    public string inspectionSchemeTypeName { get; set; } = "";
    public string orderNumber { get; set; } = "";

    public List<OrderQualityAttachmentDto> orderQualityAttachmentList { get; set; } = new();
    public List<OrderQualityDetailDto> orderQualityDetailList { get; set; } = new();

    public decimal passRate { get; set; }
    public string processCode { get; set; } = "";
    public string processName { get; set; } = "";
    public string qualityType { get; set; } = "";
    public string qualityTypeName { get; set; } = "";
    public string retainedSampleNumber { get; set; } = "";
    public decimal retainedSampleQty { get; set; }
    public decimal samplingDefectRate { get; set; }
    public string supplierName { get; set; } = "";
    public decimal totalBad { get; set; }
    public decimal totalQualified { get; set; }
    public decimal totalSampling { get; set; }
    public decimal totalUnqualified { get; set; }
}

public class OrderQualityAttachmentDto
{
    public string attachmentExt { get; set; } = "";
    public string attachmentFolder { get; set; } = "";
    public string attachmentLocation { get; set; } = "";
    public string attachmentName { get; set; } = "";
    public string attachmentRealName { get; set; } = "";
    public long attachmentSize { get; set; }
    public string attachmentUrl { get; set; } = "";
    public string id { get; set; } = "";
    public string memo { get; set; } = "";
}

public class OrderQualityDetailDto
{
    public string badCause { get; set; } = "";
    public decimal badQty { get; set; }
    public decimal badRate { get; set; }
    public string defect { get; set; } = "";
    public string id { get; set; } = "";
    public string inspectResult { get; set; } = "";
    public string inspectionAttributeName { get; set; } = "";
    public string inspectionCode { get; set; } = "";
    public string inspectionMode { get; set; } = "";
    public string inspectionName { get; set; } = "";
    public string inspectionStandard { get; set; } = "";
    public string inspectionTypeName { get; set; } = "";
    public string lowerLimit { get; set; } = "";
    public decimal sampleQty { get; set; }
    public string standardValue { get; set; } = "";
    public string unit { get; set; } = "";
    public string upperLimit { get; set; } = "";
}
public partial class OrderQualityAttachmentItem : ObservableObject
{
    // 只保留这一个：使用 MVVM Toolkit 自动生成 Public LocalPath
    [ObservableProperty]
    [JsonIgnore]                 // 不序列化给后端
    private string? localPath;

    public bool IsImage { get; set; }   // 只要它为 true 才进缩略图

    // 统一用 PascalCase 命名，别和小写混用
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentRealName { get; set; }
    public string? AttachmentExt { get; set; }
    public string? AttachmentFolder { get; set; }
    public string? AttachmentLocation { get; set; }
    public long AttachmentSize { get; set; }
    public string? Id { get; set; }
    public string? CreatedTime { get; set; }
    public string? Memo { get; set; }
    public bool IsUploaded { get; set; }

    public ImageSource? DisplaySource =>
        IsImage
            ? (!string.IsNullOrWhiteSpace(LocalPath)
                ? ImageSource.FromFile(LocalPath!)
                : (!string.IsNullOrWhiteSpace(AttachmentUrl)
                    ? ImageSource.FromUri(new Uri(AttachmentUrl))
                    : null))
            : null;
}

public class UploadAttachmentResult
{
    public string? attachmentExt { get; set; }
    public string? attachmentFolder { get; set; }
    public string? attachmentLocation { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public long attachmentSize { get; set; }
    public string? attachmentUrl { get; set; }
}
public class DefectOption
{
    public string? Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Level { get; set; }   // 一级/二级/三级… 用于配色
    public string? Status { get; set; }  // 启用/停用（可不管）
}

public class DefectDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public string? Description { get; set; }
    public string? Standard { get; set; }
    public string? Creator { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}
public class DefectPage
{
    [JsonPropertyName("pageNo")] public int PageNo { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; }
    [JsonPropertyName("total")] public long Total { get; set; }
    [JsonPropertyName("records")] public List<DefectRecord> Records { get; set; } = new();
}

public class DefectRecord
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("defectName")] public string? DefectName { get; set; }
    [JsonPropertyName("defectCode")] public string? DefectCode { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }          // 0/1 或 “启用”
    [JsonPropertyName("levelCode")] public string? LevelCode { get; set; }       // 1/2/3
    [JsonPropertyName("levelName")] public string? LevelName { get; set; }       // 一级/二级/三级
    [JsonPropertyName("defectDescription")] public string? DefectDescription { get; set; }
    [JsonPropertyName("evaluationStandard")] public string? EvaluationStandard { get; set; }
    [JsonPropertyName("creator")] public string? Creator { get; set; }
    [JsonPropertyName("createdTime")] public string? CreatedTime { get; set; }     // yyyy-MM-dd HH:mm:ss
    [JsonPropertyName("modifiedTime")] public string? ModifiedTime { get; set; }
    [JsonPropertyName("memo")] public string? Memo { get; set; }
}

/// <summary>
/// 显示在“缺陷：”后的彩色标签
/// </summary>
public class DefectChip
{
    /// <summary>
    /// 缺陷名称（展示用）
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 背景颜色
    /// </summary>
    public Color ColorHex { get; set; } = Colors.LightGray;
}