using System.Data;
using ImportOps.Application.Common;
using ImportOps.Application.Operations;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class ImportOperationService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : IImportOperationService
{
    private static readonly HashSet<string> ValidOperationalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        OperationStatuses.Open, OperationStatuses.OnHold, OperationStatuses.InProgress,
        OperationStatuses.Delivered, OperationStatuses.Cancelled, OperationStatuses.Closed
    };

    private static readonly HashSet<string> ValidDocumentaryStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        DocumentaryStatuses.Pending, DocumentaryStatuses.Observed, DocumentaryStatuses.Complete
    };

    private static readonly HashSet<string> ValidFinancialStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        FinancialStatuses.NoMovements, FinancialStatuses.Funded, FinancialStatuses.InExecution,
        FinancialStatuses.PendingCollection, FinancialStatuses.PendingSettlement,
        FinancialStatuses.PendingRefund, FinancialStatuses.Closed
    };

    private static readonly Dictionary<string, HashSet<string>> TaskTransitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [OperationTaskStatuses.Pending] = new(StringComparer.OrdinalIgnoreCase)
            {
                OperationTaskStatuses.InProgress, OperationTaskStatuses.Blocked,
                OperationTaskStatuses.Completed, OperationTaskStatuses.Cancelled
            },
            [OperationTaskStatuses.InProgress] = new(StringComparer.OrdinalIgnoreCase)
            {
                OperationTaskStatuses.Blocked, OperationTaskStatuses.Completed, OperationTaskStatuses.Cancelled
            },
            [OperationTaskStatuses.Blocked] = new(StringComparer.OrdinalIgnoreCase)
            {
                OperationTaskStatuses.InProgress, OperationTaskStatuses.Completed, OperationTaskStatuses.Cancelled
            },
            [OperationTaskStatuses.Completed] = new(StringComparer.OrdinalIgnoreCase),
            [OperationTaskStatuses.Cancelled] = new(StringComparer.OrdinalIgnoreCase)
        };

    public async Task<PagedResult<OperationListItemDto>> GetAsync(OperationQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.ImportOperations.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId);

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            if (query.ClientId.HasValue && query.ClientId.Value != currentUser.ClientId.Value)
                throw new BusinessException(
                    ErrorCodes.Forbidden,
                    "No puede consultar operaciones pertenecientes a otro cliente.",
                    403);

            q = q.Where(x => x.ClientId == currentUser.ClientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x =>
                x.OperationNo.Contains(term) ||
                x.MerchandiseDescription.Contains(term) ||
                x.Client.LegalName.Contains(term) ||
                (x.QuotationNoSnapshot != null && x.QuotationNoSnapshot.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.OperationalStatus))
        {
            var s = NormalizeOperationalStatus(query.OperationalStatus);
            q = q.Where(x => x.OperationalStatus == s);
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentaryStatus))
        {
            var s = NormalizeDocumentaryStatus(query.DocumentaryStatus);
            q = q.Where(x => x.DocumentaryStatus == s);
        }

        if (!string.IsNullOrWhiteSpace(query.FinancialStatus))
        {
            var s = NormalizeFinancialStatus(query.FinancialStatus);
            q = q.Where(x => x.FinancialStatus == s);
        }

        if (query.StageId.HasValue) q = q.Where(x => x.CurrentStageId == query.StageId.Value);
        if (query.ClientId.HasValue) q = q.Where(x => x.ClientId == query.ClientId.Value);
        if (query.QuotationId.HasValue) q = q.Where(x => x.QuotationId == query.QuotationId.Value);
        if (query.ResponsibleUserId.HasValue) q = q.Where(x => x.ResponsibleUserId == query.ResponsibleUserId.Value);

        if (query.OpenedFrom.HasValue)
        {
            var from = query.OpenedFrom.Value.Date;
            q = q.Where(x => x.OpenedAt >= from);
        }

        if (query.OpenedTo.HasValue)
        {
            var toExclusive = query.OpenedTo.Value.Date.AddDays(1);
            q = q.Where(x => x.OpenedAt < toExclusive);
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.OperationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.OperationId, x.OperationNo, x.ClientId,
                ClientName = x.Client.LegalName,
                StageCode = x.CurrentStage.Code,
                StageName = x.CurrentStage.Name,
                x.OperationalStatus, x.DocumentaryStatus, x.FinancialStatus,
                x.ResponsibleUserId,
                ResponsibleFirstName = x.ResponsibleUser.FirstName,
                ResponsibleLastName = x.ResponsibleUser.LastName,
                ResponsibleUsername = x.ResponsibleUser.Username,
                x.OpenedAt, x.EstimatedCloseDate
            })
            .ToListAsync(ct);

        var items = rows.Select(x => new OperationListItemDto(
            x.OperationId, x.OperationNo, x.ClientId, x.ClientName,
            x.StageCode, x.StageName, x.OperationalStatus, x.DocumentaryStatus,
            currentUser.IsClientUser ? null : x.FinancialStatus, x.ResponsibleUserId,
            FormatUser(x.ResponsibleFirstName, x.ResponsibleLastName, x.ResponsibleUsername),
            x.OpenedAt, x.EstimatedCloseDate)).ToList();

        return new(items, page, pageSize, total);
    }

    public async Task<OperationDetailDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var row = await (
            from operationRow in db.ImportOperations.AsNoTracking()
            join r0 in db.ImportRequests.AsNoTracking() on operationRow.RequestId equals r0.RequestId into rj
            from r in rj.DefaultIfEmpty()
            join q0 in db.Quotations.AsNoTracking() on operationRow.QuotationId equals q0.QuotationId into qj
            from q in qj.DefaultIfEmpty()
            join tm0 in db.TransportModes.AsNoTracking() on operationRow.TransportModeId equals tm0.TransportModeId into tmj
            from tm in tmj.DefaultIfEmpty()
            join oc0 in db.Countries.AsNoTracking() on operationRow.OriginCountryCode equals oc0.CountryCode into ocj
            from oc in ocj.DefaultIfEmpty()
            join dc0 in db.Countries.AsNoTracking() on operationRow.DestinationCountryCode equals dc0.CountryCode into dcj
            from dc in dcj.DefaultIfEmpty()
            join op0 in db.Ports.AsNoTracking() on operationRow.OriginPortId equals op0.PortId into opj
            from op in opj.DefaultIfEmpty()
            join dp0 in db.Ports.AsNoTracking() on operationRow.DestinationPortId equals dp0.PortId into dpj
            from dp in dpj.DefaultIfEmpty()
            where operationRow.CompanyId == currentCompany.CompanyId && operationRow.OperationId == id && (!currentUser.IsClientUser || operationRow.ClientId == currentUser.ClientId)
            select new
            {
                o = operationRow,
                RequestNo = r != null ? r.RequestNo : null,
                QuotationNo = q != null ? q.QuotationNo : operationRow.QuotationNoSnapshot,
                QuotationVersion = q != null ? (int?)q.VersionNo : operationRow.QuotationVersionSnapshot,
                ClientLiveName = operationRow.Client.LegalName,
                ContactLiveName = operationRow.ClientContact != null ? operationRow.ClientContact.FullName : null,
                SupplierLiveName = operationRow.Supplier != null ? operationRow.Supplier.LegalName : null,
                RespFirst = operationRow.ResponsibleUser.FirstName,
                RespLast = operationRow.ResponsibleUser.LastName,
                RespUsername = operationRow.ResponsibleUser.Username,
                StageCode = operationRow.CurrentStage.Code,
                StageName = operationRow.CurrentStage.Name,
                TransportName = tm != null ? tm.Name : null,
                OriginCountryName = oc != null ? oc.CountryName : null,
                DestinationCountryName = dc != null ? dc.CountryName : null,
                OriginPortName = op != null ? op.PortName : null,
                DestinationPortName = dp != null ? dp.PortName : null
            }).SingleOrDefaultAsync(ct);

        if (row is null) return null;
        var o = row.o;

        return new OperationDetailDto(
            o.OperationId, o.OperationNo,
            o.RequestId, row.RequestNo,
            o.QuotationId, row.QuotationNo, row.QuotationVersion,
            o.ClientId, row.ClientLiveName,
            o.ClientContactId, row.ContactLiveName,
            o.SupplierId, row.SupplierLiveName,
            o.ResponsibleUserId, FormatUser(row.RespFirst, row.RespLast, row.RespUsername),
            o.CurrentStageId, row.StageCode, row.StageName,
            o.OperationalStatus, o.DocumentaryStatus, currentUser.IsClientUser ? null : o.FinancialStatus,
            o.TransportModeId, row.TransportName,
            o.IncotermCode,
            o.OriginCountryCode, row.OriginCountryName, o.OriginPortId, row.OriginPortName,
            o.DestinationCountryCode, row.DestinationCountryName, o.DestinationPortId, row.DestinationPortName,
            o.MerchandiseDescription, o.PackageCount, o.GrossWeightKg, o.NetWeightKg, o.VolumeM3,
            o.DeclaredValue, o.DeclaredCurrencyCode,
            o.QuotationCurrencySnapshot, o.QuotationTotalSnapshot,
            o.OpenedAt, o.EstimatedCloseDate, o.ActualCloseDate, o.ClosedAt,
            o.CancellationReason, currentUser.IsClientUser ? null : o.Notes, o.CreatedAt, o.ModifiedAt,
            ConcurrencyToken.Encode(o.RowVer),
            new OperationSourceSnapshotDto(
                o.ClientNameSnapshot,
                o.ClientContactNameSnapshot,
                o.SupplierNameSnapshot,
                o.QuotationNoSnapshot,
                o.QuotationVersionSnapshot,
                o.QuotationCurrencySnapshot,
                o.QuotationTotalSnapshot));
    }

    public async Task<OperationDetailDto> CreateFromQuotationAsync(long quotationId, CreateOperationFromQuotationRequest request, CancellationToken ct)
    {
        await EnsureCurrentUserAsync(ct);

        var quotation = await db.Quotations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.QuotationId == quotationId, ct)
            ?? throw new ArgumentException("La cotización no existe o pertenece a otra empresa.");

        if (!string.Equals(quotation.Status, QuotationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La operación solo puede crearse desde una cotización APPROVED.");

        var importRequest = await db.ImportRequests.AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.ClientContact)
            .Include(x => x.Supplier)
            .SingleOrDefaultAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.RequestId == quotation.RequestId, ct)
            ?? throw new InvalidOperationException("No se encontró la solicitud origen.");

        if (!string.Equals(importRequest.Status, ImportRequestStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La solicitud origen debe estar APPROVED.");

        if (await db.ImportOperations.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.QuotationId == quotationId &&
            !x.IsDeleted, ct))
            throw new InvalidOperationException("Ya existe una operación para esta cotización.");

        if (await db.ImportOperations.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.RequestId == importRequest.RequestId &&
            !x.IsDeleted, ct))
            throw new InvalidOperationException("Ya existe una operación para esta solicitud.");

        var responsibleId = request.ResponsibleUserId;

        if (!responsibleId.HasValue && importRequest.AssignedUserId.HasValue &&
            await IsOperationResponsibleAsync(importRequest.AssignedUserId.Value, ct))
            responsibleId = importRequest.AssignedUserId.Value;

        if (!responsibleId.HasValue && await IsOperationResponsibleAsync(currentUser.UserId, ct))
            responsibleId = currentUser.UserId;

        if (!responsibleId.HasValue)
            throw new BusinessException(
                ErrorCodes.Validation,
                "Debe seleccionar un responsable operativo activo.",
                400);

        await EnsureOperationResponsibleAsync(responsibleId.Value, ct);

        var firstStage = await db.WorkflowStages.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No existe workflow activo para la empresa.");

        if (!string.Equals(firstStage.Code, "DOCUMENTATION", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La primera etapa activa del workflow debe ser DOCUMENTATION.");

        var sequence = await NextOperationSequenceAsync(ct);
        var operationNo = $"IMP-{DateTime.Today:yyyy}-{sequence:000000}";

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new ImportOperation
        {
            CompanyId = currentCompany.CompanyId,
            OperationNo = operationNo,
            RequestId = importRequest.RequestId,
            QuotationId = quotation.QuotationId,
            ClientId = importRequest.ClientId,
            ClientContactId = importRequest.ClientContactId,
            SupplierId = importRequest.SupplierId,
            ResponsibleUserId = responsibleId.Value,

            ClientNameSnapshot = importRequest.Client.LegalName,
            ClientContactNameSnapshot = importRequest.ClientContact?.FullName,
            SupplierNameSnapshot = importRequest.Supplier?.LegalName,
            QuotationNoSnapshot = quotation.QuotationNo,
            QuotationVersionSnapshot = quotation.VersionNo,
            QuotationCurrencySnapshot = quotation.CurrencyCode,
            QuotationTotalSnapshot = quotation.TotalAmount,

            CurrentStageId = firstStage.WorkflowStageId,
            OperationalStatus = OperationStatuses.Open,
            DocumentaryStatus = DocumentaryStatuses.Pending,
            FinancialStatus = FinancialStatuses.NoMovements,

            TransportModeId = importRequest.TransportModeId,
            IncotermCode = importRequest.IncotermCode,
            OriginCountryCode = importRequest.OriginCountryCode,
            DestinationCountryCode = importRequest.DestinationCountryCode,
            OriginPortId = importRequest.OriginPortId,
            DestinationPortId = importRequest.DestinationPortId,

            MerchandiseDescription = importRequest.MerchandiseDescription,
            PackageCount = importRequest.PackageCount,
            GrossWeightKg = importRequest.GrossWeightKg,
            VolumeM3 = importRequest.VolumeM3,
            DeclaredValue = importRequest.DeclaredValue,
            DeclaredCurrencyCode = importRequest.DeclaredCurrencyCode,

            OpenedAt = DateTime.UtcNow,
            EstimatedCloseDate = request.EstimatedCloseDate?.Date,
            Notes = Clean(request.Notes),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        db.ImportOperations.Add(entity);
        await db.SaveChangesAsync(ct);

        db.OperationStageHistories.Add(new OperationStageHistory
        {
            OperationId = entity.OperationId,
            FromStageId = null,
            ToStageId = firstStage.WorkflowStageId,
            Comment = "Apertura de operación.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        AddStatusHistory(entity.OperationId, "OPERATIONAL", null, OperationStatuses.Open, "Operación creada.");
        AddStatusHistory(entity.OperationId, "DOCUMENTARY", null, DocumentaryStatuses.Pending, "Estado documentario inicial.");
        AddStatusHistory(entity.OperationId, "FINANCIAL", null, FinancialStatuses.NoMovements, "Estado económico inicial.");

        AddSystemEvent(entity.OperationId, OperationEventTypes.OperationCreated, "Operación creada",
            $"Operación {entity.OperationNo} creada desde {quotation.QuotationNo} V{quotation.VersionNo}.");

        await db.SaveChangesAsync(ct);
        await GenerateStageTasksAsync(entity, firstStage.WorkflowStageId, ct);

        // Presupuesto inicial: snapshot económico de la cotización aprobada.
        var quoteItems = await db.QuotationItems.AsNoTracking()
            .Where(x => x.QuotationId == quotation.QuotationId)
            .OrderBy(x => x.LineNo)
            .ToListAsync(ct);

        var initialBudget = new OperationBudget
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = entity.OperationId,
            VersionNo = 1,
            BudgetStatus = FinanceStatuses.Approved,
            BaseCurrencyCode = quotation.CurrencyCode,
            TotalEstimatedCost = quoteItems.Where(x => x.ItemType != "SERVICE").Sum(x => x.LineTotal),
            TotalEstimatedService = quoteItems.Where(x => x.ItemType == "SERVICE").Sum(x => x.LineTotal),
            TotalEstimatedAmount = quoteItems.Sum(x => x.LineTotal),
            ApprovedAt = DateTime.UtcNow,
            ApprovedByUserId = currentUser.UserId,
            Notes = $"Generado automáticamente desde {quotation.QuotationNo} V{quotation.VersionNo}.",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };
        db.OperationBudgets.Add(initialBudget);
        await db.SaveChangesAsync(ct);

        foreach (var q in quoteItems)
        {
            var itemType = q.ItemType == "SERVICE" ? "SERVICE" : "COST";
            db.OperationBudgetItems.Add(new OperationBudgetItem
            {
                OperationBudgetId = initialBudget.OperationBudgetId,
                ItemType = itemType,
                ExpenseCategoryId = q.ExpenseCategoryId,
                ServiceCategoryId = q.ServiceCategoryId,
                Description = q.Description,
                CurrencyCode = quotation.CurrencyCode,
                EstimatedAmount = q.LineTotal,
                EstimatedBaseAmount = q.LineTotal,
                IsRebillable = q.IsRebillable,
                Notes = q.Notes
            });

            if (itemType == "SERVICE" && q.ServiceCategoryId.HasValue)
            {
                db.OperationServices.Add(new OperationServiceCharge
                {
                    CompanyId = currentCompany.CompanyId,
                    OperationId = entity.OperationId,
                    ServiceCategoryId = q.ServiceCategoryId.Value,
                    ServiceStatus = "PENDING",
                    Description = q.Description,
                    CurrencyCode = quotation.CurrencyCode,
                    Amount = q.LineTotal,
                    BaseAmount = q.LineTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser.UserId
                });
            }
        }

        AddSystemEvent(entity.OperationId, "FINANCE_BUDGET_CREATED", "Presupuesto inicial creado",
            $"Presupuesto generado desde cotización aprobada: {initialBudget.TotalEstimatedAmount:N2} {initialBudget.BaseCurrencyCode}.");

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await GetByIdAsync(entity.OperationId, ct))!;
    }

    public async Task<OperationDetailDto?> UpdateAsync(long id, UpdateOperationRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        db.Entry(entity).Property(x => x.RowVer).OriginalValue = ConcurrencyToken.Decode(request.RowVersion);
        EnsureEditable(entity);

        if (string.IsNullOrWhiteSpace(request.MerchandiseDescription))
            throw new ArgumentException("La descripción de mercancía es obligatoria.");

        ValidateNonNegative(request.PackageCount, nameof(request.PackageCount));
        ValidateNonNegative(request.GrossWeightKg, nameof(request.GrossWeightKg));
        ValidateNonNegative(request.NetWeightKg, nameof(request.NetWeightKg));
        ValidateNonNegative(request.VolumeM3, nameof(request.VolumeM3));
        ValidateNonNegative(request.DeclaredValue, nameof(request.DeclaredValue));

        await EnsureOperationResponsibleAsync(request.ResponsibleUserId, ct);
        await ValidateOperationReferencesAsync(entity.ClientId, request, ct);

        entity.ClientContactId = request.ClientContactId;
        entity.SupplierId = request.SupplierId;
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.TransportModeId = request.TransportModeId;
        entity.IncotermCode = Clean(request.IncotermCode)?.ToUpperInvariant();
        entity.OriginCountryCode = Clean(request.OriginCountryCode)?.ToUpperInvariant();
        entity.DestinationCountryCode = Clean(request.DestinationCountryCode)?.ToUpperInvariant();
        entity.OriginPortId = request.OriginPortId;
        entity.DestinationPortId = request.DestinationPortId;
        entity.MerchandiseDescription = request.MerchandiseDescription.Trim();
        entity.PackageCount = request.PackageCount;
        entity.GrossWeightKg = request.GrossWeightKg;
        entity.NetWeightKg = request.NetWeightKg;
        entity.VolumeM3 = request.VolumeM3;
        entity.DeclaredValue = request.DeclaredValue;
        entity.DeclaredCurrencyCode = Clean(request.DeclaredCurrencyCode)?.ToUpperInvariant();
        entity.EstimatedCloseDate = request.EstimatedCloseDate?.Date;
        entity.Notes = Clean(request.Notes);
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        // Los snapshots comerciales originales NO se sobrescriben.
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<OperationDetailDto?> AssignAsync(long id, AssignOperationRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;
        EnsureEditable(entity);
        await EnsureOperationResponsibleAsync(request.ResponsibleUserId, ct);

        var oldUserId = entity.ResponsibleUserId;
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        AddSystemEvent(entity.OperationId, OperationEventTypes.AssignmentChanged, "Responsable actualizado",
            Clean(request.Comment) ?? $"Responsable cambiado de usuario {oldUserId} a {request.ResponsibleUserId}.");

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<OperationDetailDto?> ChangeStageAsync(long id, ChangeStageRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;
        EnsureEditable(entity);

        var fromStage = await db.WorkflowStages.AsNoTracking()
            .SingleAsync(x => x.WorkflowStageId == entity.CurrentStageId, ct);

        var toStage = await db.WorkflowStages.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.WorkflowStageId == request.ToStageId &&
                x.IsActive, ct)
            ?? throw new ArgumentException("Etapa destino inválida o inactiva.");

        if (string.Equals(toStage.Code, "CLOSED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La etapa CLOSED solo se alcanza mediante el endpoint /close.");

        var transition = await db.WorkflowTransitions.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.FromStageId == entity.CurrentStageId &&
                x.ToStageId == request.ToStageId &&
                x.IsActive, ct)
            ?? throw new InvalidOperationException($"No existe transición habilitada {fromStage.Code} → {toStage.Code}.");

        if (transition.RequiresComment && string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("La transición requiere comentario.");

        await EnsureCurrentStageMandatoryTasksCompletedAsync(entity.OperationId, entity.CurrentStageId, ct);
        await EnsureCurrentStageDocumentRequirementsAsync(entity.OperationId, entity.CurrentStageId, ct);
        await EnsureStageBusinessPrerequisitesAsync(entity, toStage.Code, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var oldStageId = entity.CurrentStageId;
        entity.CurrentStageId = toStage.WorkflowStageId;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        db.OperationStageHistories.Add(new OperationStageHistory
        {
            OperationId = entity.OperationId,
            FromStageId = oldStageId,
            ToStageId = toStage.WorkflowStageId,
            Comment = Clean(request.Comment),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        AddSystemEvent(entity.OperationId, OperationEventTypes.StageChanged,
            $"Etapa: {toStage.Name}",
            Clean(request.Comment) ?? $"Cambio de {fromStage.Name} a {toStage.Name}.");

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Open, StringComparison.OrdinalIgnoreCase))
            ChangeOperationalStatus(entity, OperationStatuses.InProgress, "La operación inició ejecución.");

        if (string.Equals(toStage.Code, "DELIVERED", StringComparison.OrdinalIgnoreCase))
            ChangeOperationalStatus(entity, OperationStatuses.Delivered, "Mercancía marcada como entregada.");

        if (string.Equals(toStage.Code, "SETTLEMENT", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entity.FinancialStatus, FinancialStatuses.NoMovements, StringComparison.OrdinalIgnoreCase))
        {
            ChangeFinancialStatus(entity, FinancialStatuses.PendingSettlement,
                "La operación ingresó a etapa de rendición sin movimientos económicos registrados.");
        }

        await db.SaveChangesAsync(ct);
        await GenerateStageTasksAsync(entity, toStage.WorkflowStageId, ct);
        await RecalculateDocumentaryStatusForStageAsync(entity, toStage.WorkflowStageId, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyCollection<OperationAvailableStageDto>> GetAvailableStagesAsync(long id, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null)
            throw new BusinessException(ErrorCodes.NotFound, "Operación no encontrada.", 404);

        EnsureEditable(operation);

        return await (
            from tr in db.WorkflowTransitions.AsNoTracking()
            join st in db.WorkflowStages.AsNoTracking() on tr.ToStageId equals st.WorkflowStageId
            where tr.CompanyId == currentCompany.CompanyId &&
                  tr.FromStageId == operation.CurrentStageId &&
                  tr.IsActive &&
                  st.IsActive &&
                  st.Code != "CLOSED"
            orderby st.DisplayOrder
            select new OperationAvailableStageDto(
                st.WorkflowStageId,
                st.Code,
                st.Name,
                st.DisplayOrder,
                tr.RequiresComment))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<OperationStageHistoryDto>> GetStageHistoryAsync(long id, CancellationToken ct)
    {
        await EnsureOperationExistsAsync(id, ct);

        var rows = await (
            from h in db.OperationStageHistories.AsNoTracking()
            join f0 in db.WorkflowStages.AsNoTracking() on h.FromStageId equals f0.WorkflowStageId into fj
            from f in fj.DefaultIfEmpty()
            join t in db.WorkflowStages.AsNoTracking() on h.ToStageId equals t.WorkflowStageId
            join u in db.AppUsers.AsNoTracking() on h.ChangedBy equals u.UserId
            where h.OperationId == id
            orderby h.ChangedAt descending, h.OperationStageHistoryId descending
            select new
            {
                h.OperationStageHistoryId, h.FromStageId,
                FromStageName = f != null ? f.Name : null,
                h.ToStageId, ToStageName = t.Name, h.Comment, h.ChangedAt, h.ChangedBy,
                u.FirstName, u.LastName, u.Username
            }).ToListAsync(ct);

        return rows.Select(x => new OperationStageHistoryDto(
            x.OperationStageHistoryId,
            x.FromStageId,
            x.FromStageName,
            x.ToStageId,
            x.ToStageName,
            currentUser.IsClientUser ? null : x.Comment,
            x.ChangedAt,
            x.ChangedBy,
            currentUser.IsClientUser ? "Equipo ImportOps" : FormatUser(x.FirstName, x.LastName, x.Username))).ToList();
    }

    public async Task<IReadOnlyCollection<OperationStatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken ct)
    {
        await EnsureOperationExistsAsync(id, ct);

        var historyQuery = db.OperationStatusHistories.AsNoTracking()
            .Where(h => h.OperationId == id);

        if (currentUser.IsClientUser)
            historyQuery = historyQuery.Where(h => h.StatusDimension != "FINANCIAL");

        var rows = await (
            from h in historyQuery
            join u in db.AppUsers.AsNoTracking() on h.ChangedBy equals u.UserId
            orderby h.ChangedAt descending, h.OperationStatusHistoryId descending
            select new
            {
                h.OperationStatusHistoryId, h.StatusDimension, h.OldStatus, h.NewStatus,
                h.Comment, h.ChangedAt, h.ChangedBy, u.FirstName, u.LastName, u.Username
            }).ToListAsync(ct);

        return rows.Select(x => new OperationStatusHistoryDto(
            x.OperationStatusHistoryId,
            x.StatusDimension,
            x.OldStatus,
            x.NewStatus,
            currentUser.IsClientUser ? null : x.Comment,
            x.ChangedAt,
            x.ChangedBy,
            currentUser.IsClientUser ? "Equipo ImportOps" : FormatUser(x.FirstName, x.LastName, x.Username))).ToList();
    }

    public Task<OperationDetailDto?> HoldAsync(long id, string comment, CancellationToken ct) =>
        ChangeOperationalActionAsync(id, OperationStatuses.OnHold,
            new[] { OperationStatuses.Open, OperationStatuses.InProgress },
            Required(comment, "El motivo de pausa es obligatorio."), ct);

    public Task<OperationDetailDto?> ResumeAsync(long id, string comment, CancellationToken ct) =>
        ChangeOperationalActionAsync(id, OperationStatuses.InProgress,
            new[] { OperationStatuses.OnHold },
            Required(comment, "El comentario de reanudación es obligatorio."), ct);

    public async Task<OperationDetailDto?> CancelAsync(long id, string reason, CancellationToken ct)
    {
        reason = Required(reason, "El motivo de cancelación es obligatorio.");
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Closed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una operación cerrada no puede cancelarse.");

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una operación entregada no se cancela; debe completar su rendición y cierre.");

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La operación ya está cancelada.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        entity.CancellationReason = reason;
        ChangeOperationalStatus(entity, OperationStatuses.Cancelled, reason);

        var openTasks = await db.OperationTasks
            .Where(x => x.OperationId == entity.OperationId &&
                        x.TaskStatus != OperationTaskStatuses.Completed &&
                        x.TaskStatus != OperationTaskStatuses.Cancelled)
            .ToListAsync(ct);

        foreach (var task in openTasks)
        {
            task.TaskStatus = OperationTaskStatuses.Cancelled;
            task.ModifiedAt = DateTime.UtcNow;
            task.ModifiedBy = currentUser.UserId;
        }

        AddSystemEvent(entity.OperationId, OperationEventTypes.StatusChanged, "Operación cancelada", reason);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<OperationDetailDto?> CloseAsync(long id, string comment, CancellationToken ct)
    {
        comment = Required(comment, "El comentario de cierre es obligatorio.");
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una operación cancelada no puede cerrarse como completada.");

        if (string.Equals(entity.OperationalStatus, OperationStatuses.Closed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La operación ya está cerrada.");

        var currentStage = await db.WorkflowStages.AsNoTracking()
            .SingleAsync(x => x.WorkflowStageId == entity.CurrentStageId, ct);

        if (!string.Equals(currentStage.Code, "SETTLEMENT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La operación solo puede cerrarse desde la etapa SETTLEMENT.");

        await EnsureCurrentStageMandatoryTasksCompletedAsync(entity.OperationId, entity.CurrentStageId, ct);
        await RecalculateDocumentaryStatusForStageAsync(entity, entity.CurrentStageId, ct);

        if (!string.Equals(entity.DocumentaryStatus, DocumentaryStatuses.Complete, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No puede cerrar: los requisitos documentales obligatorios de la etapa de cierre deben estar completos.");

        if (!string.Equals(entity.FinancialStatus, FinancialStatuses.Closed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No puede cerrar: la rendición económica debe estar CLOSED.");

        var closedStage = await db.WorkflowStages.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.Code == "CLOSED" && x.IsActive, ct)
            ?? throw new InvalidOperationException("No está configurada la etapa CLOSED.");

        var transitionExists = await db.WorkflowTransitions.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.FromStageId == entity.CurrentStageId &&
            x.ToStageId == closedStage.WorkflowStageId &&
            x.IsActive, ct);

        if (!transitionExists)
            throw new InvalidOperationException("No existe transición SETTLEMENT → CLOSED.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var oldStage = entity.CurrentStageId;
        entity.CurrentStageId = closedStage.WorkflowStageId;
        entity.ClosedAt = DateTime.UtcNow;
        entity.ActualCloseDate = DateTime.Today;
        entity.ClosedByUserId = currentUser.UserId;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        db.OperationStageHistories.Add(new OperationStageHistory
        {
            OperationId = entity.OperationId,
            FromStageId = oldStage,
            ToStageId = closedStage.WorkflowStageId,
            Comment = comment,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        ChangeOperationalStatus(entity, OperationStatuses.Closed, comment);
        AddSystemEvent(entity.OperationId, OperationEventTypes.StatusChanged, "Operación cerrada", comment);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyCollection<OperationTaskDto>> GetTasksAsync(long id, string? status, int? stageId, CancellationToken ct)
    {
        if (currentUser.IsClientUser)
            throw new BusinessException(
                ErrorCodes.Forbidden,
                "Las tareas operativas son de uso interno.",
                403);

        await EnsureOperationExistsAsync(id, ct);

        var q = db.OperationTasks.AsNoTracking()
            .Where(x => x.OperationId == id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = NormalizeTaskStatus(status);
            q = q.Where(x => x.TaskStatus == s);
        }

        if (stageId.HasValue) q = q.Where(x => x.WorkflowStageId == stageId.Value);

        var rows = await (
            from t in q
            join s0 in db.WorkflowStages.AsNoTracking() on t.WorkflowStageId equals s0.WorkflowStageId into sj
            from s in sj.DefaultIfEmpty()
            join u0 in db.AppUsers.AsNoTracking() on t.AssignedUserId equals u0.UserId into uj
            from u in uj.DefaultIfEmpty()
            orderby t.TaskStatus, t.DueAt, t.OperationTaskId
            select new
            {
                t.OperationTaskId, t.WorkflowStageId, StageName = s != null ? s.Name : null,
                t.Title, t.Description, t.TaskStatus, t.Priority, t.AssignedUserId,
                UserFirst = u != null ? u.FirstName : null,
                UserLast = u != null ? u.LastName : null,
                UserName = u != null ? u.Username : null,
                t.DueAt, t.CompletedAt, t.CompletedByUserId, t.IsMandatory, t.StageTaskTemplateId
            }).ToListAsync(ct);

        return rows.Select(x => new OperationTaskDto(
            x.OperationTaskId, x.WorkflowStageId, x.StageName, x.Title, x.Description,
            x.TaskStatus, x.Priority, x.AssignedUserId,
            x.AssignedUserId.HasValue ? FormatUser(x.UserFirst, x.UserLast, x.UserName!) : null,
            x.DueAt, x.CompletedAt, x.CompletedByUserId, x.IsMandatory, x.StageTaskTemplateId.HasValue,
            TaskTransitions.TryGetValue(x.TaskStatus, out var allowed)
                ? allowed.OrderBy(v => v).ToArray()
                : Array.Empty<string>())).ToList();
    }

    public async Task<OperationTaskDto?> CreateTaskAsync(long id, CreateOperationTaskRequest request, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;
        EnsureEditable(operation);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("El título de la tarea es obligatorio.");

        var priority = NormalizePriority(request.Priority);

        if (request.AssignedUserId.HasValue)
            await EnsureUserAsync(request.AssignedUserId.Value, ct);

        if (request.WorkflowStageId.HasValue)
            await EnsureStageOwnedAsync(request.WorkflowStageId.Value, ct);

        var task = new OperationTask
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = id,
            WorkflowStageId = request.WorkflowStageId ?? operation.CurrentStageId,
            StageTaskTemplateId = null,
            Title = request.Title.Trim(),
            Description = Clean(request.Description),
            TaskStatus = OperationTaskStatuses.Pending,
            Priority = priority,
            AssignedUserId = request.AssignedUserId ?? operation.ResponsibleUserId,
            DueAt = request.DueAt,
            IsMandatory = request.IsMandatory,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        db.OperationTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return (await GetTasksAsync(id, null, null, ct)).Single(x => x.OperationTaskId == task.OperationTaskId);
    }

    public async Task<OperationTaskDto?> UpdateTaskAsync(long id, long taskId, UpdateOperationTaskRequest request, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;
        EnsureEditable(operation);

        var task = await db.OperationTasks.SingleOrDefaultAsync(x =>
            x.OperationId == id && x.OperationTaskId == taskId, ct);
        if (task is null) return null;

        if (task.TaskStatus is OperationTaskStatuses.Completed or OperationTaskStatuses.Cancelled)
            throw new InvalidOperationException("Una tarea finalizada no puede editarse.");

        if (request.AssignedUserId.HasValue)
            await EnsureUserAsync(request.AssignedUserId.Value, ct);

        if (task.StageTaskTemplateId.HasValue)
        {
            // Las tareas provenientes del workflow conservan definición e obligatoriedad.
            // Solo se permite ajustar responsable, prioridad y vencimiento.
            task.Priority = NormalizePriority(request.Priority);
            task.AssignedUserId = request.AssignedUserId;
            task.DueAt = request.DueAt;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("El título de la tarea es obligatorio.");

            task.Title = request.Title.Trim();
            task.Description = Clean(request.Description);
            task.Priority = NormalizePriority(request.Priority);
            task.AssignedUserId = request.AssignedUserId;
            task.DueAt = request.DueAt;
            task.IsMandatory = request.IsMandatory;
        }
        task.ModifiedAt = DateTime.UtcNow;
        task.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return (await GetTasksAsync(id, null, null, ct)).Single(x => x.OperationTaskId == taskId);
    }

    public async Task<OperationTaskDto?> ChangeTaskStatusAsync(long id, long taskId, ChangeTaskStatusRequest request, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;
        EnsureEditable(operation);

        var task = await db.OperationTasks.SingleOrDefaultAsync(x =>
            x.OperationId == id && x.OperationTaskId == taskId, ct);
        if (task is null) return null;

        var newStatus = NormalizeTaskStatus(request.Status);

        if (!TaskTransitions.TryGetValue(task.TaskStatus, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Transición de tarea inválida: {task.TaskStatus} → {newStatus}.");

        if (task.IsMandatory && string.Equals(newStatus, OperationTaskStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una tarea obligatoria no puede cancelarse.");

        if (string.Equals(newStatus, OperationTaskStatuses.Blocked, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Debe indicar el motivo del bloqueo.");

        task.TaskStatus = newStatus;
        task.ModifiedAt = DateTime.UtcNow;
        task.ModifiedBy = currentUser.UserId;

        if (string.Equals(newStatus, OperationTaskStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = currentUser.UserId;
        }
        else
        {
            task.CompletedAt = null;
            task.CompletedByUserId = null;
        }

        if (newStatus is OperationTaskStatuses.Completed or OperationTaskStatuses.Blocked)
        {
            AddSystemEvent(id, OperationEventTypes.TaskUpdated,
                newStatus == OperationTaskStatuses.Completed ? $"Tarea completada: {task.Title}" : $"Tarea bloqueada: {task.Title}",
                Clean(request.Comment));
        }

        await db.SaveChangesAsync(ct);
        return (await GetTasksAsync(id, null, null, ct)).Single(x => x.OperationTaskId == taskId);
    }

    public async Task<IReadOnlyCollection<OperationEventDto>> GetEventsAsync(long id, CancellationToken ct)
    {
        await EnsureOperationExistsAsync(id, ct);

        var rows = await (
            from e in db.OperationEvents.AsNoTracking()
            join u in db.AppUsers.AsNoTracking() on e.CreatedBy equals u.UserId
            where e.OperationId == id && e.CompanyId == currentCompany.CompanyId && (!currentUser.IsClientUser || e.Visibility == RecordVisibilities.Client || e.Visibility == RecordVisibilities.Both)
            orderby e.EventDate descending, e.OperationEventId descending
            select new
            {
                e.OperationEventId, e.EventType, e.EventDate, e.Title, e.Description, e.SourceType,
                e.Visibility, e.CreatedBy, u.FirstName, u.LastName, u.Username
            }).ToListAsync(ct);

        return rows.Select(x => new OperationEventDto(
            x.OperationEventId, x.EventType, x.EventDate, x.Title, x.Description, x.SourceType, x.Visibility,
            x.CreatedBy, FormatUser(x.FirstName, x.LastName, x.Username))).ToList();
    }

    public async Task<OperationEventDto?> AddManualEventAsync(long id, CreateOperationEventRequest request, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;
        EnsureEditable(operation);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("El título del evento es obligatorio.");

        var entity = new OperationEvent
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = id,
            EventType = NormalizeEventType(request.EventType),
            EventDate = request.EventDate ?? DateTime.UtcNow,
            Title = request.Title.Trim(),
            Description = Clean(request.Description),
            SourceType = OperationEventSources.Manual,
            Visibility = NormalizeVisibility(request.Visibility),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        db.OperationEvents.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetEventsAsync(id, ct)).Single(x => x.OperationEventId == entity.OperationEventId);
    }

    public async Task<IReadOnlyCollection<OperationNoteDto>> GetNotesAsync(long id, string? noteType, CancellationToken ct)
    {
        await EnsureOperationExistsAsync(id, ct);

        var q = db.OperationNotes.AsNoTracking()
            .Where(x => x.OperationId == id && x.CompanyId == currentCompany.CompanyId);

        // El aislamiento por cliente se valida en EnsureOperationExistsAsync.
        // Un usuario CLIENT solo puede ver notas destinadas al cliente.
        if (currentUser.IsClientUser)
            q = q.Where(x => x.NoteType == OperationNoteTypes.Customer);

        if (!string.IsNullOrWhiteSpace(noteType))
        {
            var normalized = NormalizeNoteType(noteType);
            q = q.Where(x => x.NoteType == normalized);
        }

        var rows = await (
            from n in q
            join u in db.AppUsers.AsNoTracking() on n.CreatedBy equals u.UserId
            orderby n.IsPinned descending, n.CreatedAt descending
            select new
            {
                n.OperationNoteId, n.NoteType, n.NoteText, n.IsPinned, n.CreatedAt, n.CreatedBy,
                u.FirstName, u.LastName, u.Username
            }).ToListAsync(ct);

        return rows.Select(x => new OperationNoteDto(
            x.OperationNoteId, x.NoteType, x.NoteText, x.IsPinned, x.CreatedAt, x.CreatedBy,
            FormatUser(x.FirstName, x.LastName, x.Username))).ToList();
    }

    public async Task<OperationNoteDto?> AddNoteAsync(long id, CreateOperationNoteRequest request, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;
        EnsureEditable(operation);

        if (string.IsNullOrWhiteSpace(request.NoteText))
            throw new ArgumentException("El contenido de la nota es obligatorio.");

        var type = NormalizeNoteType(request.NoteType);

        var note = new OperationNote
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = id,
            NoteType = type,
            NoteText = request.NoteText.Trim(),
            IsPinned = request.IsPinned,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        db.OperationNotes.Add(note);

        if (string.Equals(type, OperationNoteTypes.Incident, StringComparison.OrdinalIgnoreCase))
        {
            AddSystemEvent(id, OperationEventTypes.Incident, "Incidencia registrada", Truncate(note.NoteText, 1000));
        }

        await db.SaveChangesAsync(ct);
        return (await GetNotesAsync(id, null, ct)).Single(x => x.OperationNoteId == note.OperationNoteId);
    }

    public async Task<OperationNoteDto?> SetNotePinnedAsync(long id, long noteId, bool pinned, CancellationToken ct)
    {
        var operation = await GetOwnedTrackedAsync(id, ct);
        if (operation is null) return null;

        var note = await db.OperationNotes.SingleOrDefaultAsync(x =>
            x.OperationId == id && x.OperationNoteId == noteId && x.CompanyId == currentCompany.CompanyId, ct);
        if (note is null) return null;

        note.IsPinned = pinned;
        await db.SaveChangesAsync(ct);
        return (await GetNotesAsync(id, null, ct)).Single(x => x.OperationNoteId == noteId);
    }

    public async Task<bool> SetDocumentaryStatusAsync(long id, string newStatus, string comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        var normalized = NormalizeDocumentaryStatus(newStatus);
        if (string.Equals(entity.DocumentaryStatus, normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        ChangeDocumentaryStatus(entity, normalized, Required(comment, "Debe indicar el motivo del estado documentario."));
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetFinancialStatusAsync(long id, string newStatus, string comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return false;

        var normalized = NormalizeFinancialStatus(newStatus);
        if (string.Equals(entity.FinancialStatus, normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        ChangeFinancialStatus(entity, normalized, Required(comment, "Debe indicar el motivo del estado económico."));
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<OperationDetailDto?> ChangeOperationalActionAsync(
        long id, string newStatus, IReadOnlyCollection<string> allowedFrom, string comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (!allowedFrom.Contains(entity.OperationalStatus, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"No se puede cambiar {entity.OperationalStatus} → {newStatus}.");

        ChangeOperationalStatus(entity, newStatus, comment);
        AddSystemEvent(entity.OperationId, OperationEventTypes.StatusChanged,
            newStatus == OperationStatuses.OnHold ? "Operación en pausa" : "Operación reanudada", comment);

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task EnsureCurrentStageMandatoryTasksCompletedAsync(long operationId, int stageId, CancellationToken ct)
    {
        var pending = await db.OperationTasks.AsNoTracking()
            .Where(x => x.OperationId == operationId &&
                        x.WorkflowStageId == stageId &&
                        x.IsMandatory &&
                        x.TaskStatus != OperationTaskStatuses.Completed)
            .Select(x => x.Title)
            .ToListAsync(ct);

        if (pending.Count > 0)
            throw new InvalidOperationException(
                "No puede avanzar. Tareas obligatorias pendientes: " + string.Join("; ", pending));
    }

    private async Task EnsureCurrentStageDocumentRequirementsAsync(long operationId, int stageId, CancellationToken ct)
    {
        var requirements = await (
            from r in db.StageDocumentRequirements.AsNoTracking()
            join dt in db.DocumentTypes.AsNoTracking() on r.DocumentTypeId equals dt.DocumentTypeId
            where r.CompanyId == currentCompany.CompanyId &&
                  r.WorkflowStageId == stageId &&
                  r.IsActive &&
                  r.IsMandatory
            select new { r.DocumentTypeId, dt.Name })
            .ToListAsync(ct);

        if (requirements.Count == 0) return;

        var today = DateTime.Today;

        var validTypes = await db.Documents.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.OperationId == operationId &&
                !x.IsDeleted &&
                x.DocumentStatus == DocumentStatuses.Valid &&
                (!x.ExpirationDate.HasValue || x.ExpirationDate.Value.Date >= today))
            .Select(x => x.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);

        var validSet = validTypes.ToHashSet();
        var missing = requirements
            .Where(x => !validSet.Contains(x.DocumentTypeId))
            .Select(x => x.Name)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "No puede avanzar. Documentos obligatorios faltantes o no válidos: " + string.Join("; ", missing));
    }

    private async Task EnsureStageBusinessPrerequisitesAsync(ImportOperation operation, string destinationStageCode, CancellationToken ct)
    {
        switch (destinationStageCode.ToUpperInvariant())
        {
            case "IN_TRANSIT":
            {
                var ok = await db.Shipments.AsNoTracking().AnyAsync(x =>
                    x.CompanyId == currentCompany.CompanyId &&
                    x.OperationId == operation.OperationId &&
                    !x.IsDeleted &&
                    (x.ShipmentStatus == "DEPARTED" || x.ShipmentStatus == "IN_TRANSIT"), ct);

                if (!ok)
                    throw new InvalidOperationException(
                        "No puede pasar a IN_TRANSIT: debe existir al menos un Shipment con salida confirmada (DEPARTED/IN_TRANSIT).");
                break;
            }

            case "ARRIVED":
            case "CUSTOMS":
            {
                var ok = await db.Shipments.AsNoTracking().AnyAsync(x =>
                    x.CompanyId == currentCompany.CompanyId &&
                    x.OperationId == operation.OperationId &&
                    !x.IsDeleted &&
                    x.ShipmentStatus == "ARRIVED", ct);

                if (!ok)
                    throw new InvalidOperationException(
                        $"No puede pasar a {destinationStageCode}: debe existir un Shipment ARRIVED.");
                break;
            }

            case "RELEASED":
            case "PENDING_DELIVERY":
            {
                var ok = await db.CustomsProcesses.AsNoTracking().AnyAsync(x =>
                    x.CompanyId == currentCompany.CompanyId &&
                    x.OperationId == operation.OperationId &&
                    x.CustomsStatus == "RELEASED" &&
                    x.ReleaseDate != null, ct);

                if (!ok)
                    throw new InvalidOperationException(
                        $"No puede pasar a {destinationStageCode}: Customs debe estar RELEASED con fecha de levante.");
                break;
            }

            case "DELIVERED":
                if (string.Equals(operation.OperationalStatus, OperationStatuses.OnHold, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No puede entregar una operación que está ON_HOLD.");
                break;

            case "SETTLEMENT":
                if (!string.Equals(operation.OperationalStatus, OperationStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("SETTLEMENT requiere que la operación esté DELIVERED.");
                break;
        }
    }

    private async Task RecalculateDocumentaryStatusForStageAsync(
        ImportOperation operation,
        int stageId,
        CancellationToken ct)
    {
        var mandatoryTypeIds = await db.StageDocumentRequirements.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.WorkflowStageId == stageId &&
                x.IsActive &&
                x.IsMandatory)
            .Select(x => x.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);

        string newStatus;

        if (mandatoryTypeIds.Count == 0)
        {
            newStatus = DocumentaryStatuses.Complete;
        }
        else
        {
            var hasObservedMandatoryDocument = await db.Documents.AsNoTracking().AnyAsync(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.OperationId == operation.OperationId &&
                !x.IsDeleted &&
                mandatoryTypeIds.Contains(x.DocumentTypeId) &&
                x.DocumentStatus == DocumentStatuses.Observed, ct);

            if (hasObservedMandatoryDocument)
            {
                newStatus = DocumentaryStatuses.Observed;
            }
            else
            {
                var today = DateTime.Today;

                var validTypeIds = await db.Documents.AsNoTracking()
                    .Where(x =>
                        x.CompanyId == currentCompany.CompanyId &&
                        x.OperationId == operation.OperationId &&
                        !x.IsDeleted &&
                        mandatoryTypeIds.Contains(x.DocumentTypeId) &&
                        x.DocumentStatus == DocumentStatuses.Valid &&
                        (!x.ExpirationDate.HasValue || x.ExpirationDate.Value.Date >= today))
                    .Select(x => x.DocumentTypeId)
                    .Distinct()
                    .ToListAsync(ct);

                var validSet = validTypeIds.ToHashSet();

                newStatus = mandatoryTypeIds.All(validSet.Contains)
                    ? DocumentaryStatuses.Complete
                    : DocumentaryStatuses.Pending;
            }
        }

        if (string.Equals(
                operation.DocumentaryStatus,
                newStatus,
                StringComparison.OrdinalIgnoreCase))
            return;

        ChangeDocumentaryStatus(
            operation,
            newStatus,
            $"Recalculado para la etapa {stageId}: requisitos documentales obligatorios.");
    }

    private async Task GenerateStageTasksAsync(ImportOperation operation, int stageId, CancellationToken ct)
    {
        var templates = await db.StageTaskTemplates.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.WorkflowStageId == stageId &&
                x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);

        foreach (var template in templates)
        {
            var exists = await db.OperationTasks.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                x.OperationId == operation.OperationId &&
                x.StageTaskTemplateId == template.StageTaskTemplateId &&
                !x.IsDeleted, ct);

            if (exists) continue;

            db.OperationTasks.Add(new OperationTask
            {
                CompanyId = currentCompany.CompanyId,
                OperationId = operation.OperationId,
                WorkflowStageId = stageId,
                StageTaskTemplateId = template.StageTaskTemplateId,
                Title = template.Title,
                Description = template.Description,
                TaskStatus = OperationTaskStatuses.Pending,
                Priority = TaskPriorities.Normal,
                AssignedUserId = operation.ResponsibleUserId,
                DueAt = template.DueDaysOffset.HasValue
                    ? DateTime.UtcNow.AddDays(template.DueDaysOffset.Value)
                    : null,
                IsMandatory = template.IsMandatory,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.UserId
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private void ChangeOperationalStatus(ImportOperation entity, string newStatus, string comment)
    {
        newStatus = NormalizeOperationalStatus(newStatus);
        if (string.Equals(entity.OperationalStatus, newStatus, StringComparison.OrdinalIgnoreCase)) return;
        var old = entity.OperationalStatus;
        entity.OperationalStatus = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;
        AddStatusHistory(entity.OperationId, "OPERATIONAL", old, newStatus, comment);
    }

    private void ChangeDocumentaryStatus(ImportOperation entity, string newStatus, string comment)
    {
        var old = entity.DocumentaryStatus;
        entity.DocumentaryStatus = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;
        AddStatusHistory(entity.OperationId, "DOCUMENTARY", old, newStatus, comment);
        AddSystemEvent(entity.OperationId, OperationEventTypes.StatusChanged,
            $"Estado documentario: {newStatus}", comment);
    }

    private void ChangeFinancialStatus(ImportOperation entity, string newStatus, string comment)
    {
        var old = entity.FinancialStatus;
        entity.FinancialStatus = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;
        AddStatusHistory(entity.OperationId, "FINANCIAL", old, newStatus, comment);
        AddSystemEvent(entity.OperationId, OperationEventTypes.StatusChanged,
            $"Estado económico: {newStatus}", comment);
    }

    private void AddStatusHistory(long operationId, string dimension, string? oldStatus, string newStatus, string? comment)
    {
        db.OperationStatusHistories.Add(new OperationStatusHistory
        {
            OperationId = operationId,
            StatusDimension = dimension,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = Clean(comment),
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });
    }

    private void AddSystemEvent(long operationId, string eventType, string title, string? description)
    {
        db.OperationEvents.Add(new OperationEvent
        {
            CompanyId = currentCompany.CompanyId,
            OperationId = operationId,
            EventType = eventType,
            EventDate = DateTime.UtcNow,
            Title = Truncate(title, 180),
            Description = Truncate(Clean(description), 1000),
            SourceType = OperationEventSources.System,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        });
    }

    private async Task ValidateOperationReferencesAsync(long clientId, UpdateOperationRequest r, CancellationToken ct)
    {
        if (r.ClientContactId.HasValue)
        {
            var ok = await db.ClientContacts.AsNoTracking().AnyAsync(x =>
                x.ClientContactId == r.ClientContactId &&
                x.ClientId == clientId &&
                x.IsActive, ct);
            if (!ok) throw new ArgumentException("El contacto no pertenece al cliente o está inactivo.");
        }

        if (r.SupplierId.HasValue)
        {
            var ok = await db.Suppliers.AsNoTracking().AnyAsync(x =>
                x.SupplierId == r.SupplierId &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive, ct);
            if (!ok) throw new ArgumentException("Proveedor inválido.");
        }

        if (r.TransportModeId.HasValue &&
            !await db.TransportModes.AsNoTracking().AnyAsync(x => x.TransportModeId == r.TransportModeId && x.IsActive, ct))
            throw new ArgumentException("Modalidad de transporte inválida.");

        var incoterm = Clean(r.IncotermCode)?.ToUpperInvariant();
        if (incoterm is not null &&
            !await db.Incoterms.AsNoTracking().AnyAsync(x => x.IncotermCode == incoterm && x.IsActive, ct))
            throw new ArgumentException("Incoterm inválido.");

        var oc = Clean(r.OriginCountryCode)?.ToUpperInvariant();
        var dc = Clean(r.DestinationCountryCode)?.ToUpperInvariant();

        if (oc is not null &&
            !await db.Countries.AsNoTracking().AnyAsync(x => x.CountryCode == oc && x.IsActive, ct))
            throw new ArgumentException("País de origen inválido.");

        if (dc is not null &&
            !await db.Countries.AsNoTracking().AnyAsync(x => x.CountryCode == dc && x.IsActive, ct))
            throw new ArgumentException("País de destino inválido.");

        if (r.OriginPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking().SingleOrDefaultAsync(x => x.PortId == r.OriginPortId && x.IsActive, ct)
                ?? throw new ArgumentException("Puerto de origen inválido.");
            if (oc is not null && !string.Equals(port.CountryCode, oc, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El puerto de origen no pertenece al país de origen.");
        }

        if (r.DestinationPortId.HasValue)
        {
            var port = await db.Ports.AsNoTracking().SingleOrDefaultAsync(x => x.PortId == r.DestinationPortId && x.IsActive, ct)
                ?? throw new ArgumentException("Puerto de destino inválido.");
            if (dc is not null && !string.Equals(port.CountryCode, dc, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El puerto de destino no pertenece al país de destino.");
        }

        var currency = Clean(r.DeclaredCurrencyCode)?.ToUpperInvariant();
        if (r.DeclaredValue.HasValue && currency is null)
            throw new ArgumentException("Debe indicar moneda si registra valor declarado.");

        if (currency is not null &&
            !await db.Currencies.AsNoTracking().AnyAsync(x => x.CurrencyCode == currency && x.IsActive, ct))
            throw new ArgumentException("Moneda declarada inválida.");
    }

    private Task<ImportOperation?> GetOwnedTrackedAsync(long id, CancellationToken ct) =>
        db.ImportOperations.SingleOrDefaultAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.OperationId == id &&
            (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct);

    private async Task EnsureOperationExistsAsync(long id, CancellationToken ct)
    {
        if (!await db.ImportOperations.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.OperationId == id &&
            (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct))
            throw new BusinessException(
                ErrorCodes.NotFound,
                "La operación no existe o no está disponible para el usuario actual.",
                404);
    }

    private async Task<bool> IsOperationResponsibleAsync(
        long userId,
        CancellationToken ct)
    {
        return await db.AppUsers
            .AsNoTracking()
            .AnyAsync(u =>
                u.UserId == userId &&
                u.CompanyId == currentCompany.CompanyId &&
                u.IsActive &&
                !u.IsDeleted &&
                u.UserType == UserTypes.Internal &&
                !u.Username.StartsWith("system.") &&
                (
                    from ur in db.UserRoles
                    join role in db.Roles
                        on ur.RoleId equals role.RoleId
                    where ur.UserId == u.UserId &&
                          role.CompanyId == currentCompany.CompanyId &&
                          role.IsActive &&
                          (
                              role.Code == "ADMIN" ||
                              role.Code == "OPERATIONS"
                          )
                    select ur.UserId
                ).Any(),
                ct);
    }

    private async Task EnsureOperationResponsibleAsync(
        long userId,
        CancellationToken ct)
    {
        var valid = await IsOperationResponsibleAsync(userId, ct);

        if (!valid)
        {
            throw new BusinessException(
                ErrorCodes.Validation,
                "El responsable debe ser un usuario INTERNAL activo con perfil ADMIN u OPERATIONS.",
                400);
        }
    }

    private async Task EnsureUserAsync(long id, CancellationToken ct)
    {
        var ok = await db.AppUsers.AsNoTracking().AnyAsync(x =>
            x.UserId == id &&
            x.CompanyId == currentCompany.CompanyId &&
            x.IsActive, ct);
        if (!ok) throw new ArgumentException("Usuario responsable inválido, inactivo o de otra empresa.");
    }

    private Task EnsureCurrentUserAsync(CancellationToken ct) => EnsureUserAsync(currentUser.UserId, ct);

    private async Task EnsureStageOwnedAsync(int id, CancellationToken ct)
    {
        if (!await db.WorkflowStages.AsNoTracking().AnyAsync(x =>
            x.WorkflowStageId == id &&
            x.CompanyId == currentCompany.CompanyId &&
            x.IsActive, ct))
            throw new ArgumentException("Etapa inválida.");
    }

    private async Task<long> NextOperationSequenceAsync(CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR ops.SEQ_OPERATION_NO;";
            var value = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(value);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static void EnsureEditable(ImportOperation operation)
    {
        if (operation.OperationalStatus is OperationStatuses.Closed or OperationStatuses.Cancelled)
            throw new InvalidOperationException($"La operación {operation.OperationalStatus} no puede modificarse.");
    }

    private static string NormalizeOperationalStatus(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (!ValidOperationalStatuses.Contains(v)) throw new ArgumentException("Estado operativo inválido.");
        return v;
    }

    private static string NormalizeDocumentaryStatus(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (!ValidDocumentaryStatuses.Contains(v)) throw new ArgumentException("Estado documentario inválido.");
        return v;
    }

    private static string NormalizeFinancialStatus(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (!ValidFinancialStatuses.Contains(v)) throw new ArgumentException("Estado económico inválido.");
        return v;
    }

    private static string NormalizeTaskStatus(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (v is not (OperationTaskStatuses.Pending or OperationTaskStatuses.InProgress or
                      OperationTaskStatuses.Blocked or OperationTaskStatuses.Completed or OperationTaskStatuses.Cancelled))
            throw new ArgumentException("Estado de tarea inválido.");
        return v;
    }

    private static string NormalizePriority(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (v is not (TaskPriorities.Low or TaskPriorities.Normal or TaskPriorities.High or TaskPriorities.Critical))
            throw new ArgumentException("Prioridad inválida.");
        return v;
    }

    private static string NormalizeNoteType(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (v is not (OperationNoteTypes.Internal or OperationNoteTypes.Customer or OperationNoteTypes.Incident))
            throw new ArgumentException("NoteType debe ser INTERNAL, CUSTOMER o INCIDENT.");
        return v;
    }

    private static string NormalizeEventType(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(v) || v.Length > 40)
            throw new ArgumentException("EventType inválido.");
        return v;
    }

    private static void ValidateNonNegative(decimal? value, string field)
    {
        if (value < 0) throw new ArgumentException($"{field} no puede ser negativo.");
    }

    private static string Required(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message);
        return value.Trim();
    }

    private static string FormatUser(string? first, string? last, string username)
    {
        var full = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(full) ? username : full;
    }


    private static string NormalizeVisibility(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? RecordVisibilities.Both
            : value.Trim().ToUpperInvariant();

        if (normalized is not (
            RecordVisibilities.Internal or
            RecordVisibilities.Client or
            RecordVisibilities.Both))
            throw new BusinessException(ErrorCodes.Validation, "Visibility inválida.", 400);

        return normalized;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
