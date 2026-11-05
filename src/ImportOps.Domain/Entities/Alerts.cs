namespace ImportOps.Domain.Entities;

public sealed class AlertRule
{
    public int AlertRuleId { get; set; }
    public long CompanyId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = AlertSeverities.Warning;
    public string Audience { get; set; } = AlertAudiences.Internal;
    public int? ThresholdHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public sealed class AlertRecord
{
    public long AlertId { get; set; }
    public long CompanyId { get; set; }
    public int AlertRuleId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = AlertSeverities.Warning;
    public string Audience { get; set; } = AlertAudiences.Internal;
    public long? OperationId { get; set; }
    public long? ClientId { get; set; }
    public long? ResponsibleUserId { get; set; }
    public string? RequiredPermission { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public long? SourceId { get; set; }
    public string? ActionPath { get; set; }
    public string DedupKey { get; set; } = string.Empty;
    public string Status { get; set; } = AlertStatuses.Active;
    public DateTime TriggeredAt { get; set; }
    public DateTime LastDetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public sealed class AlertUserState
{
    public long AlertId { get; set; }
    public long UserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime? DismissedAt { get; set; }
}

public static class AlertStatuses { public const string Active="ACTIVE"; public const string Resolved="RESOLVED"; }
public static class AlertSeverities
{
    public const string Info="INFO"; public const string Warning="WARNING"; public const string Critical="CRITICAL";
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase){Info,Warning,Critical};
}
public static class AlertAudiences { public const string Internal="INTERNAL"; public const string Client="CLIENT"; public const string Both="BOTH"; }
public static class AlertTypes
{
    public const string RequestPendingReview="REQUEST_PENDING_REVIEW";
    public const string RequestObserved="REQUEST_OBSERVED";
    public const string QuotationPendingApproval="QUOTATION_PENDING_APPROVAL";
    public const string QuotationRejected="QUOTATION_REJECTED";
    public const string QuotationApprovedOperationPending="QUOTATION_APPROVED_OPERATION_PENDING";
    public const string OperationOnHold="OPERATION_ON_HOLD";
    public const string OperationCancelled="OPERATION_CANCELLED";
    public const string TaskDueSoon="TASK_DUE_SOON";
    public const string TaskOverdue="TASK_OVERDUE";
    public const string ShipmentEtaSoon="SHIPMENT_ETA_SOON";
    public const string ShipmentEtaOverdue="SHIPMENT_ETA_OVERDUE";
    public const string ShipmentArrived="SHIPMENT_ARRIVED";
    public const string DocumentObserved="DOCUMENT_OBSERVED";
    public const string DocumentRequiredMissing="DOCUMENT_REQUIRED_MISSING";
    public const string CustomsObservationOpen="CUSTOMS_OBSERVATION_OPEN";
    public const string CustomsReleasePending="CUSTOMS_RELEASE_PENDING";
    public const string CustomsReleased="CUSTOMS_RELEASED";
    public const string SettlementPending="SETTLEMENT_PENDING";
    public const string OperationReadyToClose="OPERATION_READY_TO_CLOSE";
    public static readonly IReadOnlySet<string> ClientVisible = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { RequestObserved, QuotationPendingApproval, OperationOnHold, OperationCancelled, ShipmentEtaSoon, ShipmentEtaOverdue, ShipmentArrived, DocumentObserved, DocumentRequiredMissing, CustomsReleased };
}
