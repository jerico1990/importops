using ImportOps.Application.Common;

namespace ImportOps.Application.Catalogs;

public interface ICatalogAdministrationService
{
    Task<PagedResult<CatalogAdminItemDto>> SearchAsync(string catalog, CatalogAdminQuery query, CancellationToken ct);
    Task<CatalogAdminItemDto> CreateAsync(string catalog, CatalogAdminSaveRequest request, CancellationToken ct);
    Task<CatalogAdminItemDto> UpdateAsync(string catalog, string id, CatalogAdminSaveRequest request, CancellationToken ct);
    Task SetActiveAsync(string catalog, string id, bool active, CancellationToken ct);
    Task<CatalogDeleteCheckDto> CheckDeleteAsync(string catalog, string id, CancellationToken ct);
    Task DeleteAsync(string catalog, string id, CancellationToken ct);
}
