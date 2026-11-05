namespace ImportOps.Application.Shipments;

public sealed class ShipmentQuery
{
    public long? OperationId { get; init; }
    public string? Status { get; init; }
    public int? TransportModeId { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record ShipmentContainerDto(
    long ShipmentContainerId,
    string ContainerNo,
    string? ContainerType,
    string? SealNo,
    decimal? PackageCount,
    decimal? GrossWeightKg,
    decimal? VolumeM3,
    string? Notes,
    bool IsActive);

public sealed record ShipmentListItemDto(
    long ShipmentId,
    long OperationId,
    string OperationNo,
    string ShipmentStatus,
    int TransportModeId,
    string TransportMode,
    string? CarrierName,
    string? BookingNo,
    string? MasterDocumentNo,
    string? HouseDocumentNo,
    long? OriginPortId,
    string? OriginPort,
    long? DestinationPortId,
    string? DestinationPort,
    DateTime? Etd,
    DateTime? Eta,
    DateTime? ActualDepartureAt,
    DateTime? ActualArrivalAt,
    int ContainerCount);

public sealed record ShipmentDetailDto(
    long ShipmentId,
    long OperationId,
    string OperationNo,
    string ShipmentStatus,
    int TransportModeId,
    string TransportMode,
    string? CarrierName,
    string? VesselFlightName,
    string? BookingNo,
    string? MasterDocumentNo,
    string? HouseDocumentNo,
    long? OriginPortId,
    string? OriginPort,
    long? DestinationPortId,
    string? DestinationPort,
    DateTime? Etd,
    DateTime? Eta,
    DateTime? ActualDepartureAt,
    DateTime? ActualArrivalAt,
    string? Notes,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string RowVersion,
    IReadOnlyCollection<ShipmentContainerDto> Containers);

public class CreateShipmentRequest
{
    public int? TransportModeId { get; init; }
    public string? CarrierName { get; init; }
    public string? VesselFlightName { get; init; }
    public string? BookingNo { get; init; }
    public string? MasterDocumentNo { get; init; }
    public string? HouseDocumentNo { get; init; }
    public long? OriginPortId { get; init; }
    public long? DestinationPortId { get; init; }
    public DateTime? Etd { get; init; }
    public DateTime? Eta { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateShipmentRequest : CreateShipmentRequest
{
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertShipmentContainerRequest
{
    public string ContainerNo { get; init; } = string.Empty;
    public string? ContainerType { get; init; }
    public string? SealNo { get; init; }
    public decimal? PackageCount { get; init; }
    public decimal? GrossWeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class ShipmentStatusActionRequest
{
    public DateTime? EventAt { get; init; }
    public string? Comment { get; init; }
}

public sealed class CancelShipmentRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record ShipmentAvailableActionDto(
    string Code,
    string Name,
    bool RequiresEventAt,
    bool RequiresComment,
    bool IsEnabled,
    string? BlockedReason);
