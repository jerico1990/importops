using ImportOps.Api.Security;
using ImportOps.Application.Alerts;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.AlertsView)]
[Route("api/alerts")]
public sealed class AlertsController(IAlertService service):ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get([FromQuery]AlertQuery query,CancellationToken ct)=>Ok(await service.GetAsync(query,ct));
    [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct)=>Ok(await service.GetSummaryAsync(ct));
    [HttpPost("{id:long}/read")] public async Task<IActionResult> Read(long id,CancellationToken ct){await service.MarkReadAsync(id,ct);return NoContent();}
    [HttpPost("read-all")] public async Task<IActionResult> ReadAll(CancellationToken ct){await service.MarkAllReadAsync(ct);return NoContent();}
    [HttpPost("{id:long}/dismiss")] public async Task<IActionResult> Dismiss(long id,CancellationToken ct){await service.DismissAsync(id,ct);return NoContent();}
    [HttpGet("rules")][RequirePermission(Permissions.AlertsManage)] public async Task<IActionResult> Rules(CancellationToken ct)=>Ok(await service.GetRulesAsync(ct));
    [HttpPut("rules/{id:int}")][RequirePermission(Permissions.AlertsManage)] public async Task<IActionResult> Rule(int id,[FromBody]UpdateAlertRuleRequest request,CancellationToken ct)=>Ok(await service.UpdateRuleAsync(id,request,ct));
    [HttpPost("evaluate")][RequirePermission(Permissions.AlertsManage)] public async Task<IActionResult> Evaluate(CancellationToken ct){await service.EvaluateAllCompaniesAsync(ct);return NoContent();}
}
