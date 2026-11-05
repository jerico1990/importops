namespace ImportOps.Domain.Entities;

public sealed class QuotationSupplier
{
    public long QuotationSupplierId { get; set; }
    public long QuotationId { get; set; }
    public long SupplierId { get; set; }
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
    public Quotation Quotation { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<QuotationCargoItem> CargoItems { get; set; } = new List<QuotationCargoItem>();
}

public sealed class QuotationCargoItem
{
    public long QuotationCargoItemId { get; set; }
    public long QuotationId { get; set; }
    public long? QuotationSupplierId { get; set; }
    public int LineNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? NetWeightKg { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? DeclaredValue { get; set; }
    public string? CurrencyCode { get; set; }
    public string? OriginCountryCode { get; set; }
    public string? Notes { get; set; }
    public Quotation Quotation { get; set; } = null!;
    public QuotationSupplier? QuotationSupplier { get; set; }
}

public sealed class QuotationCustomsTax
{
    public long QuotationCustomsTaxId { get; set; }
    public long QuotationId { get; set; }
    public string TaxCode { get; set; } = string.Empty; // legacy V1.17
    public int? CustomsTaxConceptId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? RateApplied { get; set; }
    public decimal? CalculationBase { get; set; }
    public bool IsCalculated { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
    public Quotation Quotation { get; set; } = null!;
    public CustomsTaxConcept? CustomsTaxConcept { get; set; }
}

public sealed class QuotationConcept
{
    public long QuotationConceptId { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int? ServiceCategoryId { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public decimal DefaultTaxRate { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRebillableDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public sealed class CompanyBankAccount
{
    public long CompanyBankAccountId { get; set; }
    public long CompanyId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Cci { get; set; }
    public string? AccountHolder { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowOnQuotation { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
