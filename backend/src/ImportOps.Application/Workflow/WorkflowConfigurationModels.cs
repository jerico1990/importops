namespace ImportOps.Application.Workflow;

public sealed record WorkflowTaskTemplateQuery(
    int Page = 1,
    int PageSize = 20,
    int? WorkflowStageId = null,
    string? Search = null,
    string Status = "ALL");

public sealed record WorkflowTaskTemplateDto(
    long StageTaskTemplateId,
    int WorkflowStageId,
    string StageCode,
    string StageName,
    string Title,
    string? Description,
    bool IsMandatory,
    int? DueDaysOffset,
    int DisplayOrder,
    bool IsActive,
    int GeneratedTasksCount);

public sealed record SaveWorkflowTaskTemplateRequest(
    int WorkflowStageId,
    string Title,
    string? Description,
    bool IsMandatory,
    int? DueDaysOffset,
    int DisplayOrder);

public sealed record SetWorkflowTaskTemplateActiveRequest(bool IsActive);

public sealed record WorkflowTaskTemplateDeleteCheckDto(
    bool CanDelete,
    int GeneratedTasksCount,
    string Message);
