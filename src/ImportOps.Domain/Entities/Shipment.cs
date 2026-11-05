namespace ImportOps.Domain.Entities;

public sealed class Shipment
{
    public long ShipmentId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }

    public int TransportModeId { get; set; }

    public string? CarrierName { get; set; }
    public string? VesselFlightName { get; set; }
    public string? BookingNo { get; set; }
    public string? MasterDocumentNo { get; set; }
    public string? HouseDocumentNo { get; set; }

    // Campos heredados de V1. Para marítimo el detalle real vive en SHIPMENT_CONTAINER.
    public string? ContainerNo { get; set; }
    public string? SealNo { get; set; }

    public long? OriginPortId { get; set; }
    public long? DestinationPortId { get; set; }

    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? ActualArrivalAt { get; set; }

    public string ShipmentStatus { get; set; } = ShipmentStatuses.Planned;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public ImportOperation Operation { get; set; } = null!;
    public ICollection<ShipmentContainer> Containers { get; set; } = new List<ShipmentContainer>();
}

public sealed class ShipmentContainer
{
    public long ShipmentContainerId { get; set; }
    public long ShipmentId { get; set; }

    public string ContainerNo { get; set; } = string.Empty;
    public string? ContainerType { get; set; }
    public string? SealNo { get; set; }

    public decimal? PackageCount { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    public Shipment Shipment { get; set; } = null!;
}

public static class ShipmentStatuses
{
    public const string Planned = "PLANNED";
    public const string Booked = "BOOKED";
    public const string Departed = "DEPARTED";
    public const string InTransit = "IN_TRANSIT";
    public const string Arrived = "ARRIVED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> Final =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Arrived, Cancelled
        };
}
