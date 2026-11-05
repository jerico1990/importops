namespace ImportOps.Application.Catalogs;

public sealed record CatalogAdminQuery(int Page = 1, int PageSize = 20, string? Search = null, string Status = "ALL", string? CountryCode = null, string? PortType = null);

public sealed record CatalogAdminItemDto(
    string Id, string Code, string Name, bool IsActive,
    string? Description = null, string? Symbol = null, int? DecimalPlaces = null,
    string? CountryCode = null, string? CountryName = null, string? CityName = null, string? PortType = null,
    string? Category = null, bool? Flag = null)
{
    public int? ServiceCategoryId { get; init; }
    public int? ExpenseCategoryId { get; init; }
    public decimal? DefaultTaxRate { get; init; }
    public bool? IsTaxable { get; init; }
    public bool? IsRebillableDefault { get; init; }
    public int? DisplayOrder { get; init; }
    public string? GroupCode { get; init; }
    public string? GroupName { get; init; }
}


public sealed record CatalogAdminSaveRequest(
    string? Code, string? Name, string? Description = null, string? Symbol = null, int? DecimalPlaces = null,
    string? CountryCode = null, string? CityName = null, string? PortType = null, string? Category = null, bool? Flag = null,
    int? ServiceCategoryId = null, int? ExpenseCategoryId = null, decimal? DefaultTaxRate = null, bool? IsTaxable = null,
    bool? IsRebillableDefault = null, int? DisplayOrder = null, string? GroupCode = null, string? GroupName = null);

public sealed record CatalogUsageDto(string Area, int Count);
public sealed record CatalogDeleteCheckDto(bool CanDelete, IReadOnlyCollection<CatalogUsageDto> Usages, string Message);
