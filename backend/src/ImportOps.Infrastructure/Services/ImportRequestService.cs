using ImportOps.Application.Common;
using ImportOps.Application.ImportRequests;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class ImportRequestService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : IImportRequestService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ImportRequestStatuses.New,
        ImportRequestStatuses.UnderReview,
        ImportRequestStatuses.Observed,
        ImportRequestStatuses.Quoted,
        ImportRequestStatuses.Approved,
        ImportRequestStatuses.Rejected,
        ImportRequestStatuses.Cancelled
    };

    public async Task<PagedResult<ImportRequestListItemDto>> GetAsync(ImportRequestQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q =
            from r in db.ImportRequests.IgnoreQueryFilters().AsNoTracking()
            join tm0 in db.TransportModes.AsNoTracking() on r.TransportModeId equals tm0.TransportModeId into tmj
            from tm in tmj.DefaultIfEmpty()
            where r.CompanyId == currentCompany.CompanyId && !r.IsDeleted
            select new { r, tm };

        // CLIENT_REQUEST_QUERY_SCOPE: the server, never React, is the data-scope authority.
        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            if (query.ClientId.HasValue && query.ClientId.Value != currentUser.ClientId.Value)
                throw new BusinessException(ErrorCodes.Forbidden, "No puede consultar solicitudes de otro cliente.", 403);

            var scopedClientId = currentUser.ClientId.Value;
            q = q.Where(x => x.r.ClientId == scopedClientId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x =>
                x.r.RequestNo.Contains(term) ||
                x.r.MerchandiseDescription.Contains(term) ||
                x.r.Client.LegalName.Contains(term) ||
                (x.r.Supplier != null && x.r.Supplier.LegalName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            q = q.Where(x => x.r.Status == status);
        }

        if (query.ClientId.HasValue) q = q.Where(x => x.r.ClientId == query.ClientId);
        if (query.SupplierId.HasValue) q = q.Where(x => x.r.SupplierId == query.SupplierId);
        if (query.AssignedUserId.HasValue) q = q.Where(x => x.r.AssignedUserId == query.AssignedUserId);
        if (query.TransportModeId.HasValue) q = q.Where(x => x.r.TransportModeId == query.TransportModeId);

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.Date;
            q = q.Where(x => x.r.RequestDate >= from);
        }

        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.Date;
            q = q.Where(x => x.r.RequestDate <= to);
        }

        var total = await q.CountAsync(ct);

        var rawItems = await q
            .OrderByDescending(x => x.r.RequestDate)
            .ThenByDescending(x => x.r.RequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.r.RequestId,
                x.r.RequestNo,
                x.r.RequestDate,
                x.r.Status,
                x.r.ClientId,
                ClientName = x.r.Client.LegalName,
                x.r.SupplierId,
                SupplierName = x.r.Supplier != null ? x.r.Supplier.LegalName : null,
                x.r.RequestedByUserId,
                RequestedByUsername = x.r.RequestedByUser.Username,
                RequestedByFirstName = x.r.RequestedByUser.FirstName,
                RequestedByLastName = x.r.RequestedByUser.LastName,
                x.r.AssignedUserId,
                AssignedUsername = x.r.AssignedUser != null ? x.r.AssignedUser.Username : null,
                AssignedFirstName = x.r.AssignedUser != null ? x.r.AssignedUser.FirstName : null,
                AssignedLastName = x.r.AssignedUser != null ? x.r.AssignedUser.LastName : null,
                x.r.TransportModeId,
                TransportMode = x.tm != null ? x.tm.Name : null,
                x.r.MerchandiseDescription,
                x.r.CreatedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(x => new ImportRequestListItemDto(
            x.RequestId,
            x.RequestNo,
            x.RequestDate,
            x.Status,
            x.ClientId,
            x.ClientName,
            x.SupplierId,
            x.SupplierName,
            x.RequestedByUserId,
            UserName(x.RequestedByUsername, x.RequestedByFirstName, x.RequestedByLastName),
            x.AssignedUserId,
            x.AssignedUserId.HasValue ? UserName(x.AssignedUsername!, x.AssignedFirstName, x.AssignedLastName) : null,
            x.TransportModeId,
            x.TransportMode,
            x.MerchandiseDescription,
            x.CreatedAt)).ToList();

        return new(items, page, pageSize, total);
    }

    public async Task<ImportRequestDetailDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var r = await db.ImportRequests.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.ClientContact)
            .Include(x => x.Supplier)
            .Include(x => x.RequestedByUser)
            .Include(x => x.AssignedUser)
            .SingleOrDefaultAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.RequestId == id &&
                !x.IsDeleted &&
                (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct);

        if (r is null) return null;

        string? transportName = null;
        string? originCountryName = null;
        string? destinationCountryName = null;
        string? originPortName = null;
        string? destinationPortName = null;

        if (r.TransportModeId.HasValue)
            transportName = await db.TransportModes.AsNoTracking()
                .Where(x => x.TransportModeId == r.TransportModeId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(ct);

        if (r.OriginCountryCode is not null)
            originCountryName = await db.Countries.AsNoTracking()
                .Where(x => x.CountryCode == r.OriginCountryCode)
                .Select(x => x.CountryName)
                .SingleOrDefaultAsync(ct);

        if (r.DestinationCountryCode is not null)
            destinationCountryName = await db.Countries.AsNoTracking()
                .Where(x => x.CountryCode == r.DestinationCountryCode)
                .Select(x => x.CountryName)
                .SingleOrDefaultAsync(ct);

        if (r.OriginPortId.HasValue)
            originPortName = await db.Ports.AsNoTracking()
                .Where(x => x.PortId == r.OriginPortId)
                .Select(x => x.PortName)
                .SingleOrDefaultAsync(ct);

        if (r.DestinationPortId.HasValue)
            destinationPortName = await db.Ports.AsNoTracking()
                .Where(x => x.PortId == r.DestinationPortId)
                .Select(x => x.PortName)
                .SingleOrDefaultAsync(ct);

        var historyQuery = db.RequestStatusHistories.AsNoTracking()
            .Where(h => h.RequestId == id);

        if (currentUser.IsClientUser)
            historyQuery = historyQuery.Where(h => h.OldStatus == null || h.OldStatus != h.NewStatus);

        var rawHistory = await historyQuery
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.RequestStatusHistoryId)
            .Select(h => new
            {
                h.RequestStatusHistoryId,
                h.OldStatus,
                h.NewStatus,
                h.Comment,
                h.ChangedAt,
                h.ChangedBy,
                Username = h.ChangedByUser.Username,
                FirstName = h.ChangedByUser.FirstName,
                LastName = h.ChangedByUser.LastName
            })
            .ToListAsync(ct);

        var history = rawHistory.Select(h => new RequestStatusHistoryDto(
            h.RequestStatusHistoryId,
            h.OldStatus,
            h.NewStatus,
            h.Comment,
            h.ChangedAt,
            h.ChangedBy,
            UserName(h.Username, h.FirstName, h.LastName))).ToList();

        return new ImportRequestDetailDto(
            r.RequestId,
            r.RequestNo,
            r.RequestDate,
            r.Status,
            r.ClientId,
            r.Client.LegalName,
            r.ClientContactId,
            r.ClientContact?.FullName,
            r.SupplierId,
            r.Supplier?.LegalName,
            r.RequestedByUserId,
            UserName(r.RequestedByUser),
            r.AssignedUserId,
            r.AssignedUser is not null ? UserName(r.AssignedUser) : null,
            r.TransportModeId,
            transportName,
            r.IncotermCode,
            r.OriginCountryCode,
            originCountryName,
            r.OriginPortId,
            originPortName,
            r.DestinationCountryCode,
            destinationCountryName,
            r.DestinationPortId,
            destinationPortName,
            r.MerchandiseDescription,
            r.PackageCount,
            r.GrossWeightKg,
            r.VolumeM3,
            r.DeclaredValue,
            r.DeclaredCurrencyCode,
            r.DesiredServiceDate,
            r.CustomerNotes,
            currentUser.IsClientUser ? null : r.InternalNotes,
            r.RejectionReason,
            r.ApprovedAt,
            r.ApprovedByUserId,
            r.CreatedAt,
            r.ModifiedAt,
            ConcurrencyToken.Encode(r.RowVer),
            history);
    }

    public async Task<ImportRequestDetailDto> CreateAsync(CreateImportRequestRequest request, CancellationToken ct)
    {
        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            if (request.ClientId != currentUser.ClientId.Value)
                throw new BusinessException("CLIENT_REQUEST_SCOPE", "No puede crear solicitudes para otro cliente.", 403);

            if (request.SupplierId.HasValue || request.AssignedUserId.HasValue || !string.IsNullOrWhiteSpace(request.InternalNotes))
                throw new BusinessException(
                    ErrorCodes.Forbidden,
                    "Un usuario cliente no puede asignar proveedor, responsable interno ni notas internas.",
                    403);
        }

        await ValidatePayloadAsync(request, ct);
        await EnsureCurrentUserAsync(ct);

        var sequence = await NextRequestSequenceAsync(ct);
        var requestNo = $"SOL-{DateTime.Today:yyyy}-{sequence:000000}";

        var entity = new ImportRequest
        {
            CompanyId = currentCompany.CompanyId,
            RequestNo = requestNo,
            ClientId = request.ClientId,
            ClientContactId = request.ClientContactId,
            SupplierId = currentUser.IsClientUser ? null : request.SupplierId,
            RequestedByUserId = currentUser.UserId,
            AssignedUserId = currentUser.IsClientUser ? null : request.AssignedUserId,
            RequestDate = (request.RequestDate ?? DateTime.Today).Date,
            Status = ImportRequestStatuses.New,
            TransportModeId = request.TransportModeId,
            IncotermCode = Clean(request.IncotermCode)?.ToUpperInvariant(),
            OriginCountryCode = Clean(request.OriginCountryCode)?.ToUpperInvariant(),
            DestinationCountryCode = Clean(request.DestinationCountryCode)?.ToUpperInvariant(),
            OriginPortId = request.OriginPortId,
            DestinationPortId = request.DestinationPortId,
            MerchandiseDescription = request.MerchandiseDescription.Trim(),
            PackageCount = request.PackageCount,
            GrossWeightKg = request.GrossWeightKg,
            VolumeM3 = request.VolumeM3,
            DeclaredValue = request.DeclaredValue,
            DeclaredCurrencyCode = Clean(request.DeclaredCurrencyCode)?.ToUpperInvariant(),
            DesiredServiceDate = request.DesiredServiceDate?.Date,
            CustomerNotes = Clean(request.CustomerNotes),
            InternalNotes = currentUser.IsClientUser ? null : Clean(request.InternalNotes),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.ImportRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            RequestId = entity.RequestId,
            OldStatus = null,
            NewStatus = ImportRequestStatuses.New,
            Comment = "Solicitud creada.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await GetByIdAsync(entity.RequestId, ct))!;
    }

    public async Task<ImportRequestDetailDto?> UpdateAsync(long id, UpdateImportRequestRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (!ImportRequestStatuses.Editable.Contains(entity.Status))
            throw new InvalidOperationException($"La solicitud no puede editarse mientras está en estado {entity.Status}.");

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue || request.ClientId != currentUser.ClientId.Value)
                throw new BusinessException(ErrorCodes.Forbidden, "No puede mover la solicitud a otro cliente.", 403);

            if ((request.SupplierId.HasValue && request.SupplierId != entity.SupplierId) ||
                (request.AssignedUserId.HasValue && request.AssignedUserId != entity.AssignedUserId) ||
                (!string.IsNullOrWhiteSpace(request.InternalNotes) && request.InternalNotes != entity.InternalNotes))
                throw new BusinessException(
                    ErrorCodes.Forbidden,
                    "Un usuario cliente no puede modificar proveedor, responsable interno ni notas internas.",
                    403);
        }

        await ValidatePayloadAsync(request, ct);
        db.Entry(entity).Property(x => x.RowVer).OriginalValue = ConcurrencyToken.Decode(request.RowVersion);

        entity.ClientId = currentUser.IsClientUser ? entity.ClientId : request.ClientId;
        entity.ClientContactId = request.ClientContactId;
        if (!currentUser.IsClientUser)
        {
            entity.SupplierId = request.SupplierId;
            entity.AssignedUserId = request.AssignedUserId;
        }
        entity.RequestDate = (request.RequestDate ?? entity.RequestDate).Date;
        entity.TransportModeId = request.TransportModeId;
        entity.IncotermCode = Clean(request.IncotermCode)?.ToUpperInvariant();
        entity.OriginCountryCode = Clean(request.OriginCountryCode)?.ToUpperInvariant();
        entity.DestinationCountryCode = Clean(request.DestinationCountryCode)?.ToUpperInvariant();
        entity.OriginPortId = request.OriginPortId;
        entity.DestinationPortId = request.DestinationPortId;
        entity.MerchandiseDescription = request.MerchandiseDescription.Trim();
        entity.PackageCount = request.PackageCount;
        entity.GrossWeightKg = request.GrossWeightKg;
        entity.VolumeM3 = request.VolumeM3;
        entity.DeclaredValue = request.DeclaredValue;
        entity.DeclaredCurrencyCode = Clean(request.DeclaredCurrencyCode)?.ToUpperInvariant();
        entity.DesiredServiceDate = request.DesiredServiceDate?.Date;
        entity.CustomerNotes = Clean(request.CustomerNotes);
        if (!currentUser.IsClientUser)
            entity.InternalNotes = Clean(request.InternalNotes);
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteDraftAsync(long id, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        if (!string.Equals(entity.Status, ImportRequestStatuses.New, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una solicitud NEW puede eliminarse. Para las demás use cancelación.");

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<ImportRequestDetailDto?> SubmitForReviewAsync(long id, string? comment, CancellationToken ct) =>
        TransitionAndGetAsync(id, ImportRequestStatuses.UnderReview,
            new[] { ImportRequestStatuses.New, ImportRequestStatuses.Observed },
            Clean(comment) ?? "Solicitud enviada a revisión.", null, ct);

    public Task<ImportRequestDetailDto?> ObserveAsync(long id, string comment, CancellationToken ct)
    {
        RequireComment(comment, "La observación es obligatoria.");
        return TransitionAndGetAsync(id, ImportRequestStatuses.Observed,
            new[] { ImportRequestStatuses.UnderReview }, comment.Trim(), null, ct);
    }

    public Task<ImportRequestDetailDto?> RejectAsync(long id, string reason, CancellationToken ct)
    {
        RequireComment(reason, "El motivo de rechazo es obligatorio.");
        return TransitionAndGetAsync(id, ImportRequestStatuses.Rejected,
            new[] { ImportRequestStatuses.UnderReview }, reason.Trim(), reason.Trim(), ct);
    }

    public Task<ImportRequestDetailDto?> CancelAsync(long id, string reason, CancellationToken ct)
    {
        RequireComment(reason, "El motivo de cancelación es obligatorio.");
        return TransitionAndGetAsync(id, ImportRequestStatuses.Cancelled,
            new[] { ImportRequestStatuses.New, ImportRequestStatuses.UnderReview, ImportRequestStatuses.Observed },
            reason.Trim(), reason.Trim(), ct);
    }

    public async Task<ImportRequestDetailDto?> AssignAsync(long id, long? assignedUserId, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (ImportRequestStatuses.Terminal.Contains(entity.Status))
            throw new InvalidOperationException("No se puede reasignar una solicitud finalizada.");

        if (assignedUserId.HasValue)
            await EnsureRequestAssigneeAsync(assignedUserId.Value, ct);

        entity.AssignedUserId = assignedUserId;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        // La asignación no cambia status; dejamos rastro como historial del estado actual.
        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            RequestId = entity.RequestId,
            OldStatus = entity.Status,
            NewStatus = entity.Status,
            Comment = Clean(comment) ?? (assignedUserId.HasValue ? $"Responsable asignado: {assignedUserId.Value}." : "Responsable removido."),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> MarkQuotedAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        if (string.Equals(entity.Status, ImportRequestStatuses.Quoted, StringComparison.OrdinalIgnoreCase))
        {
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser.UserId;

            db.RequestStatusHistories.Add(new RequestStatusHistory
            {
                RequestId = entity.RequestId,
                OldStatus = ImportRequestStatuses.Quoted,
                NewStatus = ImportRequestStatuses.Quoted,
                Comment = Clean(comment) ?? "Nueva versión de cotización enviada al cliente.",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = currentUser.UserId
            });

            await db.SaveChangesAsync(ct);
            return true;
        }

        if (!string.Equals(entity.Status, ImportRequestStatuses.UnderReview, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"No se puede marcar QUOTED desde el estado {entity.Status}.");

        await ChangeStatusAsync(entity, ImportRequestStatuses.Quoted,
            Clean(comment) ?? "Cotización generada para la solicitud.", null, ct);

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkApprovedFromQuotationAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        if (!string.Equals(entity.Status, ImportRequestStatuses.Quoted, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una solicitud QUOTED puede aprobarse desde una cotización.");

        await ChangeStatusAsync(entity, ImportRequestStatuses.Approved,
            Clean(comment) ?? "Cotización aprobada por el cliente.", null, ct);

        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<ImportRequestDetailDto?> TransitionAndGetAsync(
        long id, string newStatus, IReadOnlyCollection<string> allowedFrom,
        string comment, string? reason, CancellationToken ct)
    {
        var changed = await TransitionAsync(id, newStatus, allowedFrom, comment, reason, ct);
        return changed ? await GetByIdAsync(id, ct) : null;
    }

    private async Task<bool> TransitionAsync(
        long id, string newStatus, IReadOnlyCollection<string> allowedFrom,
        string comment, string? reason, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        if (!allowedFrom.Contains(entity.Status, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Transición inválida: {entity.Status} → {newStatus}.");

        await ChangeStatusAsync(entity, newStatus, comment, reason, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private Task ChangeStatusAsync(ImportRequest entity, string newStatus, string comment, string? reason, CancellationToken ct)
    {
        var oldStatus = entity.Status;
        entity.Status = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        if (newStatus == ImportRequestStatuses.Rejected)
            entity.RejectionReason = reason;
        else if (newStatus != ImportRequestStatuses.Rejected)
            entity.RejectionReason = null;

        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            RequestId = entity.RequestId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = comment,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        return Task.CompletedTask;
    }

    private async Task ValidatePayloadAsync(CreateImportRequestRequest r, CancellationToken ct)
    {
        if (r.ClientId <= 0) throw new ArgumentException("ClientId es obligatorio.");
        if (string.IsNullOrWhiteSpace(r.MerchandiseDescription))
            throw new ArgumentException("La descripción de la mercancía es obligatoria.");

        if (r.PackageCount < 0 || r.GrossWeightKg < 0 || r.VolumeM3 < 0 || r.DeclaredValue < 0)
            throw new ArgumentException("Bultos, peso, volumen y valor declarado no pueden ser negativos.");

        var client = await db.Clients.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.ClientId == r.ClientId && x.IsActive, ct);
        if (client is null) throw new ArgumentException("El cliente no existe, está inactivo o pertenece a otra empresa.");

        if (r.ClientContactId.HasValue)
        {
            var contactOk = await db.ClientContacts.AsNoTracking()
                .AnyAsync(x => x.ClientContactId == r.ClientContactId && x.ClientId == r.ClientId && x.IsActive, ct);
            if (!contactOk) throw new ArgumentException("El contacto no pertenece al cliente o está inactivo.");
        }

        if (r.SupplierId.HasValue)
        {
            var supplierOk = await db.Suppliers.AsNoTracking()
                .AnyAsync(x => x.SupplierId == r.SupplierId && x.CompanyId == currentCompany.CompanyId && x.IsActive, ct);
            if (!supplierOk) throw new ArgumentException("El proveedor no existe, está inactivo o pertenece a otra empresa.");
        }

        if (r.AssignedUserId.HasValue)
            await EnsureRequestAssigneeAsync(r.AssignedUserId.Value, ct);

        if (r.TransportModeId.HasValue &&
            !await db.TransportModes.AsNoTracking().AnyAsync(x => x.TransportModeId == r.TransportModeId && x.IsActive, ct))
            throw new ArgumentException("Modalidad de transporte inválida.");

        var incoterm = Clean(r.IncotermCode)?.ToUpperInvariant();
        if (incoterm is not null &&
            !await db.Incoterms.AsNoTracking().AnyAsync(x => x.IncotermCode == incoterm && x.IsActive, ct))
            throw new ArgumentException("Incoterm inválido.");

        var originCountry = Clean(r.OriginCountryCode)?.ToUpperInvariant();
        var destinationCountry = Clean(r.DestinationCountryCode)?.ToUpperInvariant();

        if (originCountry is not null &&
            !await db.Countries.AsNoTracking().AnyAsync(x => x.CountryCode == originCountry && x.IsActive, ct))
            throw new ArgumentException("País de origen inválido.");

        if (destinationCountry is not null &&
            !await db.Countries.AsNoTracking().AnyAsync(x => x.CountryCode == destinationCountry && x.IsActive, ct))
            throw new ArgumentException("País de destino inválido.");

        if (r.OriginPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking().SingleOrDefaultAsync(x => x.PortId == r.OriginPortId && x.IsActive, ct)
                ?? throw new ArgumentException("Puerto de origen inválido.");
            if (originCountry is not null && !string.Equals(port.CountryCode, originCountry, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El puerto de origen no pertenece al país de origen.");
        }

        if (r.DestinationPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking().SingleOrDefaultAsync(x => x.PortId == r.DestinationPortId && x.IsActive, ct)
                ?? throw new ArgumentException("Puerto de destino inválido.");
            if (destinationCountry is not null && !string.Equals(port.CountryCode, destinationCountry, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El puerto de destino no pertenece al país de destino.");
        }

        var currency = Clean(r.DeclaredCurrencyCode)?.ToUpperInvariant();
        if (r.DeclaredValue.HasValue && currency is null)
            throw new ArgumentException("Debe indicar moneda cuando registra un valor declarado.");

        if (currency is not null &&
            !await db.Currencies.AsNoTracking().AnyAsync(x => x.CurrencyCode == currency && x.IsActive, ct))
            throw new ArgumentException("Moneda declarada inválida.");
    }

    private async Task EnsureCurrentUserAsync(CancellationToken ct) =>
        await EnsureUserBelongsToCompanyAsync(currentUser.UserId,
            "El usuario actual no existe, está inactivo o pertenece a otra empresa. Revise X-User-Id/App:DefaultUserId.", ct);

    private async Task EnsureUserBelongsToCompanyAsync(long userId, string message, CancellationToken ct)
    {
        var ok = await db.AppUsers.AsNoTracking().AnyAsync(x =>
            x.UserId == userId && x.CompanyId == currentCompany.CompanyId && x.IsActive && !x.IsDeleted, ct);
        if (!ok) throw new InvalidOperationException(message);
    }

    private async Task EnsureRequestAssigneeAsync(long userId, CancellationToken ct)
    {
        var ok = await db.AppUsers.AsNoTracking().AnyAsync(u =>
            u.UserId == userId &&
            u.CompanyId == currentCompany.CompanyId &&
            u.IsActive &&
            !u.IsDeleted &&
            u.UserType == UserTypes.Internal &&
            !u.Username.StartsWith("system.") &&
            (from ur in db.UserRoles
             join role in db.Roles on ur.RoleId equals role.RoleId
             where ur.UserId == u.UserId &&
                   role.CompanyId == currentCompany.CompanyId &&
                   role.IsActive &&
                   (role.Code == "ADMIN" || role.Code == "COMMERCIAL" || role.Code == "OPERATIONS")
             select ur.UserId).Any(), ct);

        if (!ok)
            throw new BusinessException(
                ErrorCodes.Validation,
                "El responsable debe ser un usuario interno activo con perfil ADMIN, COMMERCIAL u OPERATIONS.",
                400);
    }

    private Task<ImportRequest?> GetOwnedTrackedAsync(long id, CancellationToken ct) =>
        db.ImportRequests.SingleOrDefaultAsync(x =>
            x.RequestId == id &&
            x.CompanyId == currentCompany.CompanyId &&
            (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct);

    private async Task<long> NextRequestSequenceAsync(CancellationToken ct)
        => await MariaDbSequence.NextAsync(db, "REQUEST_NO", ct);

    private static string NormalizeStatus(string value)
    {
        var status = value.Trim().ToUpperInvariant();
        if (!ValidStatuses.Contains(status)) throw new ArgumentException($"Estado inválido: {value}.");
        return status;
    }

    private static void RequireComment(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message);
    }

    private static string UserName(AppUser user) => UserName(user.Username, user.FirstName, user.LastName);

    private static string UserName(string username, string? firstName, string? lastName)
    {
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? username : full;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
