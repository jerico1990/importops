using ImportOps.Application.Common;

namespace ImportOps.Application.Workflow;

public interface IWorkflowConfigurationService
{
    Task<PagedResult<WorkflowTaskTemplateDto>> SearchTaskTemplatesAsync(WorkflowTaskTemplateQuery query, CancellationToken ct);
    Task<WorkflowTaskTemplateDto> CreateTaskTemplateAsync(SaveWorkflowTaskTemplateRequest request, CancellationToken ct);
    Task<WorkflowTaskTemplateDto> UpdateTaskTemplateAsync(long id, SaveWorkflowTaskTemplateRequest request, CancellationToken ct);
    Task SetTaskTemplateActiveAsync(long id, bool active, CancellationToken ct);
    Task<WorkflowTaskTemplateDeleteCheckDto> CheckTaskTemplateDeleteAsync(long id, CancellationToken ct);
    Task DeleteTaskTemplateAsync(long id, CancellationToken ct);
}
