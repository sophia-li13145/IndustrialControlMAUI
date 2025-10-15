using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Models;

    public class QualityRecordDto
    {
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

// Models/QualityDetailDto.cs
public class QualityDetailDto
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

    public decimal passRate { get; set; }
    public decimal totalQualified { get; set; }
    public decimal totalUnqualified { get; set; }
    public decimal totalBad { get; set; }
    public decimal totalSampling { get; set; }
    public decimal samplingDefectRate { get; set; }

    // 物料
    public QualityMaterial? orderQualityMaterial { get; set; }

    // 明细行
    public List<QualityItem> orderQualityDetailList { get; set; } = new();

    // 附件
    public List<QualityAttachment> orderQualityAttachmentList { get; set; } = new();
}

public class QualityMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? spec { get; set; }
    public decimal qty { get; set; }               // 生产/到货数量
    public string? unit { get; set; }
}

public class QualityItem
{
    public int? Index { get; set; }
    public string? id { get; set; }
    public string? inspectionCode { get; set; }
    public string? inspectionName { get; set; }
    public string? inspectionAttributeName { get; set; }
    public string? inspectionTypeName { get; set; }
    public string? inspectionMode { get; set; }
    public string? inspectionStandard { get; set; }

    public string? standardValue { get; set; }
    public string? lowerLimit { get; set; }
    public string? upperLimit { get; set; }
    public string? unit { get; set; }

    public string? inspectResult { get; set; }   // 合格/不合格
    public string? defect { get; set; }
    public string? badCause { get; set; }
    public decimal badQty { get; set; }
    public decimal sampleQty { get; set; }
}

public class QualityAttachment
{
    public string? id { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentUrl { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentLocation { get; set; } // main/table
    public decimal attachmentSize { get; set; }     // KB
}
