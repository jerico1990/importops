using ImportOps.Application.Common;

namespace ImportOps.Application.Customs;

public interface ICustomsService
{
    Task<PagedResult<CustomsListItemDto>> GetAsync(CustomsQuery query, CancellationToken ct);
    Task<IReadOnlyCollection<CustomsListItemDto>> GetByOperationAsync(long operationId, CancellationToken ct);
    Task<CustomsDetailDto?> GetByIdAsync(long id, CancellationToken ct);

    Task<CustomsDetailDto> CreateAsync(long operationId, CreateCustomsProcessRequest request, CancellationToken ct);
    Task<CustomsDetailDto?> UpdateAsync(long id, UpdateCustomsProcessRequest request, CancellationToken ct);

    Task<CustomsDetailDto?> StartReviewAsync(long id, CustomsActionRequest request, CancellationToken ct);
    Task<CustomsDetailDto?> AssignChannelAsync(long id, AssignCustomsChannelRequest request, CancellationToken ct);

    Task<CustomsObservationDto?> AddObservationAsync(long id, CreateCustomsObservationRequest request, CancellationToken ct);
    Task<CustomsObservationDto?> ResolveObservationAsync(long id, long observationId, ResolveCustomsObservationRequest request, CancellationToken ct);

    Task<CustomsTaxDto?> AddTaxAsync(long id, UpsertCustomsTaxRequest request, CancellationToken ct);
    Task<CustomsTaxDto?> UpdateTaxAsync(long id, long taxId, UpsertCustomsTaxRequest request, CancellationToken ct);
    Task<CustomsTaxDto?> ChangeTaxStatusAsync(long id, long taxId, CustomsTaxStatusRequest request, CancellationToken ct);

    Task<CustomsDetailDto?> ReleaseAsync(long id, ReleaseCustomsRequest request, CancellationToken ct);
    Task<CustomsDetailDto?> CancelAsync(long id, CancelCustomsRequest request, CancellationToken ct);
}
