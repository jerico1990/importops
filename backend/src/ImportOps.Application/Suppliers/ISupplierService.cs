using ImportOps.Application.Common;

namespace ImportOps.Application.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierListItemDto>> GetAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct);
    Task<SupplierDetailDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<SupplierDetailDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct);
    Task<SupplierDetailDto?> UpdateAsync(long id, UpdateSupplierRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);
    Task<SupplierContactDto?> AddContactAsync(long supplierId, UpsertSupplierContactRequest request, CancellationToken ct);
    Task<SupplierContactDto?> UpdateContactAsync(long supplierId, long contactId, UpsertSupplierContactRequest request, CancellationToken ct);
    Task<bool> DeleteContactAsync(long supplierId, long contactId, CancellationToken ct);
}
