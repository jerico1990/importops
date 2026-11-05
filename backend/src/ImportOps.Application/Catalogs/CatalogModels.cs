namespace ImportOps.Application.Catalogs;

public sealed record CurrencyDto(string CurrencyCode, string CurrencyName, string? Symbol, byte DecimalPlaces);
public sealed record CountryDto(string CountryCode, string CountryName, string? Iso3Code);
public sealed record PortDto(long PortId, string CountryCode, string? PortCode, string PortName, string? CityName, string PortType);
public sealed record TransportModeDto(int TransportModeId, string Code, string Name);
public sealed record IncotermDto(string IncotermCode, string Description);
public sealed record PaymentMethodDto(int PaymentMethodId, string Code, string Name);
public sealed record DocumentTypeDto(int DocumentTypeId, string Code, string Name, string Category, bool IsRequiredDefault);
public sealed record ExpenseCategoryDto(int ExpenseCategoryId, string Code, string Name, bool IsRebillableDefault);
public sealed record ServiceCategoryDto(int ServiceCategoryId, string Code, string Name);
public sealed record WorkflowStageDto(int WorkflowStageId, string Code, string Name, int DisplayOrder, bool IsFinal);

public sealed record AppUserCatalogDto(long UserId, string Username, string? FirstName, string? LastName, string? Email);

public sealed record ImportRequestStatusDto(string Code, string Name, bool IsEditable, bool IsTerminal);

public sealed record QuotationStatusDto(string Code, string Name, bool IsTerminal);
public sealed record QuotationItemTypeDto(string Code, string Name, string? CategorySource);

public sealed record CodeNameDto(string Code, string Name);
public sealed record IdCodeNameDto(int Id, string Code, string Name);
