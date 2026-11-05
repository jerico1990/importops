namespace ImportOps.Domain.Entities;

public sealed class Quotation
{
    public long QuotationId { get; set; }
    public long CompanyId { get; set; }
    public string QuotationNo { get; set; } = string.Empty;
    public long RequestId { get; set; }
    public long ClientId { get; set; }

    public int VersionNo { get; set; } = 1;
    public string Status { get; set; } = QuotationStatuses.Draft;
    public string CurrencyCode { get; set; } = "USD";

    public DateTime IssueDate { get; set; }
    public DateTime? ValidUntil { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Terms { get; set; }
    public string? InternalNotes { get; set; }

    public string? ReferenceFile { get; set; }
    public string? ContentDescription { get; set; }
    public string? CustomsRegime { get; set; } // legacy V1.17, solo compatibilidad de migración
    public string? CustomsOffice { get; set; } // legacy V1.17, solo compatibilidad de migración
    public int? CustomsRegimeId { get; set; }
    public int? CustomsOfficeId { get; set; }
    public DateTime? EstimatedArrivalDate { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? ExwAmount { get; set; }
    public decimal? FreightAmount { get; set; }
    public decimal? InsuranceAmount { get; set; }
    public decimal? CustomsValue { get; set; }
    public decimal? NetWeightKg { get; set; }
    public decimal? PackageCount { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? CustomsDutiesAmount { get; set; }
    public decimal? ExpenseAmount { get; set; }
    public decimal? ExpenseTaxAmount { get; set; }
    public string? BankInstructions { get; set; }
    public string? PublicNotes { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public ImportRequest Request { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public CustomsRegime? CustomsRegimeCatalog { get; set; }
    public CustomsOfficeCatalog? CustomsOfficeCatalog { get; set; }
    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    public ICollection<QuotationSupplier> Suppliers { get; set; } = new List<QuotationSupplier>();
    public ICollection<QuotationCargoItem> CargoItems { get; set; } = new List<QuotationCargoItem>();
    public ICollection<QuotationCustomsTax> CustomsTaxes { get; set; } = new List<QuotationCustomsTax>();
    public ICollection<QuotationStatusHistory> StatusHistory { get; set; } = new List<QuotationStatusHistory>();
}

public sealed class QuotationItem
{
    public long QuotationItemId { get; set; }
    public long QuotationId { get; set; }
    public int LineNo { get; set; }
    public string ItemType { get; set; } = QuotationItemTypes.Service;
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public int? ExpenseCategoryId { get; set; }
    public int? ServiceCategoryId { get; set; }
    public bool IsRebillable { get; set; }
    public string? Notes { get; set; }
    public long? QuotationConceptId { get; set; }
    public bool? IsTaxable { get; set; }
    public int? DisplayOrder { get; set; }

    public Quotation Quotation { get; set; } = null!;
}

public sealed class QuotationStatusHistory
{
    public long QuotationStatusHistoryId { get; set; }
    public long QuotationId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
    public long ChangedBy { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public AppUser ChangedByUser { get; set; } = null!;
}

public static class QuotationStatuses
{
    public const string Draft = "DRAFT";
    public const string Sent = "SENT";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> Closed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Approved, Rejected, Expired, Cancelled
        };
}

public static class QuotationItemTypes
{
    public const string Service = "SERVICE";
    public const string EstimatedCost = "ESTIMATED_COST";
    public const string Other = "OTHER";
}
