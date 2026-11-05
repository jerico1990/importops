namespace ImportOps.Domain.Entities;
public sealed class ClientAddress
{
    public long ClientAddressId { get; set; }
    public long ClientId { get; set; }
    public string AddressType { get; set; } = "LEGAL";
    public string AddressLine { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? Department { get; set; }
    public string CountryCode { get; set; } = "PE";
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public Client Client { get; set; } = null!;
}