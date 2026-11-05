using ImportOps.Application.Common;
using ImportOps.Application.Customs;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class CustomsService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : ICustomsService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        CustomsStatuses.Pending,
        CustomsStatuses.InReview,
        CustomsStatuses.Observed,
        CustomsStatuses.Released,
        CustomsStatuses.Cancelled
    };

    public async Task<PagedResult<CustomsListItemDto>> GetAsync(CustomsQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.CustomsProcesses.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId);

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            q = q.Where(x => x.Operation.ClientId == currentUser.ClientId.Value);
        }

        if (query.OperationId.HasValue) q = q.Where(x => x.OperationId == query.OperationId.Value);
        if (query.ShipmentId.HasValue) q = q.Where(x => x.ShipmentId == query.ShipmentId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            q = q.Where(x => x.CustomsStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Channel))
        {
            var channel = NormalizeChannel(query.Channel);
            q = q.Where(x => x.CustomsChannel == channel);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x =>
                x.Operation.OperationNo.Contains(term) ||
                (x.DeclarationNo != null && x.DeclarationNo.Contains(term)) ||
                (x.CustomsOfficeCatalog != null && x.CustomsOfficeCatalog.Name.Contains(term)) ||
                (x.CustomsRegimeCatalog != null && x.CustomsRegimeCatalog.Name.Contains(term)) ||
                (x.CustomsOffice != null && x.CustomsOffice.Contains(term)) ||
                (x.CustomsRegime != null && x.CustomsRegime.Contains(term)));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.CustomsProcessId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomsListItemDto(
                x.CustomsProcessId,
                x.OperationId,
                x.Operation.OperationNo,
                x.ShipmentId,
                x.Shipment.ShipmentStatus,
                x.DeclarationNo,
                x.DeclarationDate,
                x.CustomsRegimeId,
                x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Name : x.CustomsRegime,
                x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Code : null,
                x.CustomsOfficeId,
                x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Name : x.CustomsOffice,
                x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Code : null,
                x.CustomsChannel,
                x.CustomsStatus,
                x.DutiesAmount,
                x.DutiesCurrencyCode,
                x.ReleaseDate,
                db.CustomsObservations.Count(o =>
                    o.CustomsProcessId == x.CustomsProcessId &&
                    o.ObservationStatus == CustomsObservationStatuses.Open)))
            .ToListAsync(ct);

        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<CustomsListItemDto>> GetByOperationAsync(long operationId, CancellationToken ct)
    {
        await EnsureOperationOwnedAsync(operationId, ct);

        return await db.CustomsProcesses.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.OperationId == operationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CustomsListItemDto(
                x.CustomsProcessId,
                x.OperationId,
                x.Operation.OperationNo,
                x.ShipmentId,
                x.Shipment.ShipmentStatus,
                x.DeclarationNo,
                x.DeclarationDate,
                x.CustomsRegimeId,
                x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Name : x.CustomsRegime,
                x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Code : null,
                x.CustomsOfficeId,
                x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Name : x.CustomsOffice,
                x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Code : null,
                x.CustomsChannel,
                x.CustomsStatus,
                x.DutiesAmount,
                x.DutiesCurrencyCode,
                x.ReleaseDate,
                db.CustomsObservations.Count(o =>
                    o.CustomsProcessId == x.CustomsProcessId &&
                    o.ObservationStatus == CustomsObservationStatuses.Open)))
            .ToListAsync(ct);
    }

    public async Task<CustomsDetailDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await db.CustomsProcesses.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.CustomsProcessId == id && (!currentUser.IsClientUser || x.Operation.ClientId == currentUser.ClientId))
            .Select(x => new
            {
                Process = x,
                OperationNo = x.Operation.OperationNo,
                ShipmentStatus = x.Shipment.ShipmentStatus,
                x.Shipment.BookingNo,
                x.Shipment.MasterDocumentNo,
                x.Shipment.HouseDocumentNo,
                RegimeName = x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Name : x.CustomsRegime,
                RegimeCode = x.CustomsRegimeCatalog != null ? x.CustomsRegimeCatalog.Code : null,
                OfficeName = x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Name : x.CustomsOffice,
                OfficeCode = x.CustomsOfficeCatalog != null ? x.CustomsOfficeCatalog.Code : null
            })
            .SingleOrDefaultAsync(ct);

        if (entity is null) return null;

        var observationEntities = await db.CustomsObservations.AsNoTracking()
            .Where(x => x.CustomsProcessId == id)
            .OrderBy(x => x.ObservationStatus == CustomsObservationStatuses.Resolved)
            .ThenByDescending(x => x.ObservedAt)
            .ToListAsync(ct);

        var observations = observationEntities
            .Select(MapObservation)
            .ToList();

        var taxEntities = await db.CustomsTaxes.AsNoTracking()
            .Where(x => x.CustomsProcessId == id)
            .OrderBy(x => x.CustomsTaxConcept != null ? x.CustomsTaxConcept.DisplayOrder : 999)
            .ThenBy(x => x.CustomsTaxId)
            .ToListAsync(ct);

        var taxes = taxEntities
            .Select(MapTax)
            .ToList();

        var history = await db.CustomsStatusHistories.AsNoTracking()
            .Where(x => x.CustomsProcessId == id)
            .OrderByDescending(x => x.ChangedAt)
            .ThenByDescending(x => x.CustomsStatusHistoryId)
            .Select(x => new CustomsStatusHistoryDto(
                x.CustomsStatusHistoryId,
                x.OldStatus,
                x.NewStatus,
                x.Comment,
                x.ChangedAt,
                x.ChangedBy))
            .ToListAsync(ct);

        var p = entity.Process;

        return new CustomsDetailDto(
            p.CustomsProcessId,
            p.OperationId,
            entity.OperationNo,
            p.ShipmentId,
            entity.ShipmentStatus,
            entity.BookingNo,
            entity.MasterDocumentNo,
            entity.HouseDocumentNo,
            p.CustomsRegimeId,
            entity.RegimeName,
            entity.RegimeCode,
            p.DeclarationNo,
            p.DeclarationDate,
            p.CustomsOfficeId,
            entity.OfficeName,
            entity.OfficeCode,
            p.CustomsChannel,
            p.CustomsStatus,
            p.DutiesAmount,
            p.DutiesCurrencyCode,
            p.DutiesExchangeRate,
            p.DutiesBaseAmount,
            p.ReleaseDate,
            p.Observation,
            p.CreatedAt,
            p.ModifiedAt,
            ConcurrencyToken.Encode(p.RowVer),
            observations,
            taxes,
            history);
    }

    public async Task<CustomsDetailDto> CreateAsync(long operationId, CreateCustomsProcessRequest request, CancellationToken ct)
    {
        var operation = await GetOperationAsync(operationId, ct);

        if (operation.OperationalStatus is OperationStatuses.Cancelled or OperationStatuses.Closed)
            throw new BusinessException("OPERATION_NOT_ACTIVE", "La operación no admite gestión aduanera.", 409);

        var currentStage = await db.WorkflowStages.AsNoTracking()
            .SingleAsync(x => x.WorkflowStageId == operation.CurrentStageId, ct);

        if (currentStage.Code is not ("ARRIVED" or "CUSTOMS"))
            throw new BusinessException(
                "CUSTOMS_STAGE_NOT_REACHED",
                "El proceso aduanero solo puede abrirse cuando la operación está ARRIVED o CUSTOMS.",
                409);

        var shipment = await ResolveShipmentAsync(operationId, request.ShipmentId, ct);

        if (shipment.ShipmentStatus != ShipmentStatuses.Arrived || !shipment.ActualArrivalAt.HasValue)
            throw new BusinessException("SHIPMENT_NOT_ARRIVED",
                "El proceso aduanero requiere un embarque con arribo real confirmado.", 409);

        if (await db.CustomsProcesses.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.ShipmentId == shipment.ShipmentId, ct))
            throw new BusinessException("CUSTOMS_ALREADY_EXISTS",
                "El embarque ya tiene un proceso aduanero registrado.", 409);

        await ValidateCurrencyAsync(request.DutiesCurrencyCode, ct);
        ValidateExchangeRate(request.DutiesExchangeRate);
        var regime = await ResolveRegimeAsync(request.CustomsRegimeId, ct);
        var office = await ResolveOfficeAsync(request.CustomsOfficeId, ct);

        var entity = new CustomsProcess
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = operationId,
            ShipmentId = shipment.ShipmentId,
            CustomsRegimeId = regime?.CustomsRegimeId,
            CustomsRegime = regime?.Name,
            DeclarationNo = Clean(request.DeclarationNo)?.ToUpperInvariant(),
            DeclarationDate = request.DeclarationDate?.Date,
            CustomsOfficeId = office?.CustomsOfficeId,
            CustomsOffice = office?.Name,
            CustomsStatus = CustomsStatuses.Pending,
            DutiesCurrencyCode = Clean(request.DutiesCurrencyCode)?.ToUpperInvariant() ?? "PEN",
            DutiesExchangeRate = request.DutiesExchangeRate,
            DutiesAmount = 0,
            DutiesBaseAmount = 0,
            Observation = Clean(request.Observation),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.CustomsProcesses.Add(entity);
        await db.SaveChangesAsync(ct);

        db.CustomsStatusHistories.Add(new CustomsStatusHistory
        {
            CustomsProcessId = entity.CustomsProcessId,
            OldStatus = null,
            NewStatus = CustomsStatuses.Pending,
            Comment = "Proceso aduanero creado.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        AddOperationEvent(
            operationId,
            "CUSTOMS_CREATED",
            "Proceso aduanero abierto",
            $"Se abrió Customs #{entity.CustomsProcessId} para Shipment #{shipment.ShipmentId}.",
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await GetByIdAsync(entity.CustomsProcessId, ct))!;
    }

    public async Task<CustomsDetailDto?> UpdateAsync(long id, UpdateCustomsProcessRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (entity.CustomsStatus is CustomsStatuses.Released or CustomsStatuses.Cancelled)
            throw new BusinessException("CUSTOMS_LOCKED",
                "Un proceso aduanero RELEASED/CANCELLED no puede editarse.", 409);

        db.Entry(entity).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(request.RowVersion);

        await ValidateCurrencyAsync(request.DutiesCurrencyCode, ct);
        ValidateExchangeRate(request.DutiesExchangeRate);
        var regime = await ResolveRegimeAsync(request.CustomsRegimeId, ct);
        var office = await ResolveOfficeAsync(request.CustomsOfficeId, ct);

        var currency = Clean(request.DutiesCurrencyCode)?.ToUpperInvariant() ?? entity.DutiesCurrencyCode;

        var hasTaxes = await db.CustomsTaxes.AsNoTracking().AnyAsync(x =>
            x.CustomsProcessId == id && x.PaymentStatus != CustomsTaxPaymentStatuses.Void, ct);

        if (hasTaxes && !string.Equals(currency, entity.DutiesCurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("CUSTOMS_CURRENCY_LOCKED",
                "No puede cambiar la moneda consolidada después de registrar tributos.", 409);

        entity.CustomsRegimeId = regime?.CustomsRegimeId;
        entity.CustomsRegime = regime?.Name;
        entity.DeclarationNo = Clean(request.DeclarationNo)?.ToUpperInvariant();
        entity.DeclarationDate = request.DeclarationDate?.Date;
        entity.CustomsOfficeId = office?.CustomsOfficeId;
        entity.CustomsOffice = office?.Name;
        entity.DutiesCurrencyCode = currency;
        entity.DutiesExchangeRate = request.DutiesExchangeRate;
        entity.Observation = Clean(request.Observation);
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateTaxesAsync(entity, ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomsDetailDto?> StartReviewAsync(long id, CustomsActionRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        ApplyConcurrency(entity, request.RowVersion);

        if (entity.CustomsStatus != CustomsStatuses.Pending)
            throw new BusinessException("INVALID_CUSTOMS_TRANSITION",
                $"No se puede iniciar revisión desde {entity.CustomsStatus}.", 409);

        await EnsureOperationAtCustomsStageAsync(entity.OperationId, ct);
        ValidateDeclarationForReview(entity);

        await ChangeStatusAsync(
            entity,
            CustomsStatuses.InReview,
            Clean(request.Comment) ?? "Declaración ingresada a revisión aduanera.",
            "CUSTOMS_REVIEW_STARTED",
            "Revisión aduanera iniciada",
            ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomsDetailDto?> AssignChannelAsync(long id, AssignCustomsChannelRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        ApplyConcurrency(entity, request.RowVersion);

        if (entity.CustomsStatus is not (CustomsStatuses.InReview or CustomsStatuses.Observed))
            throw new BusinessException("CHANNEL_NOT_ALLOWED",
                "El canal solo puede asignarse durante IN_REVIEW/OBSERVED.", 409);

        var channel = NormalizeChannel(request.Channel);
        var previous = entity.CustomsChannel;

        entity.CustomsChannel = channel;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        AddOperationEvent(
            entity.OperationId,
            "CUSTOMS_CHANNEL_ASSIGNED",
            "Canal aduanero asignado",
            $"Canal: {channel}" + (previous is null ? "." : $" (anterior: {previous})."),
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomsObservationDto?> AddObservationAsync(long id, CreateCustomsObservationRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (entity.CustomsStatus is not (CustomsStatuses.InReview or CustomsStatuses.Observed))
            throw new BusinessException("OBSERVATION_NOT_ALLOWED",
                "Solo puede registrar observaciones durante IN_REVIEW/OBSERVED.", 409);

        await EnsureOperationAtCustomsStageAsync(entity.OperationId, ct);

        var type = NormalizeObservationType(request.ObservationType);
        var severity = NormalizeSeverity(request.Severity);

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new BusinessException(ErrorCodes.Validation, "La descripción de la observación es obligatoria.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var task = new OperationTask
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = entity.OperationId,
            WorkflowStageId = await GetCustomsStageIdAsync(ct),
            Title = $"Resolver observación aduanera: {type}",
            Description = request.Description.Trim(),
            TaskStatus = OperationTaskStatuses.Pending,
            Priority = severity switch
            {
                CustomsObservationSeverities.Critical => TaskPriorities.Critical,
                CustomsObservationSeverities.High => TaskPriorities.High,
                CustomsObservationSeverities.Low => TaskPriorities.Low,
                _ => TaskPriorities.Normal
            },
            AssignedUserId = (await db.ImportOperations.AsNoTracking()
                .Where(x => x.OperationId == entity.OperationId)
                .Select(x => (long?)x.ResponsibleUserId)
                .SingleAsync(ct)),
            IsMandatory = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        db.OperationTasks.Add(task);
        await db.SaveChangesAsync(ct);

        var observation = new CustomsObservation
        {
            CustomsProcessId = id,
            ObservationType = type,
            Severity = severity,
            Description = request.Description.Trim(),
            ObservationStatus = CustomsObservationStatuses.Open,
            ObservedAt = DateTime.UtcNow,
            ObservedBy = currentUser.UserId,
            OperationTaskId = task.OperationTaskId,
            CreatedAt = DateTime.UtcNow
        };

        db.CustomsObservations.Add(observation);

        if (entity.CustomsStatus != CustomsStatuses.Observed)
        {
            var old = entity.CustomsStatus;
            entity.CustomsStatus = CustomsStatuses.Observed;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser.UserId;

            db.CustomsStatusHistories.Add(new CustomsStatusHistory
            {
                CustomsProcessId = id,
                OldStatus = old,
                NewStatus = CustomsStatuses.Observed,
                Comment = request.Description.Trim(),
                ChangedAt = DateTime.UtcNow,
                ChangedBy = currentUser.UserId
            });
        }

        entity.Observation = request.Description.Trim();

        AddOperationEvent(
            entity.OperationId,
            "CUSTOMS_OBSERVED",
            "Observación aduanera",
            $"[{severity}] {request.Description.Trim()}",
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return MapObservation(observation);
    }

    public async Task<CustomsObservationDto?> ResolveObservationAsync(long id, long observationId, ResolveCustomsObservationRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        var observation = await db.CustomsObservations.SingleOrDefaultAsync(x =>
            x.CustomsProcessId == id && x.CustomsObservationId == observationId, ct);

        if (observation is null) return null;

        db.Entry(observation).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(request.RowVersion);

        if (observation.ObservationStatus != CustomsObservationStatuses.Open)
            throw new BusinessException("OBSERVATION_ALREADY_CLOSED",
                "La observación ya no se encuentra OPEN.", 409);

        if (string.IsNullOrWhiteSpace(request.ResolutionNote))
            throw new BusinessException(ErrorCodes.Validation, "Debe registrar cómo se resolvió la observación.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        observation.ObservationStatus = CustomsObservationStatuses.Resolved;
        observation.ResolvedAt = DateTime.UtcNow;
        observation.ResolvedBy = currentUser.UserId;
        observation.ResolutionNote = request.ResolutionNote.Trim();
        observation.ModifiedAt = DateTime.UtcNow;

        if (observation.OperationTaskId.HasValue)
        {
            var task = await db.OperationTasks.SingleOrDefaultAsync(x =>
                x.OperationTaskId == observation.OperationTaskId.Value &&
                x.OperationId == process.OperationId, ct);

            if (task is not null && task.TaskStatus != OperationTaskStatuses.Completed)
            {
                task.TaskStatus = OperationTaskStatuses.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = currentUser.UserId;
                task.ModifiedAt = DateTime.UtcNow;
                task.ModifiedBy = currentUser.UserId;
            }
        }

        await db.SaveChangesAsync(ct);

        var openCount = await db.CustomsObservations.AsNoTracking().CountAsync(x =>
            x.CustomsProcessId == id &&
            x.ObservationStatus == CustomsObservationStatuses.Open, ct);

        if (openCount == 0 && process.CustomsStatus == CustomsStatuses.Observed)
        {
            var old = process.CustomsStatus;
            process.CustomsStatus = CustomsStatuses.InReview;
            process.Observation = null;
            process.ModifiedAt = DateTime.UtcNow;
            process.ModifiedBy = currentUser.UserId;

            db.CustomsStatusHistories.Add(new CustomsStatusHistory
            {
                CustomsProcessId = id,
                OldStatus = old,
                NewStatus = CustomsStatuses.InReview,
                Comment = "Todas las observaciones aduaneras fueron resueltas.",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = currentUser.UserId
            });
        }

        AddOperationEvent(
            process.OperationId,
            "CUSTOMS_OBSERVATION_RESOLVED",
            "Observación aduanera resuelta",
            request.ResolutionNote.Trim(),
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return MapObservation(observation);
    }

    public async Task<CustomsTaxDto?> AddTaxAsync(long id, UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        EnsureTaxesEditable(process);
        var tax = await BuildTaxAsync(id, request, ct);

        db.CustomsTaxes.Add(tax);
        await db.SaveChangesAsync(ct);
        await RecalculateTaxesAsync(process, ct);

        AddOperationEvent(
            process.OperationId,
            "CUSTOMS_TAX_REGISTERED",
            "Tributo aduanero registrado",
            $"{tax.TaxType}: {tax.Amount:N2} {tax.CurrencyCode}.",
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        return MapTax(tax);
    }

    public async Task<CustomsTaxDto?> UpdateTaxAsync(long id, long taxId, UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        EnsureTaxesEditable(process);

        var tax = await db.CustomsTaxes.SingleOrDefaultAsync(x =>
            x.CustomsProcessId == id && x.CustomsTaxId == taxId, ct);

        if (tax is null) return null;

        if (tax.PaymentStatus == CustomsTaxPaymentStatuses.Paid)
            throw new BusinessException("PAID_TAX_LOCKED",
                "Un tributo PAID no puede cambiar monto/tipo. Anule y registre una nueva línea si corresponde.", 409);

        if (string.IsNullOrWhiteSpace(request.RowVersion))
            throw new BusinessException(ErrorCodes.Validation, "rowVersion es obligatorio para actualizar el tributo.", 400);

        db.Entry(tax).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(request.RowVersion);

        await ApplyTaxAsync(tax, request, ct);
        tax.ModifiedAt = DateTime.UtcNow;
        tax.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateTaxesAsync(process, ct);

        return MapTax(tax);
    }

    public async Task<CustomsTaxDto?> ChangeTaxStatusAsync(long id, long taxId, CustomsTaxStatusRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        EnsureTaxesEditable(process);

        var tax = await db.CustomsTaxes.SingleOrDefaultAsync(x =>
            x.CustomsProcessId == id && x.CustomsTaxId == taxId, ct);

        if (tax is null) return null;

        db.Entry(tax).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(request.RowVersion);

        var status = NormalizeTaxPaymentStatus(request.PaymentStatus);

        if (status == CustomsTaxPaymentStatuses.Paid)
        {
            if (tax.Amount > 0 && string.IsNullOrWhiteSpace(request.PaymentReference))
                throw new BusinessException("PAYMENT_REFERENCE_REQUIRED",
                    "Debe registrar referencia de pago para marcar el tributo como PAID.", 409);

            tax.PaymentReference = Clean(request.PaymentReference);
            tax.PaidAt = (request.PaidAt ?? DateTime.UtcNow).ToUniversalTime();
        }
        else if (status == CustomsTaxPaymentStatuses.Exempt)
        {
            tax.PaymentReference = Clean(request.PaymentReference);
            tax.PaidAt = null;
        }
        else if (status == CustomsTaxPaymentStatuses.Void)
        {
            tax.PaymentReference = Clean(request.PaymentReference);
            tax.PaidAt = null;
        }
        else
        {
            tax.PaymentReference = null;
            tax.PaidAt = null;
        }

        tax.PaymentStatus = status;
        tax.ModifiedAt = DateTime.UtcNow;
        tax.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateTaxesAsync(process, ct);

        AddOperationEvent(
            process.OperationId,
            "CUSTOMS_TAX_STATUS_CHANGED",
            "Estado de tributo actualizado",
            $"{tax.TaxType}: {status}" + (string.IsNullOrWhiteSpace(request.Comment) ? "." : $". {request.Comment.Trim()}"),
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        return MapTax(tax);
    }

    public async Task<CustomsDetailDto?> ReleaseAsync(long id, ReleaseCustomsRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        ApplyConcurrency(process, request.RowVersion);

        if (process.CustomsStatus != CustomsStatuses.InReview)
            throw new BusinessException("INVALID_CUSTOMS_TRANSITION",
                $"El levante solo puede registrarse desde IN_REVIEW. Estado actual: {process.CustomsStatus}.", 409);

        await EnsureOperationAtCustomsStageAsync(process.OperationId, ct);
        ValidateDeclarationForRelease(process);

        if (request.ReleaseDate == default)
            throw new BusinessException(ErrorCodes.Validation, "ReleaseDate es obligatorio.", 400);

        var releaseUtc = request.ReleaseDate.ToUniversalTime();

        if (process.DeclarationDate.HasValue &&
            releaseUtc.Date < process.DeclarationDate.Value.Date)
            throw new BusinessException("INVALID_RELEASE_DATE",
                "La fecha de levante no puede ser anterior a la fecha de declaración.", 409);

        var openObservations = await db.CustomsObservations.AsNoTracking().CountAsync(x =>
            x.CustomsProcessId == id &&
            x.ObservationStatus == CustomsObservationStatuses.Open, ct);

        if (openObservations > 0)
            throw new BusinessException("OPEN_CUSTOMS_OBSERVATIONS",
                $"No puede registrar levante: existen {openObservations} observación(es) abiertas.", 409);

        var unpaidTaxes = await db.CustomsTaxes.AsNoTracking().CountAsync(x =>
            x.CustomsProcessId == id &&
            x.Amount > 0 &&
            x.PaymentStatus != CustomsTaxPaymentStatuses.Paid &&
            x.PaymentStatus != CustomsTaxPaymentStatuses.Exempt &&
            x.PaymentStatus != CustomsTaxPaymentStatuses.Void, ct);

        if (unpaidTaxes > 0)
            throw new BusinessException("UNPAID_CUSTOMS_TAXES",
                $"No puede registrar levante: existen {unpaidTaxes} tributo(s) pendientes de pago/exoneración.", 409);

        process.ReleaseDate = releaseUtc;

        await ChangeStatusAsync(
            process,
            CustomsStatuses.Released,
            Clean(request.Comment) ?? $"Levante autorizado el {releaseUtc:yyyy-MM-dd HH:mm} UTC.",
            "CUSTOMS_RELEASED",
            "Levante aduanero autorizado",
            ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomsDetailDto?> CancelAsync(long id, CancelCustomsRequest request, CancellationToken ct)
    {
        var process = await GetOwnedTrackedAsync(id, ct);
        if (process is null) return null;

        ApplyConcurrency(process, request.RowVersion);

        if (process.CustomsStatus is CustomsStatuses.Released or CustomsStatuses.Cancelled)
            throw new BusinessException("CUSTOMS_CANCEL_NOT_ALLOWED",
                $"No se puede cancelar un proceso {process.CustomsStatus}.", 409);

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessException(ErrorCodes.Validation, "El motivo de cancelación es obligatorio.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var openObservations = await db.CustomsObservations
            .Where(x => x.CustomsProcessId == id &&
                        x.ObservationStatus == CustomsObservationStatuses.Open)
            .ToListAsync(ct);

        foreach (var observation in openObservations)
        {
            observation.ObservationStatus = CustomsObservationStatuses.Cancelled;
            observation.ModifiedAt = DateTime.UtcNow;

            if (observation.OperationTaskId.HasValue)
            {
                var task = await db.OperationTasks.SingleOrDefaultAsync(x =>
                    x.OperationTaskId == observation.OperationTaskId.Value, ct);

                if (task is not null && task.TaskStatus != OperationTaskStatuses.Completed)
                {
                    task.TaskStatus = OperationTaskStatuses.Cancelled;
                    task.ModifiedAt = DateTime.UtcNow;
                    task.ModifiedBy = currentUser.UserId;
                }
            }
        }

        await ChangeStatusAsync(
            process,
            CustomsStatuses.Cancelled,
            request.Reason.Trim(),
            "CUSTOMS_CANCELLED",
            "Proceso aduanero cancelado",
            ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task<CustomsTax> BuildTaxAsync(long processId, UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var tax = new CustomsTax
        {
            CustomsProcessId = processId,
            PaymentStatus = CustomsTaxPaymentStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        await ApplyTaxAsync(tax, request, ct);
        return tax;
    }

    private async Task ApplyTaxAsync(CustomsTax tax, UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var process = await db.CustomsProcesses.AsNoTracking()
            .SingleAsync(x => x.CustomsProcessId == tax.CustomsProcessId, ct);

        var concept = await db.CustomsTaxConcepts.AsNoTracking().SingleOrDefaultAsync(x => x.CustomsTaxConceptId == request.CustomsTaxConceptId && x.IsActive, ct)
            ?? throw new BusinessException(ErrorCodes.Validation, "Concepto de tributo aduanero inválido.", 400);

        if (request.Amount < 0)
            throw new BusinessException(ErrorCodes.Validation, "El monto del tributo no puede ser negativo.", 400);

        var currency = request.CurrencyCode.Trim().ToUpperInvariant();
        await ValidateCurrencyAsync(currency, ct);

        if (!string.Equals(currency, process.DutiesCurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("CUSTOMS_TAX_CURRENCY_MISMATCH",
                $"Todos los tributos del proceso deben registrarse en {process.DutiesCurrencyCode}.", 409);

        ValidateExchangeRate(request.ExchangeRate);

        var baseAmount = request.ExchangeRate.HasValue
            ? RoundMoney(request.Amount * request.ExchangeRate.Value)
            : RoundMoney(request.Amount);

        tax.CustomsTaxConceptId = concept.CustomsTaxConceptId;
        tax.TaxType = concept.Code;
        tax.Description = concept.Name;
        tax.Amount = RoundMoney(request.Amount);
        tax.CurrencyCode = currency;
        tax.ExchangeRate = request.ExchangeRate;
        tax.BaseAmount = baseAmount;
        tax.Notes = Clean(request.Notes);
    }

    private async Task RecalculateTaxesAsync(CustomsProcess process, CancellationToken ct)
    {
        var totals = await db.CustomsTaxes.AsNoTracking()
            .Where(x => x.CustomsProcessId == process.CustomsProcessId &&
                        x.PaymentStatus != CustomsTaxPaymentStatuses.Void)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Amount = g.Sum(x => x.Amount),
                BaseAmount = g.Sum(x => x.BaseAmount)
            })
            .SingleOrDefaultAsync(ct);

        process.DutiesAmount = totals?.Amount ?? 0;
        process.DutiesBaseAmount = totals?.BaseAmount ?? 0;
        process.ModifiedAt = DateTime.UtcNow;
        process.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
    }

    private async Task ChangeStatusAsync(
        CustomsProcess process,
        string newStatus,
        string comment,
        string eventType,
        string eventTitle,
        CancellationToken ct)
    {
        var oldStatus = process.CustomsStatus;

        process.CustomsStatus = newStatus;
        process.ModifiedAt = DateTime.UtcNow;
        process.ModifiedBy = currentUser.UserId;

        db.CustomsStatusHistories.Add(new CustomsStatusHistory
        {
            CustomsProcessId = process.CustomsProcessId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = comment,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        AddOperationEvent(
            process.OperationId,
            eventType,
            eventTitle,
            comment,
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
    }

    private void AddOperationEvent(long operationId, string eventType, string title, string description, DateTime eventAt)
    {
        db.OperationEvents.Add(new OperationEvent
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = operationId,
            EventType = eventType,
            EventDate = eventAt,
            Title = title,
            Description = description,
            SourceType = OperationEventSources.System,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        });
    }

    private async Task<Shipment> ResolveShipmentAsync(long operationId, long? shipmentId, CancellationToken ct)
    {
        if (shipmentId.HasValue)
        {
            return await db.Shipments.SingleOrDefaultAsync(x =>
                x.ShipmentId == shipmentId.Value &&
                x.OperationId == operationId &&
                x.CompanyId == currentCompany.CompanyId &&
                !x.IsDeleted, ct)
                ?? throw new BusinessException(ErrorCodes.Validation,
                    "El Shipment no existe o no pertenece a la operación.", 400);
        }

        var arrived = await db.Shipments
            .Where(x =>
                x.OperationId == operationId &&
                x.CompanyId == currentCompany.CompanyId &&
                !x.IsDeleted &&
                x.ShipmentStatus == ShipmentStatuses.Arrived)
            .OrderBy(x => x.ShipmentId)
            .ToListAsync(ct);

        if (arrived.Count == 0)
            throw new BusinessException("SHIPMENT_NOT_ARRIVED",
                "No existe un embarque ARRIVED para iniciar aduanas.", 409);

        if (arrived.Count > 1)
            throw new BusinessException("SHIPMENT_REQUIRED",
                "La operación tiene múltiples embarques arribados. Debe indicar ShipmentId.", 409);

        return arrived[0];
    }

    private async Task EnsureOperationAtCustomsStageAsync(long operationId, CancellationToken ct)
    {
        var stageCode = await (
            from operationRow in db.ImportOperations.AsNoTracking()
            join s in db.WorkflowStages.AsNoTracking() on operationRow.CurrentStageId equals s.WorkflowStageId
            where operationRow.OperationId == operationId && operationRow.CompanyId == currentCompany.CompanyId
            select s.Code)
            .SingleOrDefaultAsync(ct);

        if (!string.Equals(stageCode, "CUSTOMS", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("OPERATION_NOT_IN_CUSTOMS_STAGE",
                "La operación debe encontrarse en la etapa CUSTOMS para ejecutar esta acción.", 409);
    }

    private async Task<int> GetCustomsStageIdAsync(CancellationToken ct) =>
        await db.WorkflowStages.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId &&
                        x.Code == "CUSTOMS" &&
                        x.IsActive)
            .Select(x => x.WorkflowStageId)
            .SingleAsync(ct);

    private static void ValidateDeclarationForReview(CustomsProcess process)
    {
        if (!process.CustomsRegimeId.HasValue)
            throw new BusinessException("CUSTOMS_REGIME_REQUIRED", "Debe registrar régimen aduanero.", 409);

        if (string.IsNullOrWhiteSpace(process.DeclarationNo))
            throw new BusinessException("DECLARATION_REQUIRED", "Debe registrar número de declaración/DAM.", 409);

        if (!process.DeclarationDate.HasValue)
            throw new BusinessException("DECLARATION_DATE_REQUIRED", "Debe registrar fecha de numeración/declaración.", 409);

        if (!process.CustomsOfficeId.HasValue)
            throw new BusinessException("CUSTOMS_OFFICE_REQUIRED", "Debe registrar la aduana/oficina aduanera.", 409);
    }

    private static void ValidateDeclarationForRelease(CustomsProcess process)
    {
        ValidateDeclarationForReview(process);

        if (string.IsNullOrWhiteSpace(process.CustomsChannel))
            throw new BusinessException("CUSTOMS_CHANNEL_REQUIRED",
                "Debe registrar canal aduanero antes del levante.", 409);
    }

    private static void EnsureTaxesEditable(CustomsProcess process)
    {
        if (process.CustomsStatus is CustomsStatuses.Released or CustomsStatuses.Cancelled)
            throw new BusinessException("CUSTOMS_TAXES_LOCKED",
                "Los tributos no pueden modificarse después de RELEASED/CANCELLED.", 409);
    }

    private void ApplyConcurrency(CustomsProcess process, string rowVersion)
    {
        db.Entry(process).Property(x => x.RowVer).OriginalValue =
            ConcurrencyToken.Decode(rowVersion);
    }

    private async Task<CustomsRegime?> ResolveRegimeAsync(int? id, CancellationToken ct)
    {
        if (!id.HasValue) return null;
        return await db.CustomsRegimes.AsNoTracking().SingleOrDefaultAsync(x=>x.CustomsRegimeId==id.Value && x.IsActive,ct)
            ?? throw new BusinessException(ErrorCodes.Validation,"Régimen aduanero inválido.",400);
    }

    private async Task<CustomsOfficeCatalog?> ResolveOfficeAsync(int? id, CancellationToken ct)
    {
        if (!id.HasValue) return null;
        return await db.CustomsOfficeCatalogs.AsNoTracking().SingleOrDefaultAsync(x=>x.CustomsOfficeId==id.Value && x.IsActive,ct)
            ?? throw new BusinessException(ErrorCodes.Validation,"Aduana inválida.",400);
    }

    private async Task ValidateCurrencyAsync(string? currencyCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return;

        var currency = currencyCode.Trim().ToUpperInvariant();
        if (!await db.Currencies.AsNoTracking().AnyAsync(x =>
            x.CurrencyCode == currency && x.IsActive, ct))
            throw new BusinessException(ErrorCodes.Validation, "Moneda de tributos inválida.", 400);
    }

    private static void ValidateExchangeRate(decimal? exchangeRate)
    {
        if (exchangeRate.HasValue && exchangeRate.Value <= 0)
            throw new BusinessException(ErrorCodes.Validation,
                "El tipo de cambio debe ser mayor a cero.", 400);
    }

    private async Task<CustomsProcess?> GetOwnedTrackedAsync(long id, CancellationToken ct) =>
        await db.CustomsProcesses.SingleOrDefaultAsync(x =>
            x.CustomsProcessId == id &&
            x.CompanyId == currentCompany.CompanyId &&
            (!currentUser.IsClientUser || x.Operation.ClientId == currentUser.ClientId), ct);

    private async Task<ImportOperation> GetOperationAsync(long operationId, CancellationToken ct) =>
        await db.ImportOperations.SingleOrDefaultAsync(x =>
            x.OperationId == operationId &&
            x.CompanyId == currentCompany.CompanyId &&
            (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct)
        ?? throw new BusinessException(ErrorCodes.NotFound, "La operación no existe.", 404);

    private async Task EnsureOperationOwnedAsync(long operationId, CancellationToken ct)
    {
        if (!await db.ImportOperations.AsNoTracking().AnyAsync(x =>
            x.OperationId == operationId &&
            x.CompanyId == currentCompany.CompanyId &&
            (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct))
            throw new BusinessException(ErrorCodes.NotFound, "La operación no existe.", 404);
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (!ValidStatuses.Contains(normalized))
            throw new BusinessException(ErrorCodes.Validation, "Estado Customs inválido.", 400);
        return normalized;
    }

    private static string NormalizeChannel(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is not (
            CustomsChannels.Green or
            CustomsChannels.Yellow or
            CustomsChannels.Red or
            CustomsChannels.Other))
            throw new BusinessException(ErrorCodes.Validation, "Canal aduanero inválido.", 400);
        return normalized;
    }

    private static string NormalizeObservationType(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is not (
            CustomsObservationTypes.Documentary or
            CustomsObservationTypes.Valuation or
            CustomsObservationTypes.Classification or
            CustomsObservationTypes.Inspection or
            CustomsObservationTypes.AuthorityRequirement or
            CustomsObservationTypes.Other))
            throw new BusinessException(ErrorCodes.Validation, "Tipo de observación aduanera inválido.", 400);
        return normalized;
    }

    private static string NormalizeSeverity(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is not (
            CustomsObservationSeverities.Low or
            CustomsObservationSeverities.Normal or
            CustomsObservationSeverities.High or
            CustomsObservationSeverities.Critical))
            throw new BusinessException(ErrorCodes.Validation, "Severidad inválida.", 400);
        return normalized;
    }

    private static string NormalizeTaxPaymentStatus(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is not (
            CustomsTaxPaymentStatuses.Pending or
            CustomsTaxPaymentStatuses.Paid or
            CustomsTaxPaymentStatuses.Exempt or
            CustomsTaxPaymentStatuses.Void))
            throw new BusinessException(ErrorCodes.Validation, "Estado de pago de tributo inválido.", 400);
        return normalized;
    }

    private static CustomsObservationDto MapObservation(CustomsObservation x) =>
        new(
            x.CustomsObservationId,
            x.ObservationType,
            x.Severity,
            x.Description,
            x.ObservationStatus,
            x.ObservedAt,
            x.ObservedBy,
            x.ResolvedAt,
            x.ResolvedBy,
            x.ResolutionNote,
            x.OperationTaskId,
            ConcurrencyToken.Encode(x.RowVer));

    private static CustomsTaxDto MapTax(CustomsTax x) =>
        new(
            x.CustomsTaxId,
            x.CustomsTaxConceptId,
            x.TaxType,
            x.Description,
            x.Amount,
            x.CurrencyCode,
            x.ExchangeRate,
            x.BaseAmount,
            x.PaymentStatus,
            x.PaymentReference,
            x.PaidAt,
            x.Notes,
            ConcurrencyToken.Encode(x.RowVer));

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
