namespace ImportOps.Domain.Entities;

public sealed class DocumentRecord
{
    public long DocumentId { get; set; }
    public long CompanyId { get; set; }
    public long? RequestId { get; set; }
    public long? QuotationId { get; set; }
    public long? OperationId { get; set; }

    public int DocumentTypeId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }

    public string StorageProvider { get; set; } = "LOCAL";
    public string StorageKey { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }

    public int VersionNo { get; set; } = 1;
    public string DocumentStatus { get; set; } = DocumentStatuses.PendingReview;
    public string Visibility { get; set; } = RecordVisibilities.Both;

    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Notes { get; set; }

    public DateTime UploadedAt { get; set; }
    public long UploadedBy { get; set; }

    public long? ReplacedDocumentId { get; set; }

    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class DocumentObservation
{
    public long DocumentObservationId { get; set; }
    public long DocumentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = DocumentObservationStatuses.Open;
    public DateTime ObservedAt { get; set; }
    public long ObservedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public long? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
    public long? OperationTaskId { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public static class DocumentStatuses
{
    public const string PendingReview = "PENDING_REVIEW";
    public const string Valid = "VALID";
    public const string Observed = "OBSERVED";
    public const string Replaced = "REPLACED";
    public const string Expired = "EXPIRED";
    public const string Void = "VOID";
}

public static class DocumentObservationStatuses
{
    public const string Open = "OPEN";
    public const string Resolved = "RESOLVED";
    public const string Cancelled = "CANCELLED";
}
