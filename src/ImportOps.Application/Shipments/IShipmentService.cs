using ImportOps.Application.Common;

namespace ImportOps.Application.Shipments;

public interface IShipmentService
{
    Task<PagedResult<ShipmentListItemDto>> GetAsync(ShipmentQuery query, CancellationToken ct);
    Task<IReadOnlyCollection<ShipmentListItemDto>> GetByOperationAsync(long operationId, CancellationToken ct);
    Task<ShipmentDetailDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<ShipmentAvailableActionDto>> GetAvailableActionsAsync(long id, CancellationToken ct);

    Task<ShipmentDetailDto> CreateAsync(long operationId, CreateShipmentRequest request, CancellationToken ct);
    Task<ShipmentDetailDto?> UpdateAsync(long id, UpdateShipmentRequest request, CancellationToken ct);
    Task<bool> DeletePlannedAsync(long id, string rowVersion, CancellationToken ct);

    Task<ShipmentContainerDto?> AddContainerAsync(long shipmentId, UpsertShipmentContainerRequest request, CancellationToken ct);
    Task<ShipmentContainerDto?> UpdateContainerAsync(long shipmentId, long containerId, UpsertShipmentContainerRequest request, CancellationToken ct);
    Task<bool> DeleteContainerAsync(long shipmentId, long containerId, CancellationToken ct);

    Task<ShipmentDetailDto?> BookAsync(long id, string? comment, CancellationToken ct);
    Task<ShipmentDetailDto?> DepartAsync(long id, DateTime actualDepartureAt, string? comment, CancellationToken ct);
    Task<ShipmentDetailDto?> MarkInTransitAsync(long id, string? comment, CancellationToken ct);
    Task<ShipmentDetailDto?> ArriveAsync(long id, DateTime actualArrivalAt, string? comment, CancellationToken ct);
    Task<ShipmentDetailDto?> CancelAsync(long id, string reason, CancellationToken ct);
}
