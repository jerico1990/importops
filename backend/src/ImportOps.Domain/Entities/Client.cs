using ImportOps.Domain.Common;
namespace ImportOps.Domain.Entities;
public sealed class Client : AuditableEntity
{
    public long ClientId { get; set; }
    public long CompanyId { get; set; }
    public string ClientType { get; set; } = "COMPANY";
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? TaxId { get; set; }
    public string CountryCode { get; set; } = "PE";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? CreditDays { get; set; }
    public string? PreferredCurrencyCode { get; set; }
    public string? Notes { get; set; }
    public long? AccountManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ClientContact> Contacts { get; set; } = new List<ClientContact>();
    public ICollection<ClientAddress> Addresses { get; set; } = new List<ClientAddress>();
}