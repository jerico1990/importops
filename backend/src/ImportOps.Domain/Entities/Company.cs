using ImportOps.Domain.Common;

namespace ImportOps.Domain.Entities;

public sealed class Company : AuditableEntity
{
    public long CompanyId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "PE";
    public string BaseCurrencyCode { get; set; } = "PEN";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public bool IsActive { get; set; } = true;
}
