namespace ImportOps.Application.Operations;

public sealed class OperationQuery
{
    public string? Search { get; init; }
    public string? OperationalStatus { get; init; }
    public string? DocumentaryStatus { get; init; }
    public string? FinancialStatus { get; init; }
    public int? StageId { get; init; }
    public long? ClientId { get; init; }
    public long? QuotationId { get; init; }
    public long? ResponsibleUserId { get; init; }
    public DateTime? OpenedFrom { get; init; }
    public DateTime? OpenedTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CreateOperationFromQuotationRequest
{
    public long? ResponsibleUserId { get; init; }
    public DateTime? EstimatedCloseDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateOperationRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public long? ClientContactId { get; init; }
    public long? SupplierId { get; init; }
    public long ResponsibleUserId { get; init; }
    public int? TransportModeId { get; init; }
    public string? IncotermCode { get; init; }
    public string? OriginCountryCode { get; init; }
    public string? DestinationCountryCode { get; init; }
    public long? OriginPortId { get; init; }
    public long? DestinationPortId { get; init; }
    public string MerchandiseDescription { get; init; } = string.Empty;
    public decimal? PackageCount { get; init; }
    public decimal? GrossWeightKg { get; init; }
    public decimal? NetWeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public decimal? DeclaredValue { get; init; }
    public string? DeclaredCurrencyCode { get; init; }
    public DateTime? EstimatedCloseDate { get; init; }
    public string? Notes { get; init; }
}

public sealed record OperationListItemDto(
    long OperationId,
    string OperationNo,
    long ClientId,
    string ClientName,
    string StageCode,
    string StageName,
    string OperationalStatus,
    string DocumentaryStatus,
    string? FinancialStatus,
    long ResponsibleUserId,
    string ResponsibleName,
    DateTime OpenedAt,
    DateTime? EstimatedCloseDate);

public sealed record OperationStageHistoryDto(
    long OperationStageHistoryId,
    int? FromStageId,
    string? FromStageName,
    int ToStageId,
    string ToStageName,
    string? Comment,
    DateTime ChangedAt,
    long ChangedBy,
    string ChangedByName);

public sealed record OperationStatusHistoryDto(
    long OperationStatusHistoryId,
    string StatusDimension,
    string? OldStatus,
    string NewStatus,
    string? Comment,
    DateTime ChangedAt,
    long ChangedBy,
    string ChangedByName);

public sealed record OperationTaskDto(
    long OperationTaskId,
    int? WorkflowStageId,
    string? StageName,
    string Title,
    string? Description,
    string TaskStatus,
    string Priority,
    long? AssignedUserId,
    string? AssignedUserName,
    DateTime? DueAt,
    DateTime? CompletedAt,
    long? CompletedByUserId,
    bool IsMandatory,
    bool IsFromTemplate,
    IReadOnlyCollection<string> AllowedNextStatuses);

public sealed record OperationEventDto(
    long OperationEventId,
    string EventType,
    DateTime EventDate,
    string Title,
    string? Description,
    string SourceType,
    string Visibility,
    long CreatedBy,
    string CreatedByName);

public sealed record OperationNoteDto(
    long OperationNoteId,
    string NoteType,
    string NoteText,
    bool IsPinned,
    DateTime CreatedAt,
    long CreatedBy,
    string CreatedByName);

public sealed record OperationSourceSnapshotDto(
    string? ClientName,
    string? ClientContactName,
    string? SupplierName,
    string? QuotationNo,
    int? QuotationVersion,
    string? QuotationCurrency,
    decimal? QuotationTotal);

public sealed record OperationDetailDto(
    long OperationId,
    string OperationNo,
    long? RequestId,
    string? RequestNo,
    long? QuotationId,
    string? QuotationNo,
    int? QuotationVersion,
    long ClientId,
    string ClientName,
    long? ClientContactId,
    string? ClientContactName,
    long? SupplierId,
    string? SupplierName,
    long ResponsibleUserId,
    string ResponsibleName,
    int CurrentStageId,
    string CurrentStageCode,
    string CurrentStageName,
    string OperationalStatus,
    string DocumentaryStatus,
    string? FinancialStatus,
    int? TransportModeId,
    string? TransportMode,
    string? IncotermCode,
    string? OriginCountryCode,
    string? OriginCountry,
    long? OriginPortId,
    string? OriginPort,
    string? DestinationCountryCode,
    string? DestinationCountry,
    long? DestinationPortId,
    string? DestinationPort,
    string MerchandiseDescription,
    decimal? PackageCount,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    decimal? VolumeM3,
    decimal? DeclaredValue,
    string? DeclaredCurrencyCode,
    string? QuotationCurrencySnapshot,
    decimal? QuotationTotalSnapshot,
    DateTime OpenedAt,
    DateTime? EstimatedCloseDate,
    DateTime? ActualCloseDate,
    DateTime? ClosedAt,
    string? CancellationReason,
    string? Notes,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string RowVersion,
    OperationSourceSnapshotDto SourceSnapshot);

public sealed class ChangeStageRequest
{
    public int ToStageId { get; init; }
    public string? Comment { get; init; }
}

public sealed class OperationActionRequest
{
    public string Comment { get; init; } = string.Empty;
}

public sealed class AssignOperationRequest
{
    public long ResponsibleUserId { get; init; }
    public string? Comment { get; init; }
}

public sealed class CreateOperationTaskRequest
{
    public int? WorkflowStageId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Priority { get; init; } = "NORMAL";
    public long? AssignedUserId { get; init; }
    public DateTime? DueAt { get; init; }
    public bool IsMandatory { get; init; }
}

public sealed class UpdateOperationTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Priority { get; init; } = "NORMAL";
    public long? AssignedUserId { get; init; }
    public DateTime? DueAt { get; init; }
    public bool IsMandatory { get; init; }
}

public sealed class ChangeTaskStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public sealed class CreateOperationEventRequest
{
    public string EventType { get; init; } = "MANUAL";
    public DateTime? EventDate { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = "BOTH";
}

public sealed class CreateOperationNoteRequest
{
    public string NoteType { get; init; } = "INTERNAL";
    public string NoteText { get; init; } = string.Empty;
    public bool IsPinned { get; init; }
}

public sealed record OperationAvailableStageDto(
    int WorkflowStageId,
    string Code,
    string Name,
    int DisplayOrder,
    bool RequiresComment);
