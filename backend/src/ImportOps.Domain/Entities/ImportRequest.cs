namespace ImportOps.Domain.Entities;

public sealed class ImportRequest
{
    public long RequestId { get; set; }
    public long CompanyId { get; set; }
    public string RequestNo { get; set; } = string.Empty;

    public long ClientId { get; set; }
    public long? ClientContactId { get; set; }
    public long? SupplierId { get; set; }

    public long RequestedByUserId { get; set; }
    public long? AssignedUserId { get; set; }

    public DateTime RequestDate { get; set; }
    public string Status { get; set; } = ImportRequestStatuses.New;

    public int? TransportModeId { get; set; }
    public string? IncotermCode { get; set; }
    public string? OriginCountryCode { get; set; }
    public string? DestinationCountryCode { get; set; }
    public long? OriginPortId { get; set; }
    public long? DestinationPortId { get; set; }

    public string MerchandiseDescription { get; set; } = string.Empty;
    public decimal? PackageCount { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? DeclaredValue { get; set; }
    public string? DeclaredCurrencyCode { get; set; }
    public DateTime? DesiredServiceDate { get; set; }

    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public Client Client { get; set; } = null!;
    public ClientContact? ClientContact { get; set; }
    public Supplier? Supplier { get; set; }
    public AppUser RequestedByUser { get; set; } = null!;
    public AppUser? AssignedUser { get; set; }
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
}

public sealed class RequestStatusHistory
{
    public long RequestStatusHistoryId { get; set; }
    public long RequestId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
    public long ChangedBy { get; set; }

    public ImportRequest Request { get; set; } = null!;
    public AppUser ChangedByUser { get; set; } = null!;
}

public static class ImportRequestStatuses
{
    public const string New = "NEW";
    public const string UnderReview = "UNDER_REVIEW";
    public const string Observed = "OBSERVED";
    public const string Quoted = "QUOTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> Editable =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { New, Observed };

    public static readonly IReadOnlySet<string> Terminal =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Approved, Rejected, Cancelled };
}
