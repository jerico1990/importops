namespace ImportOps.Domain.Entities;

public sealed class CustomsProcess
{
    public long CustomsProcessId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long ShipmentId { get; set; }

    public string? CustomsRegime { get; set; } // legacy
    public int? CustomsRegimeId { get; set; }
    public string? DeclarationNo { get; set; }
    public DateTime? DeclarationDate { get; set; }
    public string? CustomsOffice { get; set; } // legacy
    public int? CustomsOfficeId { get; set; }

    public string? CustomsChannel { get; set; }
    public string CustomsStatus { get; set; } = CustomsStatuses.Pending;

    public decimal? DutiesAmount { get; set; }
    public string? DutiesCurrencyCode { get; set; }
    public decimal? DutiesExchangeRate { get; set; }
    public decimal? DutiesBaseAmount { get; set; }

    public DateTime? ReleaseDate { get; set; }
    public string? Observation { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public ImportOperation Operation { get; set; } = null!;
    public CustomsRegime? CustomsRegimeCatalog { get; set; }
    public CustomsOfficeCatalog? CustomsOfficeCatalog { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public ICollection<CustomsObservation> Observations { get; set; } = new List<CustomsObservation>();
    public ICollection<CustomsTax> Taxes { get; set; } = new List<CustomsTax>();
    public ICollection<CustomsStatusHistory> StatusHistory { get; set; } = new List<CustomsStatusHistory>();
}

public sealed class CustomsObservation
{
    public long CustomsObservationId { get; set; }
    public long CustomsProcessId { get; set; }

    public string ObservationType { get; set; } = CustomsObservationTypes.Other;
    public string Severity { get; set; } = CustomsObservationSeverities.Normal;
    public string Description { get; set; } = string.Empty;
    public string ObservationStatus { get; set; } = CustomsObservationStatuses.Open;

    public DateTime ObservedAt { get; set; }
    public long ObservedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public long? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }

    public long? OperationTaskId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public CustomsProcess CustomsProcess { get; set; } = null!;
}

public sealed class CustomsTax
{
    public long CustomsTaxId { get; set; }
    public long CustomsProcessId { get; set; }

    public string TaxType { get; set; } = string.Empty; // legacy snapshot; la FK es la fuente de verdad
    public int? CustomsTaxConceptId { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }

    public string PaymentStatus { get; set; } = CustomsTaxPaymentStatuses.Pending;
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public CustomsProcess CustomsProcess { get; set; } = null!;
    public CustomsTaxConcept? CustomsTaxConcept { get; set; }
}

public sealed class CustomsStatusHistory
{
    public long CustomsStatusHistoryId { get; set; }
    public long CustomsProcessId { get; set; }

    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }

    public DateTime ChangedAt { get; set; }
    public long ChangedBy { get; set; }

    public CustomsProcess CustomsProcess { get; set; } = null!;
}

public static class CustomsStatuses
{
    public const string Pending = "PENDING";
    public const string InReview = "IN_REVIEW";
    public const string Observed = "OBSERVED";
    public const string Released = "RELEASED";
    public const string Cancelled = "CANCELLED";
}

public static class CustomsChannels
{
    public const string Green = "GREEN";
    public const string Yellow = "YELLOW";
    public const string Red = "RED";
    public const string Other = "OTHER";
}

public static class CustomsObservationTypes
{
    public const string Documentary = "DOCUMENTARY";
    public const string Valuation = "VALUATION";
    public const string Classification = "CLASSIFICATION";
    public const string Inspection = "INSPECTION";
    public const string AuthorityRequirement = "AUTHORITY_REQUIREMENT";
    public const string Other = "OTHER";
}

public static class CustomsObservationSeverities
{
    public const string Low = "LOW";
    public const string Normal = "NORMAL";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";
}

public static class CustomsObservationStatuses
{
    public const string Open = "OPEN";
    public const string Resolved = "RESOLVED";
    public const string Cancelled = "CANCELLED";
}

public static class CustomsTaxPaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Paid = "PAID";
    public const string Exempt = "EXEMPT";
    public const string Void = "VOID";
}
