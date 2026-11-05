namespace ImportOps.Application.Clients;

public sealed record ClientListItemDto(long ClientId, string ClientType, string LegalName, string? TradeName, string? TaxId, string? Email, string? Phone, bool IsActive);
public sealed record ClientContactDto(long ClientContactId, string FullName, string? PositionName, string? Email, string? Phone, string? Mobile, bool IsPrimary, bool IsActive);
public sealed record ClientAddressDto(long ClientAddressId, string AddressType, string AddressLine, string? District, string? Province, string? Department, string CountryCode, bool IsPrimary, bool IsActive);

public sealed record ClientDetailDto(
    long ClientId, string ClientType, string LegalName, string? TradeName, string? TaxId,
    string CountryCode, string? Email, string? Phone, int? CreditDays, string? PreferredCurrencyCode,
    string? Notes, long? AccountManagerUserId, bool IsActive, string RowVersion,
    IReadOnlyCollection<ClientContactDto> Contacts, IReadOnlyCollection<ClientAddressDto> Addresses);

public class CreateClientRequest
{
    public string ClientType { get; init; } = "COMPANY";
    public string LegalName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string? TaxId { get; init; }
    public string CountryCode { get; init; } = "PE";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int? CreditDays { get; init; }
    public string? PreferredCurrencyCode { get; init; }
    public string? Notes { get; init; }
    public long? AccountManagerUserId { get; init; }
}

public sealed class UpdateClientRequest : CreateClientRequest
{
    public bool IsActive { get; init; } = true;
    public string RowVersion { get; init; } = string.Empty;
}

public class CreateClientContactRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? PositionName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class UpdateClientContactRequest : CreateClientContactRequest
{
    public bool IsActive { get; init; } = true;
}

public class CreateClientAddressRequest
{
    public string AddressType { get; init; } = "LEGAL";
    public string AddressLine { get; init; } = string.Empty;
    public string? District { get; init; }
    public string? Province { get; init; }
    public string? Department { get; init; }
    public string CountryCode { get; init; } = "PE";
    public bool IsPrimary { get; init; }
}

public sealed class UpdateClientAddressRequest : CreateClientAddressRequest
{
    public bool IsActive { get; init; } = true;
}
