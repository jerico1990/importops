using ImportOps.Domain.Common;

namespace ImportOps.Domain.Entities;

public sealed class Supplier : AuditableEntity
{
    public long SupplierId { get; set; }
    public long CompanyId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? CountryCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
}
