namespace ImportOps.Domain.Entities;

public sealed class ImportOperation
{
    public long OperationId { get; set; }
    public long CompanyId { get; set; }
    public string OperationNo { get; set; } = string.Empty;

    public long? RequestId { get; set; }
    public long? QuotationId { get; set; }
    public long ClientId { get; set; }
    public long? ClientContactId { get; set; }
    public long? SupplierId { get; set; }
    public long ResponsibleUserId { get; set; }

    public string? ClientNameSnapshot { get; set; }
    public string? ClientContactNameSnapshot { get; set; }
    public string? SupplierNameSnapshot { get; set; }
    public string? QuotationNoSnapshot { get; set; }
    public int? QuotationVersionSnapshot { get; set; }
    public string? QuotationCurrencySnapshot { get; set; }
    public decimal? QuotationTotalSnapshot { get; set; }

    public int CurrentStageId { get; set; }
    public string OperationalStatus { get; set; } = OperationStatuses.Open;
    public string DocumentaryStatus { get; set; } = DocumentaryStatuses.Pending;
    public string FinancialStatus { get; set; } = FinancialStatuses.NoMovements;

    public int? TransportModeId { get; set; }
    public string? IncotermCode { get; set; }
    public string? OriginCountryCode { get; set; }
    public string? DestinationCountryCode { get; set; }
    public long? OriginPortId { get; set; }
    public long? DestinationPortId { get; set; }

    public string MerchandiseDescription { get; set; } = string.Empty;
    public decimal? PackageCount { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? NetWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? DeclaredValue { get; set; }
    public string? DeclaredCurrencyCode { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? EstimatedCloseDate { get; set; }
    public DateTime? ActualCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    // Plazo comercial acordado para esta operación (días calendario).
    public int? FinancingDays { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public Client Client { get; set; } = null!;
    public ClientContact? ClientContact { get; set; }
    public Supplier? Supplier { get; set; }
    public AppUser ResponsibleUser { get; set; } = null!;
    public WorkflowStage CurrentStage { get; set; } = null!;
}

public sealed class OperationStageHistory
{
    public long OperationStageHistoryId { get; set; }
    public long OperationId { get; set; }
    public int? FromStageId { get; set; }
    public int ToStageId { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
    public long ChangedBy { get; set; }
}

public sealed class OperationStatusHistory
{
    public long OperationStatusHistoryId { get; set; }
    public long OperationId { get; set; }
    public string StatusDimension { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
    public long ChangedBy { get; set; }
}

public sealed class OperationTask
{
    public long OperationTaskId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public int? WorkflowStageId { get; set; }
    public long? StageTaskTemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskStatus { get; set; } = OperationTaskStatuses.Pending;
    public string Priority { get; set; } = TaskPriorities.Normal;
    public long? AssignedUserId { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? CompletedByUserId { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class OperationEvent
{
    public long OperationEventId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceType { get; set; } = OperationEventSources.Manual;
    public string Visibility { get; set; } = RecordVisibilities.Both;
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
}

public sealed class OperationNote
{
    public long OperationNoteId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public string NoteType { get; set; } = OperationNoteTypes.Internal;
    public string NoteText { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
}



public static class OperationStatuses
{
    public const string Open = "OPEN";
    public const string OnHold = "ON_HOLD";
    public const string InProgress = "IN_PROGRESS";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";
    public const string Closed = "CLOSED";
}

public static class DocumentaryStatuses
{
    public const string Pending = "PENDING";
    public const string Observed = "OBSERVED";
    public const string Complete = "COMPLETE";
}

public static class FinancialStatuses
{
    public const string NoMovements = "NO_MOVEMENTS";
    public const string Funded = "FUNDED";
    public const string InExecution = "IN_EXECUTION";
    public const string PendingCollection = "PENDING_COLLECTION";
    public const string PendingSettlement = "PENDING_SETTLEMENT";
    public const string PendingRefund = "PENDING_REFUND";
    public const string Closed = "CLOSED";
}

public static class OperationTaskStatuses
{
    public const string Pending = "PENDING";
    public const string InProgress = "IN_PROGRESS";
    public const string Blocked = "BLOCKED";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
}

public static class TaskPriorities
{
    public const string Low = "LOW";
    public const string Normal = "NORMAL";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";
}

public static class OperationEventSources
{
    public const string Manual = "MANUAL";
    public const string System = "SYSTEM";
    public const string Import = "IMPORT";
}

public static class OperationEventTypes
{
    public const string OperationCreated = "OPERATION_CREATED";
    public const string StageChanged = "STAGE_CHANGED";
    public const string StatusChanged = "STATUS_CHANGED";
    public const string AssignmentChanged = "ASSIGNMENT_CHANGED";
    public const string TaskUpdated = "TASK_UPDATED";
    public const string Incident = "INCIDENT";
    public const string Manual = "MANUAL";
}

public static class OperationNoteTypes
{
    public const string Internal = "INTERNAL";
    public const string Customer = "CUSTOMER";
    public const string Incident = "INCIDENT";
}


public static class RecordVisibilities
{
    public const string Internal = "INTERNAL";
    public const string Client = "CLIENT";
    public const string Both = "BOTH";
}
