namespace ImportOps.Application.Documents;

public sealed class DocumentQuery
{
    public long? RequestId { get; init; }
    public long? QuotationId { get; init; }
    public long? OperationId { get; init; }
    public int? DocumentTypeId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record DocumentDto(
    long DocumentId,
    long? RequestId,
    long? QuotationId,
    long? OperationId,
    int DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeName,
    string OriginalFileName,
    string? MimeType,
    long? FileSizeBytes,
    string? Sha256Hash,
    int VersionNo,
    string DocumentStatus,
    string Visibility,
    DateTime? DocumentDate,
    DateTime? ExpirationDate,
    string? Notes,
    DateTime UploadedAt,
    long UploadedBy,
    long? ReplacedDocumentId,
    string RowVersion);

public sealed record DocumentObservationDto(
    long DocumentObservationId,
    long DocumentId,
    string Description,
    string Status,
    DateTime ObservedAt,
    long ObservedBy,
    DateTime? ResolvedAt,
    long? ResolvedBy,
    string? ResolutionNote,
    long? OperationTaskId,
    string RowVersion);

public sealed record StageDocumentRequirementDto(
    int WorkflowStageId,
    string StageCode,
    string StageName,
    int DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeName,
    bool IsMandatory,
    bool IsSatisfied,
    long? ValidDocumentId);

public sealed record OperationDocumentStatusDto(
    long OperationId,
    string DocumentaryStatus,
    int RequiredCount,
    int ValidCount,
    int MissingCount,
    int ObservedCount,
    IReadOnlyCollection<StageDocumentRequirementDto> Requirements);

public sealed class UploadDocumentRequest
{
    public long? RequestId { get; init; }
    public long? QuotationId { get; init; }
    public long? OperationId { get; init; }
    public int DocumentTypeId { get; init; }
    public DateTime? DocumentDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public string? Notes { get; init; }
    public string Visibility { get; init; } = "BOTH";
}

public sealed class ValidateDocumentRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public sealed class ObserveDocumentRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string Observation { get; init; } = string.Empty;
}

public sealed class ResolveDocumentObservationRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string ResolutionNote { get; init; } = string.Empty;
}

public sealed class VoidDocumentRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
