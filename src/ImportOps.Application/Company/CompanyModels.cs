namespace ImportOps.Application.Company;

public sealed record CompanyDto(
    long CompanyId,
    string LegalName,
    string? TradeName,
    string TaxId,
    string CountryCode,
    string BaseCurrencyCode,
    string? Email,
    string? Phone,
    string? AddressLine,
    bool IsActive);

public sealed class UpdateCompanyRequest
{
    public string LegalName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string TaxId { get; init; } = string.Empty;
    public string CountryCode { get; init; } = "PE";
    public string BaseCurrencyCode { get; init; } = "PEN";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine { get; init; }
    public bool IsActive { get; init; } = true;
}
