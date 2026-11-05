namespace ImportOps.Application.Alerts;
public interface IAlertService
{
 Task EvaluateCompanyAsync(long companyId,CancellationToken ct);
 Task EvaluateAllCompaniesAsync(CancellationToken ct);
 Task<AlertPageDto> GetAsync(AlertQuery query,CancellationToken ct);
 Task<AlertSummaryDto> GetSummaryAsync(CancellationToken ct);
 Task MarkReadAsync(long alertId,CancellationToken ct);
 Task MarkAllReadAsync(CancellationToken ct);
 Task DismissAsync(long alertId,CancellationToken ct);
 Task<IReadOnlyList<AlertRuleDto>> GetRulesAsync(CancellationToken ct);
 Task<AlertRuleDto> UpdateRuleAsync(int id,UpdateAlertRuleRequest request,CancellationToken ct);
}
