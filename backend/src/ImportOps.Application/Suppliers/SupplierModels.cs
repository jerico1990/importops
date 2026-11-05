namespace ImportOps.Application.Suppliers;

public sealed record SupplierListItemDto(
    long SupplierId, string LegalName, string? TaxId, string? CountryCode,
    string? Email, string? Phone, bool IsActive);

public sealed record SupplierContactDto(
    long SupplierContactId, string FullName, string? Email, string? Phone,
    string? PositionName, bool IsPrimary, bool IsActive);

public sealed record SupplierDetailDto(
    long SupplierId, string LegalName, string? TaxId, string? CountryCode,
    string? Email, string? Phone, string? AddressLine, string? Notes, bool IsActive, string RowVersion,
    IReadOnlyCollection<SupplierContactDto> Contacts);

public class CreateSupplierRequest
{
    public string LegalName { get; init; } = string.Empty;
    public string? TaxId { get; init; }
    public string? CountryCode { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateSupplierRequest : CreateSupplierRequest
{
    public bool IsActive { get; init; } = true;
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertSupplierContactRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? PositionName { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; } = true;
}
