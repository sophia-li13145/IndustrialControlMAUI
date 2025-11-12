using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace IndustrialControlMAUI.Models;

    public class InspectionRecordDto
    {
    /// <summary>主键ID</summary>
    public string? id { get; set; }

    /// <summary>设备名称</summary>
    public string? devName { get; set; }

    public string? devCode { get; set; }

    /// <summary>工厂编码</summary>
    public string? factoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? factoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? inspectNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? inspectStatus { get; set; }
    public string? inspectStatusText { get; set; }

    public string? inspectResult { get; set; }

    public string? inspecter { get; set; }

    /// <summary>检验数量</summary>
    public decimal? inspectNum { get; set; }

    /// <summary>合格总数</summary>
    public decimal? qualifiedNum { get; set; }

    /// <summary>不合格总数</summary>
    public decimal? unqualifiedNum { get; set; }

    /// <summary>计划编码</summary>
    public string? planCode { get; set; }

    /// <summary>计划名称</summary>
    public string? planName { get; set; }

    /// <summary>计划检验日期</summary>
    public string? planInspectTime { get; set; }

    /// <summary>实际检验时间</summary>
    public string? inspectTime { get; set; }

    /// <summary>备注</summary>
    public string? memo { get; set; }

    /// <summary>创建时间</summary>
    public string? createdTime { get; set; }

    /// <summary>修改时间</summary>
    public string? modifiedTime { get; set; }
}

/// <summary>
/// 设备
/// </summary>
public class InspectionOrderItem
{
    /// <summary>主键ID</summary>
    public string? Id { get; set; }

    /// <summary>设备名称</summary>
    public string? DevName { get; set; }

    /// <summary>设备编码</summary>
    public string? DevCode { get; set; }

    /// <summary>工厂编码</summary>
    public string? FactoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? FactoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? InspectNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? InspectStatus { get; set; }

    /// <summary>检验状态名称（映射显示用）</summary>
    public string? InspectStatusText { get; set; }

    public string? InspectResult { get; set; }

    /// <summary>检验员</summary>
    public string? Inspecter { get; set; }

    /// <summary>检验数量</summary>
    public decimal? InspectNum { get; set; }

    /// <summary>合格总数</summary>
    public decimal? QualifiedNum { get; set; }

    /// <summary>不合格总数</summary>
    public decimal? UnqualifiedNum { get; set; }

    /// <summary>计划编码</summary>
    public string? PlanCode { get; set; }

    /// <summary>计划名称</summary>
    public string? PlanName { get; set; }

    /// <summary>计划检验日期</summary>
    public DateTime? PlanInspectTime { get; set; }

    /// <summary>实际检验时间</summary>
    public DateTime? InspectTime { get; set; }

    /// <summary>备注</summary>
    public string? Memo { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreatedTime { get; set; }

    /// <summary>修改时间</summary>
    public DateTime? ModifiedTime { get; set; }
}

public class DictInspection
{
    public List<DictItem> InspectStatus { get; set; } = new();
}


public class InspectionDetailDto : ObservableObject
{
    public string? id { get; set; }
    /// <summary>设备编码</summary>
    public string? devCode { get; set; }

    public string? devName { get; set; }

    /// <summary>工厂编码</summary>
    public string? factoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? factoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? inspectNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? inspectStatus { get; set; }

    /// <summary>检验状态名称（映射显示用）</summary>
    public string? inspectStatusText { get; set; }

    public string? inspectResult { get; set; }

    /// <summary>检验员</summary>
    public string? inspecter { get; set; }

    /// <summary>检验数量</summary>
    public decimal? inspectNum { get; set; }

    public string? inspectMemo { get; set; }

    /// <summary>合格总数</summary>
    public decimal? qualifiedNum { get; set; }

    /// <summary>不合格总数</summary>
    public decimal? unqualifiedNum { get; set; }

    /// <summary>计划编码</summary>
    public string? planCode { get; set; }

    /// <summary>计划名称</summary>
    public string? planName { get; set; }

    /// <summary>计划检验日期</summary>
    public string? planInspectTime { get; set; }

    /// <summary>实际检验时间</summary>
    public string? inspectTime { get; set; }

    /// <summary>备注</summary>
    public string? memo { get; set; }

    /// <summary>创建时间</summary>
    public string? createdTime { get; set; }

    /// <summary>修改时间</summary>
    public string? modifiedTime { get; set; }


    public List<InspectionItem>? devInspectTaskDetailList { get; set; } = new();
    public List<InspectionAttachment>? devInspectTaskAttachmentList { get; set; } = new();


}



public class InspectionMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? spec { get; set; }
    public decimal? qty { get; set; }               // 生产/到货数量
    public string? unit { get; set; }
}

public partial class InspectionItem : ObservableObject
{
    public int? index { get; set; }
    public string? id { get; set; }
    public string? itemName { get; set; }
    public string? itemCode { get; set; }
    public string? inspectionStandard { get; set; }
    public string? inspectionMode { get; set; }
    public string? inspectionAttributeName { get; set; }
    public string? inspectionAttribute { get; set; }
    public string? inspectValue { get; set; }
    public string? inspectResult { get; set; }
    public string? inspectNo { get; set; }

    public string? memo { get; set; }
    public string? upperLimit { get; set; }
    public string? lowerLimit { get; set; }
    public string? standardValue { get; set; }
    public string? unit { get; set; }
}

public class InspectionAttachment
{
    public string? id { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentUrl { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentLocation { get; set; } // main/table
    public decimal? attachmentSize { get; set; }     // KB
    public string? attachmentFolder { get; set; }
    public string? createdTime { get; set; }
}

public partial class OrderInspectionAttachmentItem : ObservableObject
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
    // 预览接口返回的直连 URL（短期有效）
    private string? _previewUrl;
    public string? PreviewUrl { get => _previewUrl; set => SetProperty(ref _previewUrl, value); }

    // 供 XAML 绑定：优先显示 Preview → Local → 原地址
    public string? DisplaySource => PreviewUrl ?? LocalPath ?? AttachmentUrl;

    // 通知 UI 刷新 DisplaySource
    public void RefreshDisplay() => OnPropertyChanged(nameof(DisplaySource));
}

public class InspectWorkflowNode
{
    public string? statusValue { get; set; }  // "0" | "1" | "2"
    public string? statusName { get; set; }  // "新建"、"待检验" 等（仅展示用）
    public string? statusTime { get; set; }  // "2025-01-02 12:34:56"
}
public class WorkflowVmItem
{
    public string StatusValue { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Time { get; set; }

    // 计算/标注用
    public int StepNo { get; set; }           // 1,2,3
    public bool IsCompleted { get; set; }     // 已完成（在当前之前且有时间）
    public bool IsCurrent { get; set; }       // 当前节点（最后一个有时间的）
    public bool IsActive => IsCompleted || IsCurrent; // 蓝色描边/连线的条件
    public bool IsLast { get; set; }
}


public class MaintenanceRecordDto
{
    /// <summary>主键ID</summary>
    public string? id { get; set; }

    /// <summary>设备名称</summary>
    public string? devName { get; set; }

    public string? devCode { get; set; }

    /// <summary>工厂编码</summary>
    public string? factoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? factoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? upkeepNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? upkeepStatus { get; set; }
    public string? upkeepStatusText { get; set; }

    public string? upkeepResult { get; set; }


    /// <summary>计划编码</summary>
    public string? planCode { get; set; }

    /// <summary>计划名称</summary>
    public string? planName { get; set; }

    /// <summary>实际保养日期</summary>
    public string? upkeepTime { get; set; }

    /// <summary>计划保养日期</summary>
    public string? planUpkeepTime { get; set; }

    /// <summary>备注</summary>
    public string? memo { get; set; }

    /// <summary>创建时间</summary>
    public string? createdTime { get; set; }

    /// <summary>修改时间</summary>
    public string? modifiedTime { get; set; }
}

/// <summary>
/// 设备
/// </summary>
public class MaintenanceOrderItem
{
    /// <summary>主键ID</summary>
    public string? Id { get; set; }

    /// <summary>设备名称</summary>
    public string? DevName { get; set; }

    /// <summary>设备编码</summary>
    public string? DevCode { get; set; }

    /// <summary>工厂编码</summary>
    public string? FactoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? FactoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? UpkeepNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? UpkeepStatus { get; set; }

    /// <summary>检验状态名称（映射显示用）</summary>
    public string? UpkeepStatusText { get; set; }

    /// <summary>检验员</summary>
    public string? UpkeepOperator { get; set; }

    /// <summary>计划编码</summary>
    public string? PlanCode { get; set; }

    /// <summary>计划名称</summary>
    public string? PlanName { get; set; }

    /// <summary>计划保养日期</summary>
    public DateTime? PlanUpkeepTime { get; set; }

    /// <summary>保养日期</summary>
    public DateTime? UpkeepTime { get; set; }

    /// <summary>备注</summary>
    public string? Memo { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreatedTime { get; set; }

    /// <summary>修改时间</summary>
    public DateTime? ModifiedTime { get; set; }
}

public class DictMaintenance
{
    public List<DictItem> MaintenanceStatus { get; set; } = new();
}


public class MaintenanceDetailDto : ObservableObject
{
    public string? id { get; set; }
    /// <summary>设备编码</summary>
    public string? devCode { get; set; }

    /// <summary>工厂编码</summary>
    public string? factoryCode { get; set; }

    /// <summary>工厂名称</summary>
    public string? factoryName { get; set; }

    /// <summary>质检单号</summary>
    public string? upkeepNo { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? upkeepStatus { get; set; }

    /// <summary>检验状态名称（映射显示用）</summary>
    public string? upkeepStatusText { get; set; }
    //保养内容
    public string? upkeepContent { get; set; }
    //耗材
    public string? consumeMaterial { get; set; }
    //工具
    public string? upkeepTool { get; set; }

    //标准
    public string? upkeepStandard { get; set; }

    //保养结果
    public string? upkeepResult { get; set; }

    public string? upkeepMemo { get; set; }

    /// <summary>计划编码</summary>
    public string? planCode { get; set; }

    /// <summary>计划名称</summary>
    public string? planName { get; set; }

    /// <summary>计划保养日期</summary>
    public string? planUpkeepTime { get; set; }

    /// <summary>保养日期</summary>
    public string? upkeepTime { get; set; }

    /// <summary>备注</summary>
    public string? memo { get; set; }

    /// <summary>创建时间</summary>
    public string? createdTime { get; set; }

    /// <summary>修改时间</summary>
    public string? modifiedTime { get; set; }

    public List<MaintenanceItem>? devUpkeepTaskDetailList { get; set; } = new();
    public List<MaintenanceAttachment>? devUpkeepTaskAttachmentList { get; set; } = new();


}



public class MaintenanceMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? spec { get; set; }
    public decimal? qty { get; set; }               // 生产/到货数量
    public string? unit { get; set; }
}

public partial class MaintenanceItem : ObservableObject
{
    public int? index { get; set; }
    public string? id { get; set; }
    public string? itemName { get; set; }
    public string? itemCode { get; set; }
    public string? upkeepStandard { get; set; }
    public string? upkeepContent { get; set; }
    public string? consumeMaterial { get; set; }
    public string? upkeepTool { get; set; }
    public string? upkeepResult { get; set; }
    public string? upkeepNo { get; set; }
    public string? memo { get; set; }

}

public class MaintenanceAttachment
{
    public string? id { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentUrl { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentLocation { get; set; } // main/table
    public decimal? attachmentSize { get; set; }     // KB
    public string? attachmentFolder { get; set; }
    public string? createdTime { get; set; }
}

public partial class OrderMaintenanceAttachmentItem : ObservableObject
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
    // 预览接口返回的直连 URL（短期有效）
    private string? _previewUrl;
    public string? PreviewUrl { get => _previewUrl; set => SetProperty(ref _previewUrl, value); }

    // 供 XAML 绑定：优先显示 Preview → Local → 原地址
    public string? DisplaySource => PreviewUrl ?? LocalPath ?? AttachmentUrl;

    // 通知 UI 刷新 DisplaySource
    public void RefreshDisplay() => OnPropertyChanged(nameof(DisplaySource));
}

public class MaintenanceWorkflowNode
{
    public string? statusValue { get; set; }  // "0" | "1" | "2"
    public string? statusName { get; set; }  // "新建"、"待检验" 等（仅展示用）
    public string? statusTime { get; set; }  // "2025-01-02 12:34:56"
}


public class RepairRecordDto
{
    public string? id { get; set; }
    public string? maintainNo { get; set; }
    public string? workOrderNo { get; set; }

    public string? devCode { get; set; }
    public string? devName { get; set; }
    public string? devModel { get; set; }

    public string? maintainType { get; set; }
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? assignTo { get; set; }
    public string? assignToName { get; set; }

    public string? mainRepairUser { get; set; }
    public string? assitRepairUsers { get; set; }

    public string? acceptanceDate { get; set; }
    public string? acceptanceOpinion { get; set; }
    public string? acceptor { get; set; }

    public string? expectedRepairDate { get; set; }
    public string? completedRepairDate { get; set; }
    public string? repairStartTime { get; set; }
    public string? repairEndTime { get; set; }
    public string? repairResult { get; set; }

    public decimal? repairDuration { get; set; }
    public string? urgent { get; set; }

    public string? urgentText { get; set; }

    public bool? auditBack { get; set; }
    public string? auditStatus { get; set; }
    public string?  auditStatusText { get; set; }

    public string? dataSource { get; set; }
    public string? dataTimeId { get; set; }
    public bool? delStatus { get; set; }

    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }

}

/// <summary>
/// 设备
/// </summary>
public class RepairOrderItem
{
    public string? id { get; set; }
    public string? maintainNo { get; set; }
    public string? workOrderNo { get; set; }

    public string? devCode { get; set; }
    public string? devName { get; set; }
    public string? devModel { get; set; }

    public string? maintainType { get; set; }
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? assignTo { get; set; }
    public string? assignToName { get; set; }

    public string? mainRepairUser { get; set; }
    public string? assitRepairUsers { get; set; }

    public string? acceptanceDate { get; set; }
    public string? acceptanceOpinion { get; set; }
    public string? acceptor { get; set; }

    public DateTime? expectedRepairDate { get; set; }
    public DateTime? completedRepairDate { get; set; }
    public DateTime? repairStartTime { get; set; }
    public DateTime? repairEndTime { get; set; }
    public string? repairResult { get; set; }


    public decimal? repairDuration { get; set; }
    public string? urgent { get; set; }

    public string? urgentText { get; set; }

    public bool? auditBack { get; set; }
    public string? auditStatus { get; set; }

    public string? auditStatusText { get; set; }

    public string? dataSource { get; set; }
    public string? dataTimeId { get; set; }
    public bool? delStatus { get; set; }

    public DateTime? createdTime { get; set; }
    public string? creator { get; set; }
    public DateTime? modifiedTime { get; set; }
    public string? modifier { get; set; }
}

public class DictRepair
{
    public List<DictItem> AuditStatus { get; set; } = new();

    public List<DictItem> Urgent { get; set; } = new();

    public List<DictItem> MaintainType { get; set; } = new();
}


public partial class RepairDetailDto : ObservableObject
{
    public string? id { get; set; }
    public string? maintainNo { get; set; }
    public string? workOrderNo { get; set; }

    public string? devCode { get; set; }
    public string? devName { get; set; }
    public string? devModel { get; set; }

    public string? maintainType { get; set; }

    [ObservableProperty] private string? maintainTypeText;
    [ObservableProperty] private string? mainRepairUserText;
    [ObservableProperty] private string? assitRepairUsersText;
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? assignTo { get; set; }
    public string? assignToName { get; set; }

    public string? mainRepairUser { get; set; }
    public string? assitRepairUsers { get; set; }

    [ObservableProperty] private string? repairStartTime;
    [ObservableProperty] private string? repairEndTime;
    [ObservableProperty] private string? expectedRepairDate;
    public string? acceptanceOpinion { get; set; }
    public string? acceptor { get; set; }
    public string? completedRepairDate { get; set; }
    public string? repairResult { get; set; }

    public decimal? repairDuration { get; set; }
    public string? urgent { get; set; }

    [ObservableProperty] public string? urgentText;

    public bool? auditBack { get; set; }
    public string? auditStatus { get; set; }

    public string? dataSource { get; set; }
    public string? dataTimeId { get; set; }
    public bool? delStatus { get; set; }

    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }

    public MaintainReportDomain? maintainReportDomain { get; set; }

    public List<RepairAttachment>? maintainWorkOrderAttachmentDomainList { get; set; }
    public List<MaintainWorkOrderItemDomain>? maintainWorkOrderItemDomainList { get; set; }


}

/// <summary>
/// 维修报告
/// </summary>
public class MaintainReportDomain
{
    public string? id { get; set; }
    public string? maintainNo { get; set; }
    public string? devCode { get; set; }
    public string? devName { get; set; }
    public string? devModel { get; set; }
    public string? devStatus { get; set; }

    public string? workOrderId { get; set; }
    public string? workOrderNo { get; set; }

    public string? phenomena { get; set; }
    public string? description { get; set; }
    public string? memo { get; set; }
    public string? urgent { get; set; }

    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? workShopName { get; set; }

    public string? expectedRepairDate { get; set; }
    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }

    public bool? auditBack { get; set; }
    public string? auditStatus { get; set; }
    public bool? delStatus { get; set; }
    public string? dataSource { get; set; }
    public string? dataTimeId { get; set; }

    public List<RepairReportAttachment>? maintainReportAttachmentDomainList { get; set; }
}

/// <summary>
/// 报告附件
/// </summary>
public class RepairReportAttachment : ObservableObject
{
    public string? id { get; set; }
    public string? devMaintainId { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentFolder { get; set; }
    public string? attachmentLocation { get; set; }
    public string? attachmentUrl { get; set; }
    public decimal? attachmentSize { get; set; }

    public string? memo { get; set; }
    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }
    public bool? delStatus { get; set; }
}

/// <summary>
/// 工单附件
/// </summary>
public class RepairAttachment
{
    public string? id { get; set; }
    public string? workOrderId { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public string? attachmentExt { get; set; }
    public string? attachmentFolder { get; set; }
    public string? attachmentLocation { get; set; }
    public string? attachmentUrl { get; set; }
    public decimal? attachmentSize { get; set; }

    public string? memo { get; set; }
    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }
    public bool? delStatus { get; set; }
}

/// <summary>
/// 工单项目明细
/// </summary>
public class MaintainWorkOrderItemDomain
{
    public int? index { get; set; }
    public string? id { get; set; }
    public string? workOrderId { get; set; }
    public string? factoryCode { get; set; }
    public string? faultName { get; set; }
    public string? faultDescription { get; set; }
    public string? faultCause { get; set; }
    public string? faultPart { get; set; }
    public string? repairMethod { get; set; }
    public string? suggestions { get; set; }

    public string? memo { get; set; }
    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }
    public bool? delStatus { get; set; }
}

public class RepairMaterial
{
    public string? materialCode { get; set; }
    public string? materialName { get; set; }
    public string? model { get; set; }
    public string? spec { get; set; }
    public decimal? qty { get; set; }               // 生产/到货数量
    public string? unit { get; set; }
}

public partial class RepairItem : ObservableObject
{
    public int? index { get; set; }
    public string? id { get; set; }
    public string? itemName { get; set; }
    public string? itemCode { get; set; }
    public string? maintainionStandard { get; set; }
    public string? maintainionMode { get; set; }
    public string? maintainionAttributeName { get; set; }
    public string? maintainionAttribute { get; set; }
    public string? maintainValue { get; set; }
    public string? maintainResult { get; set; }
    public string? maintainNo { get; set; }

    public string? memo { get; set; }
    public string? upperLimit { get; set; }
    public string? lowerLimit { get; set; }
    public string? standardValue { get; set; }
    public string? unit { get; set; }
}

public partial class OrderRepairAttachmentItem : ObservableObject
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
    // 预览接口返回的直连 URL（短期有效）
    private string? _previewUrl;
    public string? PreviewUrl { get => _previewUrl; set => SetProperty(ref _previewUrl, value); }

    // 供 XAML 绑定：优先显示 Preview → Local → 原地址
    public string? DisplaySource => PreviewUrl ?? LocalPath ?? AttachmentUrl;

    // 通知 UI 刷新 DisplaySource
    public void RefreshDisplay() => OnPropertyChanged(nameof(DisplaySource));
}

public class RepairWorkflowNode
{
    public string? statusValue { get; set; }  // "0" | "1" | "2"
    public string? statusName { get; set; }  // "新建"、"待检验" 等（仅展示用）
    public string? statusTime { get; set; }  // "2025-01-02 12:34:56"
}
public class RepairWorkflowVmItem
{
    public string? StatusValue { get; set; }
    public string? Title { get; set; }
    public string? Time { get; set; }
    public int StepNo { get; set; }

    public bool IsCurrent { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; }   // 用于标题/连线变色
    public bool IsLast { get; set; }   // 最后一个节点隐藏连线
    public bool IsRowEnd { get; set; }   // 每行末尾（第 3、6 个）隐藏连线

}