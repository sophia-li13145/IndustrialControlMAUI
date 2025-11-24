using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public List<DictItem> InspectResult { get; set; } = new();
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

public partial class InspectionItem : INotifyPropertyChanged
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
    private string? _inspectResult;
    public string? inspectResult
    {
        get => _inspectResult;
        set
        {
            if (_inspectResult != value)
            {
                _inspectResult = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string? _inspectResultText;
    public string? inspectResultText
    {
        get => _inspectResultText;
        set
        {
            if (_inspectResultText != value)
            {
                _inspectResultText = value;
                OnPropertyChanged();
            }
        }
    }
    public string? inspectNo { get; set; }

    public string? memo { get; set; }
    public string? upperLimit { get; set; }
    public string? lowerLimit { get; set; }
    public string? standardValue { get; set; }
    public string? unit { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    public string? name { get; set; }        // 前端显示名（通常就是文件名）
    public int? percent { get; set; }     // 进度（完成100）
    public string? status { get; set; }      // "done" / "uploading" / "error"
    public string? uid { get; set; }         // 前端生成或服务端返回的 uid
    public string? url { get; set; }         // 可直接访问的绝对地址（若有）
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
    public string? Name { get; set; } = null;       // 默认用文件名
    public int Percent { get; set; } = 100;
    public string Status { get; set; } = "done";
    public string? Uid { get; set; } = null;
    public string? Url { get; set; } = null;        // 绝对可访问地址（若有）
    public string? QualityNo { get; set; } = null;  // 质检单号（从 Detail.qualityNo 带过来）
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
    public List<DictItem> MaintenanceResult { get; set; } = new();
}


public class MaintenanceDetailDto : ObservableObject
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
    public string? upkeepNo { get; set; }

    public string? upkeepOperator { get; set; }

    /// <summary>检验状态（0-新建；1-待检验；2-检验中；3-检验完成）</summary>
    public string? upkeepStatus { get; set; }
    //保养内容
    public string? upkeepContent { get; set; }
    //耗材
    public string? consumeMaterial { get; set; }
    //工具
    public string? upkeepTool { get; set; }

    //标准
    public string? upkeepStandard { get; set; }

    public string?  upkeepResultText { get; set; }

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

    private string? _upkeepResult;
    public string? upkeepResult
    {
        get => _upkeepResult;
        set
        {
            if (_upkeepResult != value)
            {
                _upkeepResult = value;
                OnPropertyChanged();
            }
        }
    }
    private string? _upkeepResultText;
    public string? upkeepResultText
    {
        get => _upkeepResultText;
        set
        {
            if (_upkeepResultText != value)
            {
                _upkeepResultText = value;
                OnPropertyChanged();
            }
        }
    }
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
    public string? name { get; set; }        // 前端显示名（通常就是文件名）
    public int? percent { get; set; }     // 进度（完成100）
    public string? status { get; set; }      // "done" / "uploading" / "error"
    public string? uid { get; set; }         // 前端生成或服务端返回的 uid
    public string? url { get; set; }         // 可直接访问的绝对地址（若有）

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
    public string? Name { get; set; } = null;       // 默认用文件名
    public int Percent { get; set; } = 100;
    public string Status { get; set; } = "done";
    public string? Uid { get; set; } = null;
    public string? Url { get; set; } = null;        // 绝对可访问地址（若有）
    public string? QualityNo { get; set; } = null;  // 质检单号（从 Detail.qualityNo 带过来）
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
    public string? name { get; set; }        // 前端显示名（通常就是文件名）
    public int? percent { get; set; }     // 进度（完成100）
    public string? status { get; set; }      // "done" / "uploading" / "error"
    public string? uid { get; set; }         // 前端生成或服务端返回的 uid
    public string? url { get; set; }         // 可直接访问的绝对地址（若有）
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
    public string? Name { get; set; } = null;       // 默认用文件名
    public int Percent { get; set; } = 100;
    public string Status { get; set; } = "done";
    public string? Uid { get; set; } = null;
    public string? Url { get; set; } = null;        // 绝对可访问地址（若有）
    public string? QualityNo { get; set; } = null;  // 质检单号（从 Detail.qualityNo 带过来）
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

    /// <summary>
    /// 设备巡检 - 详情 DTO（与后端字段一一对应）
    /// </summary>
    public class InspectDetailDto : ObservableObject
    {
        // ======================= 顶层基础字段 =======================

        public string? id { get; set; }

    public decimal? qualifiedNum { get; set; }
    public decimal? unqualifiedNum { get; set; }
    public decimal? inspectNum { get; set; }

    /// <summary>
    /// 巡检备注
    /// </summary>
    public string? inspectMemo { get; set; }



        private string? _inspectResult;
        /// <summary>
        /// 检验结果（合格 / 不合格 等）
        /// </summary>
        public string? inspectResult
        {
            get => _inspectResult;
            set => SetProperty(ref _inspectResult, value);
        }

        /// <summary>
        /// 巡检时间
        /// </summary>
        public string? inspectTime { get; set; }

        /// <summary>
        /// 巡检人
        /// </summary>
        public string? inspecter { get; set; }

       

        // ======================= 明细 + 附件 =======================

        /// <summary>
        /// 明细列表（与 JSON devInspectTaskDetailList 对应）
        /// </summary>
        public List<InspectionItem>? devInspectTaskDetailList { get; set; } = new();

        /// <summary>
        /// 附件列表（与 JSON devInspectTaskAttachmentList 对应）
        /// </summary>
        public List<InspectionAttachment>? devInspectTaskAttachmentList { get; set; } = new();


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

    /// <summary>计划编码</summary>
    public string? planCode { get; set; }

    /// <summary>计划名称</summary>
    public string? planName { get; set; }

    /// <summary>计划检验日期</summary>
    public string? planInspectTime { get; set; }

    /// <summary>备注</summary>
    public string? memo { get; set; }

    public string? creator { get; set; }

    public string? modifier { get; set; }

    /// <summary>创建时间</summary>
    public string? createdTime { get; set; }

    /// <summary>修改时间</summary>
    public string? modifiedTime { get; set; }



}

    /// <summary>
    /// 设备巡检 - 附件项（devInspectTaskAttachmentList）
    /// </summary>
    public class DevInspectTaskAttachment
    {
        public string? attachmentExt { get; set; }
        public string? attachmentFolder { get; set; }
        public string? attachmentLocation { get; set; }
        public string? attachmentName { get; set; }
        public string? attachmentRealName { get; set; }
        public decimal? attachmentSize { get; set; }
        public string? attachmentUrl { get; set; }
        public string? id { get; set; }
        public string? memo { get; set; }
    }

    /// <summary>
    /// 设备巡检 - 明细项（devInspectTaskDetailList）
    /// </summary>
    public class DevInspectTaskDetailItem
    {
    public int? index { get; set; }
    public string? id { get; set; }

        /// <summary>
        /// 明细检验结果
        /// </summary>
        public string? inspectResult { get; set; }

        /// <summary>
        /// 实测值
        /// </summary>
        public string? inspectValue { get; set; }

        public string? inspectionAttribute { get; set; }
        public string? inspectionAttributeName { get; set; }
        public string? inspectionMode { get; set; }
        public string? inspectionStandard { get; set; }
        public string? itemCode { get; set; }
        public string? itemName { get; set; }

        public string? lowerLimit { get; set; }
        public string? memo { get; set; }
        public string? standardValue { get; set; }
        public string? unit { get; set; }
        public string? unitCode { get; set; }
        public string? upperLimit { get; set; }
}


    // =======================================================================
    // 维修附件项：maintainWorkOrderAttachmentDomainList
    // =======================================================================

    public class RepairAttachmentItem
    {
        public string? attachmentExt { get; set; }
        public string? attachmentFolder { get; set; }
        public string? attachmentLocation { get; set; }
        public string? attachmentName { get; set; }
        public string? attachmentRealName { get; set; }
        public decimal? attachmentSize { get; set; }
        public string? attachmentUrl { get; set; }
        public string? id { get; set; }
        public string? memo { get; set; }
    }

    // =======================================================================
    // 维修明细项：maintainWorkOrderItemDomainList
    // =======================================================================

    public class RepairDetailItem
    {
        public string? id { get; set; }
        public string? faultCause { get; set; }
        public string? faultDescription { get; set; }
        public string? faultName { get; set; }
        public string? faultPart { get; set; }
        public string? memo { get; set; }
        public string? repairMethod { get; set; }
        public string? suggestions { get; set; }
    }
//异常提报
public class MaintenanceReportDto : ObservableObject
{
    public bool auditBack { get; set; }
    public string? auditStatus { get; set; }
    public string? auditStatusText { get; set; }

    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public string? dataSource { get; set; }
    public string? dataTimeId { get; set; }
    public bool delStatus { get; set; }
    public string? description { get; set; }
    private string? _devCode;
    public string? devCode
    {
        get => _devCode;
        set => SetProperty(ref _devCode, value);
    }
    private string? _devModel;
    public string? devModel
    {
        get => _devModel;
        set => SetProperty(ref _devModel, value);
    }
    private string? _devName;
    public string? devName
    {
        get => _devName;
        set => SetProperty(ref _devName, value);
    }
    public string? devStatus { get; set; }
    public string? expectedRepairDate { get; set; }
    public string? factoryCode { get; set; }
    public string? factoryName { get; set; }
    public string? id { get; set; }
    public string? maintainNo { get; set; }
    public List<MaintainReportAttachment> maintainReportAttachmentDomainList { get; set; } = new();
    public string? memo { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }
    public string? phenomena { get; set; }
    public string? urgent { get; set; }
    private string? _urgentText;
    public string? urgentText
    {
        get => _urgentText;
        set => SetProperty(ref _urgentText, value);
    }

    private string? _devStatusText;
    public string? devStatusText
    {
        get => _devStatusText;
        set => SetProperty(ref _devStatusText, value);
    }

    public string? workOrderId { get; set; }
    public string? workOrderNo { get; set; }
    private string? _workShopName;
    public string? workShopName
    {
        get => _workShopName;
        set => SetProperty(ref _workShopName, value);
    }
}
public class MaintainReportAttachment
{
    public string? attachmentExt { get; set; }
    public string? attachmentFolder { get; set; }
    public string? attachmentLocation { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public decimal? attachmentSize { get; set; }
    public string? attachmentUrl { get; set; }
    public string? createdTime { get; set; }
    public string? creator { get; set; }
    public bool delStatus { get; set; }
    public string? devMaintainId { get; set; }
    public string? id { get; set; }
    public string? memo { get; set; }
    public string? modifiedTime { get; set; }
    public string? modifier { get; set; }
    public string status { get; set; } = "done";
    public string? uid { get; set; } = null;
    public string? url { get; set; } = null;        // 绝对可访问地址（若有）
}
public class DictExcept
{
    public List<DictItem> AuditStatus { get; set; } = new();

    public List<DictItem> Urgent { get; set; } = new();

    public List<DictItem> DevStatus { get; set; } = new();
}
public partial class OrderExceptAttachmentItem : ObservableObject
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
    public string? Name { get; set; } = null;       // 默认用文件名
    public int Percent { get; set; } = 100;
    public string Status { get; set; } = "done";
    public string? Uid { get; set; } = null;
    public string? Url { get; set; } = null;        // 绝对可访问地址（若有）
    public string? QualityNo { get; set; } = null;  // 质检单号（从 Detail.qualityNo 带过来）
}
public class ExceptWorkflowVmItem
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
public class ExceptWorkflowNode
{
    public string? statusValue { get; set; }  // "0" | "1" | "2"
    public string? statusName { get; set; }  // "新建"、"待检验" 等（仅展示用）
    public string? statusTime { get; set; }  // "2025-01-02 12:34:56"
}

// 异常提报 - 新建接口入参（严格对应 swagger 上那段 JSON）
public class BuildExceptRequest
{
    public string? description { get; set; }
    public string? devCode { get; set; }
    public string? devModel { get; set; }
    public string? devName { get; set; }
    public string? devStatus { get; set; }
    public string? expectedRepairDate { get; set; }
    public string? id { get; set; }                    // 新建一般为 null

    public List<BuildExceptAttachment> maintainReportAttachmentDomainList { get; set; } = new();

    public string? memo { get; set; }
    public string? phenomena { get; set; }
    public string? urgent { get; set; }
    public string? workShopName { get; set; }
}

// 附件结构：只保留接口例子里的字段
public class BuildExceptAttachment
{
    public string? attachmentExt { get; set; }
    public string? attachmentFolder { get; set; }
    public string? attachmentLocation { get; set; }
    public string? attachmentName { get; set; }
    public string? attachmentRealName { get; set; }
    public decimal? attachmentSize { get; set; }
    public string? attachmentUrl { get; set; }
    public string? id { get; set; }
    public string? memo { get; set; }
}




