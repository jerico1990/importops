namespace ImportOps.Application.ImportRequests;

public sealed class ImportRequestQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public long? ClientId { get; init; }
    public long? SupplierId { get; init; }
    public long? AssignedUserId { get; init; }
    public int? TransportModeId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record ImportRequestListItemDto(
    long RequestId,
    string RequestNo,
    DateTime RequestDate,
    string Status,
    long ClientId,
    string ClientName,
    long? SupplierId,
    string? SupplierName,
    long RequestedByUserId,
    string RequestedBy,
    long? AssignedUserId,
    string? AssignedTo,
    int? TransportModeId,
    string? TransportMode,
    string MerchandiseDescription,
    DateTime CreatedAt);

public sealed record RequestStatusHistoryDto(
    long RequestStatusHistoryId,
    string? OldStatus,
    string NewStatus,
    string? Comment,
    DateTime ChangedAt,
    long ChangedBy,
    string ChangedByName);

public sealed record ImportRequestDetailDto(
    long RequestId,
    string RequestNo,
    DateTime RequestDate,
    string Status,
    long ClientId,
    string ClientName,
    long? ClientContactId,
    string? ClientContactName,
    long? SupplierId,
    string? SupplierName,
    long RequestedByUserId,
    string RequestedBy,
    long? AssignedUserId,
    string? AssignedTo,
    int? TransportModeId,
    string? TransportMode,
    string? IncotermCode,
    string? OriginCountryCode,
    string? OriginCountry,
    long? OriginPortId,
    string? OriginPort,
    string? DestinationCountryCode,
    string? DestinationCountry,
    long? DestinationPortId,
    string? DestinationPort,
    string MerchandiseDescription,
    decimal? PackageCount,
    decimal? GrossWeightKg,
    decimal? VolumeM3,
    decimal? DeclaredValue,
    string? DeclaredCurrencyCode,
    DateTime? DesiredServiceDate,
    string? CustomerNotes,
    string? InternalNotes,
    string? RejectionReason,
    DateTime? ApprovedAt,
    long? ApprovedByUserId,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string RowVersion,
    IReadOnlyCollection<RequestStatusHistoryDto> History);

public class CreateImportRequestRequest
{
    public long ClientId { get; init; }
    public long? ClientContactId { get; init; }
    public long? SupplierId { get; init; }
    public long? AssignedUserId { get; init; }

    public DateTime? RequestDate { get; init; }

    public int? TransportModeId { get; init; }
    public string? IncotermCode { get; init; }
    public string? OriginCountryCode { get; init; }
    public string? DestinationCountryCode { get; init; }
    public long? OriginPortId { get; init; }
    public long? DestinationPortId { get; init; }

    public string MerchandiseDescription { get; init; } = string.Empty;
    public decimal? PackageCount { get; init; }
    public decimal? GrossWeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public decimal? DeclaredValue { get; init; }
    public string? DeclaredCurrencyCode { get; init; }
    public DateTime? DesiredServiceDate { get; init; }

    public string? CustomerNotes { get; init; }
    public string? InternalNotes { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpdateImportRequestRequest : CreateImportRequestRequest
{
}

public sealed class ChangeRequestStatusRequest
{
    public string Comment { get; init; } = string.Empty;
}

public sealed class AssignImportRequestRequest
{
    public long? AssignedUserId { get; init; }
    public string? Comment { get; init; }
}
