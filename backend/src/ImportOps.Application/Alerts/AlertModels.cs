namespace ImportOps.Application.Alerts;
public sealed record AlertQuery(string? Status=null,string? Severity=null,bool? Unread=null,int Page=1,int PageSize=30);
public sealed record AlertDto(long AlertId,string AlertType,string Severity,string Status,string Title,string Message,long? OperationId,string? OperationNo,long? ClientId,string? ClientName,string SourceType,long? SourceId,string? ActionPath,DateTime TriggeredAt,bool IsRead);
public sealed record AlertPageDto(IReadOnlyList<AlertDto> Items,int Total,int Page,int PageSize);
public sealed record AlertSummaryDto(int Active,int Unread,int Critical,int Warning);
public sealed record AlertRuleDto(int AlertRuleId,string AlertType,string Name,string? Description,string Severity,string Audience,int? ThresholdHours,bool IsActive);
public sealed record UpdateAlertRuleRequest(string Severity,string Audience,int? ThresholdHours,bool IsActive);
