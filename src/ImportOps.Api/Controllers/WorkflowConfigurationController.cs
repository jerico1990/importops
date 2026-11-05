using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/configuration/workflow")]
[RequirePermission(Permissions.ConfigurationView)]
public sealed class WorkflowConfigurationController(IWorkflowConfigurationService service) : ControllerBase
{
    [HttpGet("task-templates")]
    public async Task<IActionResult> SearchTaskTemplates([FromQuery] WorkflowTaskTemplateQuery query, CancellationToken ct) =>
        Ok(await service.SearchTaskTemplatesAsync(query, ct));

    [HttpPost("task-templates")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> CreateTaskTemplate([FromBody] SaveWorkflowTaskTemplateRequest request, CancellationToken ct) =>
        Ok(await service.CreateTaskTemplateAsync(request, ct));

    [HttpPut("task-templates/{id:long}")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> UpdateTaskTemplate(long id, [FromBody] SaveWorkflowTaskTemplateRequest request, CancellationToken ct) =>
        Ok(await service.UpdateTaskTemplateAsync(id, request, ct));

    [HttpPatch("task-templates/{id:long}/active")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> SetTaskTemplateActive(long id, [FromBody] SetWorkflowTaskTemplateActiveRequest request, CancellationToken ct)
    {
        await service.SetTaskTemplateActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    [HttpGet("task-templates/{id:long}/delete-check")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> CheckTaskTemplateDelete(long id, CancellationToken ct) =>
        Ok(await service.CheckTaskTemplateDeleteAsync(id, ct));

    [HttpDelete("task-templates/{id:long}")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> DeleteTaskTemplate(long id, CancellationToken ct)
    {
        await service.DeleteTaskTemplateAsync(id, ct);
        return NoContent();
    }
}
