namespace ImportOps.Domain.Entities;

public sealed class WorkflowTransition
{
    public long WorkflowTransitionId { get; set; }
    public long CompanyId { get; set; }
    public int FromStageId { get; set; }
    public int ToStageId { get; set; }
    public bool RequiresComment { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StageTaskTemplate
{
    public long StageTaskTemplateId { get; set; }
    public long CompanyId { get; set; }
    public int WorkflowStageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public int? DueDaysOffset { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StageDocumentRequirement
{
    public long StageDocumentRequirementId { get; set; }
    public long CompanyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int DocumentTypeId { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
}
