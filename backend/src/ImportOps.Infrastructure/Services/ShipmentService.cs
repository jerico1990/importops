using ImportOps.Application.Common;
using ImportOps.Application.Shipments;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class ShipmentService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : IShipmentService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ShipmentStatuses.Planned,
        ShipmentStatuses.Booked,
        ShipmentStatuses.Departed,
        ShipmentStatuses.InTransit,
        ShipmentStatuses.Arrived,
        ShipmentStatuses.Cancelled
    };

    public async Task<PagedResult<ShipmentListItemDto>> GetAsync(ShipmentQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q =
            from s in db.Shipments.AsNoTracking()
            join tm in db.TransportModes.AsNoTracking()
                on s.TransportModeId equals tm.TransportModeId
            join op0 in db.Ports.AsNoTracking()
                on s.OriginPortId equals op0.PortId into opj
            from op in opj.DefaultIfEmpty()
            join dp0 in db.Ports.AsNoTracking()
                on s.DestinationPortId equals dp0.PortId into dpj
            from dp in dpj.DefaultIfEmpty()
            where s.CompanyId == currentCompany.CompanyId &&
                  (!currentUser.IsClientUser || s.Operation.ClientId == currentUser.ClientId)
            select new
            {
                Shipment = s,
                TransportModeName = tm.Name,
                OriginPortName = op != null ? op.PortName : null,
                DestinationPortName = dp != null ? dp.PortName : null
            };

        if (query.OperationId.HasValue)
            q = q.Where(x => x.Shipment.OperationId == query.OperationId.Value);

        if (query.TransportModeId.HasValue)
            q = q.Where(x => x.Shipment.TransportModeId == query.TransportModeId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            q = q.Where(x => x.Shipment.ShipmentStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x =>
                x.Shipment.Operation.OperationNo.Contains(term) ||
                (x.Shipment.BookingNo != null && x.Shipment.BookingNo.Contains(term)) ||
                (x.Shipment.MasterDocumentNo != null && x.Shipment.MasterDocumentNo.Contains(term)) ||
                (x.Shipment.HouseDocumentNo != null && x.Shipment.HouseDocumentNo.Contains(term)) ||
                (x.Shipment.CarrierName != null && x.Shipment.CarrierName.Contains(term)));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(x => x.Shipment.CreatedAt)
            .ThenByDescending(x => x.Shipment.ShipmentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ShipmentListItemDto(
                x.Shipment.ShipmentId,
                x.Shipment.OperationId,
                x.Shipment.Operation.OperationNo,
                x.Shipment.ShipmentStatus,
                x.Shipment.TransportModeId,
                x.TransportModeName,
                x.Shipment.CarrierName,
                x.Shipment.BookingNo,
                x.Shipment.MasterDocumentNo,
                x.Shipment.HouseDocumentNo,
                x.Shipment.OriginPortId,
                x.OriginPortName,
                x.Shipment.DestinationPortId,
                x.DestinationPortName,
                x.Shipment.Etd,
                x.Shipment.Eta,
                x.Shipment.ActualDepartureAt,
                x.Shipment.ActualArrivalAt,
                db.ShipmentContainers.Count(c =>
                    c.ShipmentId == x.Shipment.ShipmentId &&
                    c.IsActive)))
            .ToListAsync(ct);

        return new(rows, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<ShipmentListItemDto>> GetByOperationAsync(long operationId, CancellationToken ct)
    {
        await EnsureOperationOwnedAsync(operationId, ct);

        var rows = await (
            from s in db.Shipments.AsNoTracking()
            join tm in db.TransportModes.AsNoTracking()
                on s.TransportModeId equals tm.TransportModeId
            join op0 in db.Ports.AsNoTracking()
                on s.OriginPortId equals op0.PortId into opj
            from op in opj.DefaultIfEmpty()
            join dp0 in db.Ports.AsNoTracking()
                on s.DestinationPortId equals dp0.PortId into dpj
            from dp in dpj.DefaultIfEmpty()
            where s.CompanyId == currentCompany.CompanyId &&
                  s.OperationId == operationId &&
                  (!currentUser.IsClientUser || s.Operation.ClientId == currentUser.ClientId)
            orderby s.CreatedAt descending, s.ShipmentId descending
            select new ShipmentListItemDto(
                s.ShipmentId,
                s.OperationId,
                s.Operation.OperationNo,
                s.ShipmentStatus,
                s.TransportModeId,
                tm.Name,
                s.CarrierName,
                s.BookingNo,
                s.MasterDocumentNo,
                s.HouseDocumentNo,
                s.OriginPortId,
                op != null ? op.PortName : null,
                s.DestinationPortId,
                dp != null ? dp.PortName : null,
                s.Etd,
                s.Eta,
                s.ActualDepartureAt,
                s.ActualArrivalAt,
                db.ShipmentContainers.Count(c =>
                    c.ShipmentId == s.ShipmentId &&
                    c.IsActive))
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<ShipmentDetailDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var row = await (
            from shipment in db.Shipments.AsNoTracking()
            join tm in db.TransportModes.AsNoTracking()
                on shipment.TransportModeId equals tm.TransportModeId
            join op0 in db.Ports.AsNoTracking()
                on shipment.OriginPortId equals op0.PortId into opj
            from op in opj.DefaultIfEmpty()
            join dp0 in db.Ports.AsNoTracking()
                on shipment.DestinationPortId equals dp0.PortId into dpj
            from dp in dpj.DefaultIfEmpty()
            where shipment.CompanyId == currentCompany.CompanyId &&
                  shipment.ShipmentId == id &&
                  (!currentUser.IsClientUser || shipment.Operation.ClientId == currentUser.ClientId)
            select new
            {
                Shipment = shipment,
                OperationNo = shipment.Operation.OperationNo,
                TransportMode = tm.Name,
                OriginPort = op != null ? op.PortName : null,
                DestinationPort = dp != null ? dp.PortName : null
            }
        ).SingleOrDefaultAsync(ct);

        if (row is null)
            return null;

        var containers = await db.ShipmentContainers.AsNoTracking()
            .Where(x => x.ShipmentId == id)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.ContainerNo)
            .Select(x => new ShipmentContainerDto(
                x.ShipmentContainerId,
                x.ContainerNo,
                x.ContainerType,
                x.SealNo,
                x.PackageCount,
                x.GrossWeightKg,
                x.VolumeM3,
                currentUser.IsClientUser ? null : x.Notes,
                x.IsActive))
            .ToListAsync(ct);

        var shipmentEntity = row.Shipment;

        return new ShipmentDetailDto(
            shipmentEntity.ShipmentId,
            shipmentEntity.OperationId,
            row.OperationNo,
            shipmentEntity.ShipmentStatus,
            shipmentEntity.TransportModeId,
            row.TransportMode,
            shipmentEntity.CarrierName,
            shipmentEntity.VesselFlightName,
            shipmentEntity.BookingNo,
            shipmentEntity.MasterDocumentNo,
            shipmentEntity.HouseDocumentNo,
            shipmentEntity.OriginPortId,
            row.OriginPort,
            shipmentEntity.DestinationPortId,
            row.DestinationPort,
            shipmentEntity.Etd,
            shipmentEntity.Eta,
            shipmentEntity.ActualDepartureAt,
            shipmentEntity.ActualArrivalAt,
            currentUser.IsClientUser ? null : shipmentEntity.Notes,
            shipmentEntity.CreatedAt,
            shipmentEntity.ModifiedAt,
            ConcurrencyToken.Encode(shipmentEntity.RowVer),
            containers);
    }

    public async Task<IReadOnlyCollection<ShipmentAvailableActionDto>> GetAvailableActionsAsync(long id, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null)
            throw new BusinessException(ErrorCodes.NotFound, "Embarque no encontrado.", 404);

        var result = new List<ShipmentAvailableActionDto>();

        if (entity.ShipmentStatus == ShipmentStatuses.Planned)
        {
            string? blocked = null;
            if (string.IsNullOrWhiteSpace(entity.BookingNo))
                blocked = "Registre el número de booking.";
            else if (!entity.Etd.HasValue || !entity.Eta.HasValue)
                blocked = "Registre ETD y ETA.";
            else if (entity.Eta <= entity.Etd)
                blocked = "ETA debe ser posterior a ETD.";

            result.Add(new("BOOK", "Confirmar booking", false, false, blocked is null, blocked));
            result.Add(new("CANCEL", "Cancelar embarque", false, true, true, null));
        }
        else if (entity.ShipmentStatus == ShipmentStatuses.Booked)
        {
            string? blocked = null;
            if (string.IsNullOrWhiteSpace(entity.CarrierName))
                blocked = "Registre el carrier/transportista.";
            else if (!entity.OriginPortId.HasValue || !entity.DestinationPortId.HasValue)
                blocked = "Registre origen y destino.";
            else
            {
                var mode = await db.TransportModes.AsNoTracking()
                    .SingleAsync(x => x.TransportModeId == entity.TransportModeId, ct);

                if (mode.Code == "SEA")
                {
                    if (string.IsNullOrWhiteSpace(entity.MasterDocumentNo) &&
                        string.IsNullOrWhiteSpace(entity.HouseDocumentNo))
                        blocked = "Registre MBL o HBL.";
                    else if (!await db.ShipmentContainers.AsNoTracking()
                        .AnyAsync(x => x.ShipmentId == entity.ShipmentId && x.IsActive, ct))
                        blocked = "Registre al menos un contenedor activo.";
                }
                else if (mode.Code == "AIR" &&
                         string.IsNullOrWhiteSpace(entity.MasterDocumentNo) &&
                         string.IsNullOrWhiteSpace(entity.HouseDocumentNo))
                    blocked = "Registre MAWB o HAWB.";
            }

            result.Add(new("DEPART", "Confirmar salida", true, false, blocked is null, blocked));
            result.Add(new("CANCEL", "Cancelar embarque", false, true, true, null));
        }
        else if (entity.ShipmentStatus == ShipmentStatuses.Departed)
        {
            result.Add(new("IN_TRANSIT", "Marcar en tránsito", false, false, true, null));
            result.Add(new("ARRIVE", "Confirmar arribo", true, false, true, null));
            result.Add(new("CANCEL", "Cancelar embarque", false, true, true, null));
        }
        else if (entity.ShipmentStatus == ShipmentStatuses.InTransit)
        {
            result.Add(new("ARRIVE", "Confirmar arribo", true, false, true, null));
            result.Add(new("CANCEL", "Cancelar embarque", false, true, true, null));
        }

        return result;
    }

    public async Task<ShipmentDetailDto> CreateAsync(long operationId, CreateShipmentRequest request, CancellationToken ct)
    {
        var operation = await GetOperationAsync(operationId, ct);

        if (operation.OperationalStatus is OperationStatuses.Cancelled or OperationStatuses.Closed)
            throw new BusinessException("OPERATION_NOT_ACTIVE", "La operación no admite nuevos embarques.", 409);

        var transportModeId = request.TransportModeId ?? operation.TransportModeId
            ?? throw new BusinessException(ErrorCodes.Validation, "La modalidad de transporte es obligatoria.", 400);

        await ValidateHeaderAsync(
            operation,
            transportModeId,
            request.OriginPortId ?? operation.OriginPortId,
            request.DestinationPortId ?? operation.DestinationPortId,
            request.Etd,
            request.Eta,
            ct);

        var entity = new Shipment
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = operation.OperationId,
            TransportModeId = transportModeId,
            CarrierName = Clean(request.CarrierName),
            VesselFlightName = Clean(request.VesselFlightName),
            BookingNo = Clean(request.BookingNo),
            MasterDocumentNo = Clean(request.MasterDocumentNo),
            HouseDocumentNo = Clean(request.HouseDocumentNo),
            OriginPortId = request.OriginPortId ?? operation.OriginPortId,
            DestinationPortId = request.DestinationPortId ?? operation.DestinationPortId,
            Etd = request.Etd,
            Eta = request.Eta,
            ShipmentStatus = ShipmentStatuses.Planned,
            Notes = Clean(request.Notes),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Shipments.Add(entity);
        await db.SaveChangesAsync(ct);

        AddEvent(operation.OperationId,
            "SHIPMENT_CREATED",
            DateTime.UtcNow,
            "Embarque creado",
            $"Shipment #{entity.ShipmentId} creado en estado PLANNED.");

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await GetByIdAsync(entity.ShipmentId, ct))!;
    }

    public async Task<ShipmentDetailDto?> UpdateAsync(long id, UpdateShipmentRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (entity.ShipmentStatus is ShipmentStatuses.Departed or ShipmentStatuses.InTransit or ShipmentStatuses.Arrived or ShipmentStatuses.Cancelled)
            throw new BusinessException("SHIPMENT_LOCKED", "El embarque ya inició ejecución y su cabecera logística no puede modificarse libremente.", 409);

        db.Entry(entity).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(request.RowVersion);

        var operation = await GetOperationAsync(entity.OperationId, ct);
        var transportModeId = request.TransportModeId ?? entity.TransportModeId;

        await ValidateHeaderAsync(
            operation,
            transportModeId,
            request.OriginPortId,
            request.DestinationPortId,
            request.Etd,
            request.Eta,
            ct);

        entity.TransportModeId = transportModeId;
        entity.CarrierName = Clean(request.CarrierName);
        entity.VesselFlightName = Clean(request.VesselFlightName);
        entity.BookingNo = Clean(request.BookingNo);
        entity.MasterDocumentNo = Clean(request.MasterDocumentNo);
        entity.HouseDocumentNo = Clean(request.HouseDocumentNo);
        entity.OriginPortId = request.OriginPortId;
        entity.DestinationPortId = request.DestinationPortId;
        entity.Etd = request.Etd;
        entity.Eta = request.Eta;
        entity.Notes = Clean(request.Notes);
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeletePlannedAsync(long id, string rowVersion, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        if (!string.Equals(entity.ShipmentStatus, ShipmentStatuses.Planned, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("SHIPMENT_DELETE_NOT_ALLOWED", "Solo un embarque PLANNED puede eliminarse.", 409);

        db.Entry(entity).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(rowVersion);

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        AddEvent(entity.OperationId,
            "SHIPMENT_DELETED",
            DateTime.UtcNow,
            "Embarque eliminado",
            $"Shipment #{entity.ShipmentId} eliminado antes de ser reservado.");

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShipmentContainerDto?> AddContainerAsync(long shipmentId, UpsertShipmentContainerRequest request, CancellationToken ct)
    {
        var shipment = await GetOwnedTrackedAsync(shipmentId, ct);
        if (shipment is null) return null;

        EnsureContainerEditable(shipment);
        await EnsureSeaShipmentAsync(shipment, ct);
        ValidateContainer(request);

        var containerNo = NormalizeContainerNo(request.ContainerNo);

        if (await db.ShipmentContainers.AnyAsync(x =>
            x.ShipmentId == shipmentId &&
            x.ContainerNo == containerNo &&
            x.IsActive, ct))
            throw new BusinessException("CONTAINER_DUPLICATE", "El contenedor ya está registrado en este embarque.", 409);

        var entity = new ShipmentContainer
        {
            ShipmentId = shipmentId,
            ContainerNo = containerNo,
            ContainerType = NormalizeContainerType(request.ContainerType),
            SealNo = Clean(request.SealNo)?.ToUpperInvariant(),
            PackageCount = request.PackageCount,
            GrossWeightKg = request.GrossWeightKg,
            VolumeM3 = request.VolumeM3,
            Notes = Clean(request.Notes),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        db.ShipmentContainers.Add(entity);
        shipment.ModifiedAt = DateTime.UtcNow;
        shipment.ModifiedBy = currentUser.UserId;
        await db.SaveChangesAsync(ct);

        return MapContainer(entity);
    }

    public async Task<ShipmentContainerDto?> UpdateContainerAsync(long shipmentId, long containerId, UpsertShipmentContainerRequest request, CancellationToken ct)
    {
        var shipment = await GetOwnedTrackedAsync(shipmentId, ct);
        if (shipment is null) return null;

        EnsureContainerEditable(shipment);
        await EnsureSeaShipmentAsync(shipment, ct);
        ValidateContainer(request);

        var entity = await db.ShipmentContainers.SingleOrDefaultAsync(x =>
            x.ShipmentId == shipmentId && x.ShipmentContainerId == containerId, ct);

        if (entity is null) return null;

        var containerNo = NormalizeContainerNo(request.ContainerNo);

        if (await db.ShipmentContainers.AnyAsync(x =>
            x.ShipmentId == shipmentId &&
            x.ShipmentContainerId != containerId &&
            x.ContainerNo == containerNo &&
            x.IsActive, ct))
            throw new BusinessException("CONTAINER_DUPLICATE", "El contenedor ya está registrado en este embarque.", 409);

        entity.ContainerNo = containerNo;
        entity.ContainerType = NormalizeContainerType(request.ContainerType);
        entity.SealNo = Clean(request.SealNo)?.ToUpperInvariant();
        entity.PackageCount = request.PackageCount;
        entity.GrossWeightKg = request.GrossWeightKg;
        entity.VolumeM3 = request.VolumeM3;
        entity.Notes = Clean(request.Notes);
        entity.IsActive = request.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        shipment.ModifiedAt = DateTime.UtcNow;
        shipment.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return MapContainer(entity);
    }

    public async Task<bool> DeleteContainerAsync(long shipmentId, long containerId, CancellationToken ct)
    {
        var shipment = await GetOwnedTrackedAsync(shipmentId, ct);
        if (shipment is null) return false;

        EnsureContainerEditable(shipment);

        var entity = await db.ShipmentContainers.SingleOrDefaultAsync(x =>
            x.ShipmentId == shipmentId && x.ShipmentContainerId == containerId, ct);

        if (entity is null) return false;

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        shipment.ModifiedAt = DateTime.UtcNow;
        shipment.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShipmentDetailDto?> BookAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        EnsureTransition(entity, ShipmentStatuses.Booked, ShipmentStatuses.Planned);

        if (string.IsNullOrWhiteSpace(entity.BookingNo))
            throw new BusinessException("BOOKING_REQUIRED", "Debe registrar booking_no antes de confirmar la reserva.", 409);

        if (!entity.Etd.HasValue || !entity.Eta.HasValue)
            throw new BusinessException("SCHEDULE_REQUIRED", "ETD y ETA son obligatorios antes de confirmar booking.", 409);

        if (entity.Eta <= entity.Etd)
            throw new BusinessException("INVALID_SCHEDULE", "ETA debe ser posterior a ETD.", 409);

        await ChangeStatusAsync(entity, ShipmentStatuses.Booked,
            "SHIPMENT_BOOKED",
            "Booking confirmado",
            Clean(comment) ?? $"Booking {entity.BookingNo} confirmado.",
            DateTime.UtcNow,
            ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ShipmentDetailDto?> DepartAsync(long id, DateTime actualDepartureAt, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        EnsureTransition(entity, ShipmentStatuses.Departed, ShipmentStatuses.Booked);

        if (actualDepartureAt == default)
            throw new BusinessException(ErrorCodes.Validation, "actualDepartureAt es obligatorio.", 400);

        await ValidateDepartureRequirementsAsync(entity, ct);

        entity.ActualDepartureAt = actualDepartureAt.ToUniversalTime();

        await ChangeStatusAsync(entity, ShipmentStatuses.Departed,
            "SHIPMENT_DEPARTED",
            "Carga embarcada",
            Clean(comment) ?? $"Salida real confirmada para Shipment #{entity.ShipmentId}.",
            entity.ActualDepartureAt.Value,
            ct);

        await EnsureOperationInProgressAsync(entity.OperationId, ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ShipmentDetailDto?> MarkInTransitAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        EnsureTransition(entity, ShipmentStatuses.InTransit, ShipmentStatuses.Departed);

        if (!entity.ActualDepartureAt.HasValue)
            throw new BusinessException("DEPARTURE_REQUIRED", "No puede marcar IN_TRANSIT sin salida real.", 409);

        await ChangeStatusAsync(entity, ShipmentStatuses.InTransit,
            "SHIPMENT_IN_TRANSIT",
            "Embarque en tránsito",
            Clean(comment) ?? $"Shipment #{entity.ShipmentId} en tránsito.",
            DateTime.UtcNow,
            ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ShipmentDetailDto?> ArriveAsync(long id, DateTime actualArrivalAt, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (entity.ShipmentStatus is not (ShipmentStatuses.Departed or ShipmentStatuses.InTransit))
            throw new BusinessException("INVALID_SHIPMENT_TRANSITION",
                $"No se puede marcar ARRIVED desde {entity.ShipmentStatus}.", 409);

        if (!entity.ActualDepartureAt.HasValue)
            throw new BusinessException("DEPARTURE_REQUIRED", "No puede registrar arribo sin salida real.", 409);

        var arrivalUtc = actualArrivalAt.ToUniversalTime();
        if (arrivalUtc <= entity.ActualDepartureAt.Value)
            throw new BusinessException("INVALID_ARRIVAL_DATE", "La fecha de arribo debe ser posterior a la salida real.", 409);

        entity.ActualArrivalAt = arrivalUtc;

        await ChangeStatusAsync(entity, ShipmentStatuses.Arrived,
            "SHIPMENT_ARRIVED",
            "Arribo confirmado",
            Clean(comment) ?? $"Arribo real confirmado para Shipment #{entity.ShipmentId}.",
            arrivalUtc,
            ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ShipmentDetailDto?> CancelAsync(long id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessException(ErrorCodes.Validation, "El motivo de cancelación es obligatorio.", 400);

        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (entity.ShipmentStatus is ShipmentStatuses.Arrived or ShipmentStatuses.Cancelled)
            throw new BusinessException("SHIPMENT_CANCEL_NOT_ALLOWED",
                $"No se puede cancelar un embarque {entity.ShipmentStatus}.", 409);

        await ChangeStatusAsync(entity, ShipmentStatuses.Cancelled,
            "SHIPMENT_CANCELLED",
            "Embarque cancelado",
            reason.Trim(),
            DateTime.UtcNow,
            ct);

        return await GetByIdAsync(id, ct);
    }


    private async Task ValidateHeaderAsync(
        ImportOperation operation,
        int transportModeId,
        long? originPortId,
        long? destinationPortId,
        DateTime? etd,
        DateTime? eta,
        CancellationToken ct)
    {
        var mode = await db.TransportModes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TransportModeId == transportModeId && x.IsActive, ct)
            ?? throw new BusinessException(ErrorCodes.Validation, "Modalidad de transporte inválida.", 400);

        if (originPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PortId == originPortId.Value && x.IsActive, ct)
                ?? throw new BusinessException(ErrorCodes.Validation, "Puerto de origen inválido.", 400);

            if (!string.IsNullOrWhiteSpace(operation.OriginCountryCode) &&
                !string.Equals(port.CountryCode, operation.OriginCountryCode, StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("ORIGIN_PORT_COUNTRY_MISMATCH",
                    "El puerto de origen no pertenece al país de origen de la operación.", 409);
        }

        if (destinationPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PortId == destinationPortId.Value && x.IsActive, ct)
                ?? throw new BusinessException(ErrorCodes.Validation, "Puerto de destino inválido.", 400);

            if (!string.IsNullOrWhiteSpace(operation.DestinationCountryCode) &&
                !string.Equals(port.CountryCode, operation.DestinationCountryCode, StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("DESTINATION_PORT_COUNTRY_MISMATCH",
                    "El puerto de destino no pertenece al país destino de la operación.", 409);
        }

        if (etd.HasValue && eta.HasValue && eta.Value <= etd.Value)
            throw new BusinessException("INVALID_SCHEDULE", "ETA debe ser posterior a ETD.", 409);

        // Marítimo/aéreo requieren puertos/aeropuertos.
        if (mode.Code is "SEA" or "AIR")
        {
            if (!originPortId.HasValue || !destinationPortId.HasValue)
                throw new BusinessException("PORTS_REQUIRED",
                    "Origen y destino son obligatorios para transporte marítimo/aéreo.", 409);
        }
    }

    private async Task ValidateDepartureRequirementsAsync(Shipment entity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.CarrierName))
            throw new BusinessException("CARRIER_REQUIRED", "Debe registrar el transportista/carrier antes de la salida.", 409);

        if (!entity.OriginPortId.HasValue || !entity.DestinationPortId.HasValue)
            throw new BusinessException("PORTS_REQUIRED", "Debe registrar origen y destino antes de la salida.", 409);

        var mode = await db.TransportModes.AsNoTracking()
            .SingleAsync(x => x.TransportModeId == entity.TransportModeId, ct);

        if (mode.Code == "SEA")
        {
            if (string.IsNullOrWhiteSpace(entity.MasterDocumentNo) &&
                string.IsNullOrWhiteSpace(entity.HouseDocumentNo))
                throw new BusinessException("BL_REQUIRED", "Debe registrar MBL o HBL antes de la salida marítima.", 409);

            var containers = await db.ShipmentContainers.AsNoTracking()
                .CountAsync(x => x.ShipmentId == entity.ShipmentId && x.IsActive, ct);

            if (containers == 0)
                throw new BusinessException("CONTAINER_REQUIRED", "Un embarque marítimo debe tener al menos un contenedor activo.", 409);
        }

        if (mode.Code == "AIR")
        {
            if (string.IsNullOrWhiteSpace(entity.MasterDocumentNo) &&
                string.IsNullOrWhiteSpace(entity.HouseDocumentNo))
                throw new BusinessException("AWB_REQUIRED", "Debe registrar MAWB o HAWB antes de la salida aérea.", 409);
        }
    }

    private async Task ChangeStatusAsync(
        Shipment entity,
        string newStatus,
        string eventType,
        string title,
        string description,
        DateTime eventAt,
        CancellationToken ct)
    {
        entity.ShipmentStatus = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        AddEvent(entity.OperationId, eventType, eventAt, title, description);
        await db.SaveChangesAsync(ct);
    }

    private void AddEvent(long operationId, string eventType, DateTime eventAt, string title, string description)
    {
        db.OperationEvents.Add(new OperationEvent
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = operationId,
            EventType = eventType,
            EventDate = eventAt,
            Title = title,
            Description = description,
            SourceType = "SYSTEM",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        });
    }

    private async Task EnsureOperationInProgressAsync(long operationId, CancellationToken ct)
    {
        var operation = await db.ImportOperations
            .SingleAsync(x => x.OperationId == operationId && x.CompanyId == currentCompany.CompanyId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct);

        if (operation.OperationalStatus == OperationStatuses.Open)
        {
            var old = operation.OperationalStatus;
            operation.OperationalStatus = OperationStatuses.InProgress;
            operation.ModifiedAt = DateTime.UtcNow;
            operation.ModifiedBy = currentUser.UserId;

            db.OperationStatusHistories.Add(new OperationStatusHistory
            {
                OperationId = operation.OperationId,
                StatusDimension = "OPERATIONAL",
                OldStatus = old,
                NewStatus = OperationStatuses.InProgress,
                Comment = "La operación inició ejecución con la salida de un embarque.",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = currentUser.UserId
            });

            await db.SaveChangesAsync(ct);
        }
    }

    private Task<Shipment?> GetOwnedTrackedAsync(long id, CancellationToken ct) =>
        db.Shipments.SingleOrDefaultAsync(x =>
            x.ShipmentId == id &&
            x.CompanyId == currentCompany.CompanyId &&
            (!currentUser.IsClientUser || x.Operation.ClientId == currentUser.ClientId), ct);

    private async Task<ImportOperation> GetOperationAsync(long operationId, CancellationToken ct) =>
        await db.ImportOperations.SingleOrDefaultAsync(x =>
            x.OperationId == operationId && x.CompanyId == currentCompany.CompanyId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct)
        ?? throw new BusinessException(ErrorCodes.NotFound, "La operación no existe.", 404);

    private async Task EnsureOperationOwnedAsync(long operationId, CancellationToken ct)
    {
        if (!await db.ImportOperations.AsNoTracking().AnyAsync(x =>
            x.OperationId == operationId && x.CompanyId == currentCompany.CompanyId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct))
            throw new BusinessException(ErrorCodes.NotFound, "La operación no existe.", 404);
    }

    private async Task EnsureSeaShipmentAsync(Shipment shipment, CancellationToken ct)
    {
        var mode = await db.TransportModes.AsNoTracking()
            .SingleAsync(x => x.TransportModeId == shipment.TransportModeId, ct);

        if (!string.Equals(mode.Code, "SEA", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("CONTAINERS_ONLY_FOR_SEA",
                "Los contenedores se gestionan únicamente en embarques marítimos.", 409);
    }

    private static void EnsureContainerEditable(Shipment shipment)
    {
        if (shipment.ShipmentStatus is ShipmentStatuses.Departed or ShipmentStatuses.InTransit or ShipmentStatuses.Arrived or ShipmentStatuses.Cancelled)
            throw new BusinessException("CONTAINER_LOCKED",
                "Los contenedores no pueden modificarse después de la salida del embarque.", 409);
    }

    private static void EnsureTransition(Shipment entity, string to, params string[] from)
    {
        if (!from.Contains(entity.ShipmentStatus, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException("INVALID_SHIPMENT_TRANSITION",
                $"Transición inválida: {entity.ShipmentStatus} → {to}.", 409);
    }

    private static void ValidateContainer(UpsertShipmentContainerRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ContainerNo))
            throw new BusinessException(ErrorCodes.Validation, "ContainerNo es obligatorio.", 400);

        if (r.PackageCount < 0 || r.GrossWeightKg < 0 || r.VolumeM3 < 0)
            throw new BusinessException(ErrorCodes.Validation,
                "Bultos, peso y volumen del contenedor no pueden ser negativos.", 400);
    }

    private static string NormalizeContainerNo(string value) =>
        value.Trim().Replace(" ", string.Empty).ToUpperInvariant();

    private static string? NormalizeContainerType(string? value)
    {
        var clean = Clean(value)?.ToUpperInvariant();
        if (clean is null) return null;

        var allowed = new[] { "20GP", "40GP", "40HC", "45HC", "20RF", "40RF", "OPEN_TOP", "FLAT_RACK", "OTHER" };
        if (!allowed.Contains(clean, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException(ErrorCodes.Validation, "ContainerType inválido.", 400);

        return clean;
    }

    private static string NormalizeStatus(string value)
    {
        var status = value.Trim().ToUpperInvariant();
        if (!ValidStatuses.Contains(status))
            throw new BusinessException(ErrorCodes.Validation, "Estado de embarque inválido.", 400);
        return status;
    }

    private static ShipmentContainerDto MapContainer(ShipmentContainer x) =>
        new(x.ShipmentContainerId, x.ContainerNo, x.ContainerType, x.SealNo,
            x.PackageCount, x.GrossWeightKg, x.VolumeM3, x.Notes, x.IsActive);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
