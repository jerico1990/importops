using ImportOps.Application.Common;

namespace ImportOps.Application.Clients;

public interface IClientService
{
    Task<PagedResult<ClientListItemDto>> GetAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<ClientDetailDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<ClientDetailDto> CreateAsync(CreateClientRequest request, CancellationToken ct);
    Task<ClientDetailDto?> UpdateAsync(long id, UpdateClientRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(long id, CancellationToken ct);

    Task<ClientContactDto?> AddContactAsync(long clientId, CreateClientContactRequest request, CancellationToken ct);
    Task<ClientContactDto?> UpdateContactAsync(long clientId, long contactId, UpdateClientContactRequest request, CancellationToken ct);
    Task<bool> DeleteContactAsync(long clientId, long contactId, CancellationToken ct);

    Task<ClientAddressDto?> AddAddressAsync(long clientId, CreateClientAddressRequest request, CancellationToken ct);
    Task<ClientAddressDto?> UpdateAddressAsync(long clientId, long addressId, UpdateClientAddressRequest request, CancellationToken ct);
    Task<bool> DeleteAddressAsync(long clientId, long addressId, CancellationToken ct);
}
