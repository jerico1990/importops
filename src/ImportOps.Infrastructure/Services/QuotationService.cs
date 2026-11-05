using System.Data;
using ImportOps.Application.Common;
using ImportOps.Application.ImportRequests;
using ImportOps.Application.Quotations;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class QuotationService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser,
    IImportRequestService importRequestService) : IQuotationService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        QuotationStatuses.Draft,
        QuotationStatuses.Sent,
        QuotationStatuses.Approved,
        QuotationStatuses.Rejected,
        QuotationStatuses.Expired,
        QuotationStatuses.Cancelled
    };

    public async Task<PagedResult<QuotationListItemDto>> GetAsync(QuotationQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.Quotations.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId);

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            if (query.ClientId.HasValue && query.ClientId.Value != currentUser.ClientId.Value)
                throw new BusinessException(ErrorCodes.Forbidden, "No puede consultar cotizaciones de otro cliente.", 403);

            var scopedClientId = currentUser.ClientId.Value;

            // El portal cliente solo conoce cotizaciones que realmente fueron enviadas.
            // Un DRAFT o una versión cancelada antes de envío sigue siendo información interna.
            q = q.Where(x =>
                x.ClientId == scopedClientId &&
                x.SentAt != null);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x =>
                x.QuotationNo.Contains(term) ||
                x.Request.RequestNo.Contains(term) ||
                x.Client.LegalName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            q = q.Where(x => x.Status == status);
        }

        if (query.RequestId.HasValue) q = q.Where(x => x.RequestId == query.RequestId);
        if (query.ClientId.HasValue) q = q.Where(x => x.ClientId == query.ClientId);

        if (query.IssueDateFrom.HasValue)
        {
            var from = query.IssueDateFrom.Value.Date;
            q = q.Where(x => x.IssueDate >= from);
        }

        if (query.IssueDateTo.HasValue)
        {
            var to = query.IssueDateTo.Value.Date;
            q = q.Where(x => x.IssueDate <= to);
        }

        if (query.OnlyLatestVersion == true)
        {
            q = q.Where(x => x.VersionNo ==
                db.Quotations
                    .Where(y => y.CompanyId == currentCompany.CompanyId && y.RequestId == x.RequestId)
                    .Max(y => y.VersionNo));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.QuotationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QuotationListItemDto(
                x.QuotationId,
                x.QuotationNo,
                x.VersionNo,
                x.Status,
                x.RequestId,
                x.Request.RequestNo,
                x.ClientId,
                x.Client.LegalName,
                x.CurrencyCode,
                x.IssueDate,
                x.ValidUntil,
                x.TotalAmount,
                x.CreatedAt))
            .ToListAsync(ct);

        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<QuotationListItemDto>> GetByRequestAsync(long requestId, CancellationToken ct)
    {
        await EnsureRequestOwnedAsync(requestId, ct);

        return await db.Quotations.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.RequestId == requestId &&
                (!currentUser.IsClientUser ||
                    (x.ClientId == currentUser.ClientId && x.SentAt != null)))
            .OrderByDescending(x => x.VersionNo)
            .Select(x => new QuotationListItemDto(
                x.QuotationId,
                x.QuotationNo,
                x.VersionNo,
                x.Status,
                x.RequestId,
                x.Request.RequestNo,
                x.ClientId,
                x.Client.LegalName,
                x.CurrencyCode,
                x.IssueDate,
                x.ValidUntil,
                x.TotalAmount,
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<QuotationDetailDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var q = await db.Quotations.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.QuotationId == id &&
                (!currentUser.IsClientUser ||
                    (x.ClientId == currentUser.ClientId && x.SentAt != null)))
            .Select(x => new
            {
                Entity = x,
                RequestNo = x.Request.RequestNo,
                ClientName = x.Client.LegalName
            })
            .SingleOrDefaultAsync(ct);

        if (q is null) return null;

        var items = await (
            from i in db.QuotationItems.AsNoTracking()
            join ec0 in db.ExpenseCategories.AsNoTracking() on i.ExpenseCategoryId equals ec0.ExpenseCategoryId into ecj
            from ec in ecj.DefaultIfEmpty()
            join sc0 in db.ServiceCategories.AsNoTracking() on i.ServiceCategoryId equals sc0.ServiceCategoryId into scj
            from sc in scj.DefaultIfEmpty()
            where i.QuotationId == id
            orderby i.LineNo
            select new QuotationItemDto(
                i.QuotationItemId,
                i.LineNo,
                i.ItemType,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.DiscountAmount,
                i.TaxRate,
                i.TaxAmount,
                i.LineTotal,
                i.ExpenseCategoryId,
                ec != null ? ec.Name : null,
                i.ServiceCategoryId,
                sc != null ? sc.Name : null,
                i.IsRebillable,
                i.Notes,
                i.QuotationConceptId,
                i.IsTaxable,
                i.DisplayOrder))
            .ToListAsync(ct);

        var history = await db.QuotationStatusHistories.AsNoTracking()
            .Where(x => x.QuotationId == id)
            .OrderByDescending(x => x.ChangedAt)
            .ThenByDescending(x => x.QuotationStatusHistoryId)
            .Select(x => new QuotationStatusHistoryDto(
                x.QuotationStatusHistoryId,
                x.OldStatus,
                x.NewStatus,
                x.Comment,
                x.ChangedAt,
                x.ChangedBy,
                ((x.ChangedByUser.FirstName ?? "") + " " + (x.ChangedByUser.LastName ?? "")).Trim() != "" ? ((x.ChangedByUser.FirstName ?? "") + " " + (x.ChangedByUser.LastName ?? "")).Trim() : x.ChangedByUser.Username))
            .ToListAsync(ct);

        var e = q.Entity;

        return new QuotationDetailDto(
            e.QuotationId,
            e.QuotationNo,
            e.VersionNo,
            e.Status,
            e.RequestId,
            q.RequestNo,
            e.ClientId,
            q.ClientName,
            e.CurrencyCode,
            e.IssueDate,
            e.ValidUntil,
            e.Subtotal,
            e.DiscountAmount,
            e.TaxAmount,
            e.TotalAmount,
            e.Terms,
            currentUser.IsClientUser ? null : e.InternalNotes,
            e.SentAt,
            e.ApprovedAt,
            e.ApprovedByName,
            e.RejectedAt,
            e.RejectionReason,
            e.CreatedAt,
            e.ModifiedAt,
            ConcurrencyToken.Encode(e.RowVer),
            items,
            history);
    }

    public async Task<QuotationDetailDto> CreateAsync(long requestId, CreateQuotationRequest request, CancellationToken ct)
    {
        if (currentUser.IsClientUser)
            throw new BusinessException(ErrorCodes.Forbidden, "Los usuarios cliente no pueden crear cotizaciones.", 403);

        await EnsureCurrentUserAsync(ct);

        var importRequest = await db.ImportRequests
            .SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.RequestId == requestId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct)
            ?? throw new ArgumentException("La solicitud no existe o pertenece a otra empresa.");

        if (!string.Equals(importRequest.Status, ImportRequestStatuses.UnderReview, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una solicitud UNDER_REVIEW puede iniciar una cotización.");

        var existingDraft = await db.Quotations.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.RequestId == requestId &&
            x.Status == QuotationStatuses.Draft, ct);

        if (existingDraft)
            throw new InvalidOperationException("Ya existe una cotización DRAFT para esta solicitud.");

        if (await db.Quotations.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.RequestId == requestId &&
            x.Status == QuotationStatuses.Approved, ct))
            throw new InvalidOperationException("La solicitud ya tiene una cotización aprobada.");

        await ValidateHeaderAsync(request.CurrencyCode, request.IssueDate, request.ValidUntil, ct);

        var maxVersion = await db.Quotations.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.RequestId == requestId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId))
            .Select(x => (int?)x.VersionNo)
            .MaxAsync(ct) ?? 0;

        if (maxVersion > 0)
            throw new InvalidOperationException("Para crear una nueva versión use el endpoint /new-version.");

        var sequence = await NextQuotationSequenceAsync(ct);
        var quotationNo = $"COT-{DateTime.Today:yyyy}-{sequence:000000}";

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new Quotation
        {
            CompanyId = currentCompany.CompanyId,
            QuotationNo = quotationNo,
            RequestId = importRequest.RequestId,
            ClientId = importRequest.ClientId,
            VersionNo = 1,
            Status = QuotationStatuses.Draft,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            IssueDate = (request.IssueDate ?? DateTime.Today).Date,
            ValidUntil = request.ValidUntil?.Date,
            Terms = Clean(request.Terms),
            InternalNotes = Clean(request.InternalNotes),
            ContentDescription = Clean(importRequest.MerchandiseDescription),
            EstimatedArrivalDate = importRequest.DesiredServiceDate?.Date,
            PackageCount = importRequest.PackageCount,
            VolumeM3 = importRequest.VolumeM3,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        db.Quotations.Add(entity);
        await db.SaveChangesAsync(ct);

        long? quotationSupplierId = null;
        if (importRequest.SupplierId.HasValue)
        {
            var qs = new QuotationSupplier { QuotationId = entity.QuotationId, SupplierId = importRequest.SupplierId.Value, DisplayOrder = 10 };
            db.QuotationSuppliers.Add(qs);
            await db.SaveChangesAsync(ct);
            quotationSupplierId = qs.QuotationSupplierId;
        }
        if (!string.IsNullOrWhiteSpace(importRequest.MerchandiseDescription))
        {
            db.QuotationCargoItems.Add(new QuotationCargoItem
            {
                QuotationId = entity.QuotationId, QuotationSupplierId = quotationSupplierId, LineNo = 1,
                Description = importRequest.MerchandiseDescription.Trim(), GrossWeightKg = importRequest.GrossWeightKg,
                VolumeM3 = importRequest.VolumeM3, DeclaredValue = importRequest.DeclaredValue,
                CurrencyCode = importRequest.DeclaredCurrencyCode, OriginCountryCode = importRequest.OriginCountryCode
            });
        }

        var line = 1;
        foreach (var item in request.Items)
        {
            var built = await BuildItemAsync(entity.QuotationId, line++, item, ct);
            db.QuotationItems.Add(built);
        }

        db.QuotationStatusHistories.Add(new QuotationStatusHistory
        {
            QuotationId = entity.QuotationId,
            OldStatus = null,
            NewStatus = QuotationStatuses.Draft,
            Comment = "Cotización creada.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(entity.QuotationId, ct);
        await tx.CommitAsync(ct);

        return (await GetByIdAsync(entity.QuotationId, ct))!;
    }

    public async Task<QuotationDetailDto?> UpdateAsync(long id, UpdateQuotationRequest request, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;
        EnsureDraft(entity);

        await ValidateHeaderAsync(request.CurrencyCode, request.IssueDate, request.ValidUntil, ct);
        db.Entry(entity).Property(x => x.RowVer).OriginalValue = ConcurrencyToken.Decode(request.RowVersion);

        entity.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        entity.IssueDate = (request.IssueDate ?? entity.IssueDate).Date;
        entity.ValidUntil = request.ValidUntil?.Date;
        entity.Terms = Clean(request.Terms);
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
        EnsureDraft(entity);

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<QuotationItemDto?> AddItemAsync(long quotationId, CreateQuotationItemRequest request, CancellationToken ct)
    {
        var quotation = await GetOwnedTrackedAsync(quotationId, ct);
        if (quotation is null) return null;
        EnsureDraft(quotation);

        var nextLine = (await db.QuotationItems
            .Where(x => x.QuotationId == quotationId)
            .Select(x => (int?)x.LineNo)
            .MaxAsync(ct) ?? 0) + 1;

        var item = await BuildItemAsync(quotationId, nextLine, request, ct);
        db.QuotationItems.Add(item);
        quotation.ModifiedAt = DateTime.UtcNow;
        quotation.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(quotationId, ct);

        return await GetItemDtoAsync(item.QuotationItemId, ct);
    }

    public async Task<QuotationItemDto?> UpdateItemAsync(long quotationId, long itemId, UpdateQuotationItemRequest request, CancellationToken ct)
    {
        var quotation = await GetOwnedTrackedAsync(quotationId, ct);
        if (quotation is null) return null;
        EnsureDraft(quotation);

        var item = await db.QuotationItems
            .SingleOrDefaultAsync(x => x.QuotationId == quotationId && x.QuotationItemId == itemId, ct);

        if (item is null) return null;

        await ApplyItemAsync(item, request, ct);
        quotation.ModifiedAt = DateTime.UtcNow;
        quotation.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(quotationId, ct);

        return await GetItemDtoAsync(itemId, ct);
    }

    public async Task<bool> DeleteItemAsync(long quotationId, long itemId, CancellationToken ct)
    {
        var quotation = await GetOwnedTrackedAsync(quotationId, ct);
        if (quotation is null) return false;
        EnsureDraft(quotation);

        var item = await db.QuotationItems
            .SingleOrDefaultAsync(x => x.QuotationId == quotationId && x.QuotationItemId == itemId, ct);

        if (item is null) return false;

        db.QuotationItems.Remove(item);
        await db.SaveChangesAsync(ct);

        var remaining = await db.QuotationItems
            .Where(x => x.QuotationId == quotationId)
            .OrderBy(x => x.LineNo)
            .ToListAsync(ct);

        for (var index = 0; index < remaining.Count; index++)
            remaining[index].LineNo = index + 1;

        quotation.ModifiedAt = DateTime.UtcNow;
        quotation.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(quotationId, ct);
        return true;
    }

    public async Task<QuotationDetailDto?> SendAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;
        EnsureDraft(entity);

        await EnsureCanSendAsync(entity, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await ChangeStatusAsync(entity, QuotationStatuses.Sent,
            Clean(comment) ?? "Cotización enviada al cliente.", ct);

        entity.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await importRequestService.MarkQuotedAsync(
            entity.RequestId,
            $"Cotización {entity.QuotationNo} V{entity.VersionNo} enviada al cliente.",
            ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<QuotationDetailDto?> ApproveAsync(long id, ApproveQuotationRequest request, CancellationToken ct)
    {
        var approvedByName = currentUser.IsClientUser
            ? await GetCurrentUserDisplayNameAsync(ct)
            : Clean(request.ApprovedByName);

        if (string.IsNullOrWhiteSpace(approvedByName))
            throw new ArgumentException("Debe indicar quién aprobó la cotización.");

        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (!string.Equals(entity.Status, QuotationStatuses.Sent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una cotización SENT puede aprobarse.");

        if (entity.ValidUntil.HasValue && entity.ValidUntil.Value.Date < DateTime.Today)
            throw new InvalidOperationException("La cotización está vencida y no puede aprobarse.");

        if (await db.Quotations.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.RequestId == entity.RequestId &&
            x.QuotationId != entity.QuotationId &&
            x.Status == QuotationStatuses.Approved, ct))
            throw new InvalidOperationException("Ya existe otra cotización aprobada para esta solicitud.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await ChangeStatusAsync(entity, QuotationStatuses.Approved,
            Clean(request.Comment) ?? $"Cotización aprobada por {approvedByName}.", ct);

        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByName = approvedByName;
        entity.RejectedAt = null;
        entity.RejectionReason = null;

        await db.SaveChangesAsync(ct);

        await importRequestService.MarkApprovedFromQuotationAsync(
            entity.RequestId,
            $"Cotización {entity.QuotationNo} V{entity.VersionNo} aprobada por {entity.ApprovedByName}.",
            ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<QuotationDetailDto?> RejectAsync(long id, string reason, CancellationToken ct)
    {
        Require(reason, "El motivo de rechazo es obligatorio.");

        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (!string.Equals(entity.Status, QuotationStatuses.Sent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una cotización SENT puede rechazarse.");

        await ChangeStatusAsync(entity, QuotationStatuses.Rejected, reason.Trim(), ct);
        entity.RejectedAt = DateTime.UtcNow;
        entity.RejectionReason = reason.Trim();

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<QuotationDetailDto?> CancelAsync(long id, string reason, CancellationToken ct)
    {
        Require(reason, "El motivo de cancelación es obligatorio.");

        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (string.Equals(entity.Status, QuotationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una cotización aprobada no puede cancelarse.");

        if (string.Equals(entity.Status, QuotationStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cotización ya está cancelada.");

        await ChangeStatusAsync(entity, QuotationStatuses.Cancelled, reason.Trim(), ct);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<QuotationDetailDto?> ExpireAsync(long id, string? comment, CancellationToken ct)
    {
        var entity = await GetOwnedTrackedAsync(id, ct);
        if (entity is null) return null;

        if (!string.Equals(entity.Status, QuotationStatuses.Sent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una cotización SENT puede expirar.");

        if (!entity.ValidUntil.HasValue)
            throw new InvalidOperationException("La cotización no tiene fecha de vigencia.");

        if (entity.ValidUntil.Value.Date >= DateTime.Today)
            throw new InvalidOperationException("La cotización aún se encuentra vigente.");

        await ChangeStatusAsync(entity, QuotationStatuses.Expired,
            Clean(comment) ?? $"Cotización vencida el {entity.ValidUntil:yyyy-MM-dd}.", ct);

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<QuotationDetailDto?> CreateNewVersionAsync(long sourceQuotationId, NewQuotationVersionRequest request, CancellationToken ct)
    {
        if (currentUser.IsClientUser)
            throw new BusinessException(ErrorCodes.Forbidden, "Los usuarios cliente no pueden versionar cotizaciones.", 403);

        await EnsureCurrentUserAsync(ct);

        var source = await db.Quotations.AsNoTracking()
            .Include(x => x.Items).Include(x => x.Suppliers).Include(x => x.CargoItems).Include(x => x.CustomsTaxes)
            .SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.QuotationId == sourceQuotationId, ct);

        if (source is null) return null;

        if (string.Equals(source.Status, QuotationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cotización aún está DRAFT; edite esa versión en lugar de crear otra.");

        if (string.Equals(source.Status, QuotationStatuses.Sent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una cotización SENT sigue vigente. Debe ser rechazada, vencer o cancelarse antes de crear otra versión.");

        if (string.Equals(source.Status, QuotationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Una cotización aprobada es final y no puede versionarse.");

        if (source.Status is not (
            QuotationStatuses.Rejected or
            QuotationStatuses.Expired or
            QuotationStatuses.Cancelled))
            throw new InvalidOperationException("Solo una cotización REJECTED, EXPIRED o CANCELLED puede generar una nueva versión.");

        var maxVersion = await db.Quotations.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.RequestId == source.RequestId)
            .MaxAsync(x => x.VersionNo, ct);

        if (source.VersionNo != maxVersion)
            throw new InvalidOperationException("Solo la última versión de una cotización puede generar una nueva versión.");

        if (await db.Quotations.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.RequestId == source.RequestId &&
            x.Status == QuotationStatuses.Draft, ct))
            throw new InvalidOperationException("Ya existe una versión DRAFT para esta solicitud.");

        var importRequest = await db.ImportRequests
            .SingleAsync(x => x.CompanyId == currentCompany.CompanyId && x.RequestId == source.RequestId, ct);

        if (string.Equals(importRequest.Status, ImportRequestStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La solicitud ya está aprobada.");

        // Si venía QUOTED por la versión previa, permanece QUOTED mientras se prepara la nueva versión.
        // La nueva versión recién vuelve a 'enviada' cuando se ejecute /send.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var baseQuotationNo = BaseQuotationNo(source.QuotationNo);
        var versionQuotationNo = $"{baseQuotationNo}-V{maxVersion + 1}";

        if (versionQuotationNo.Length > 30)
            throw new InvalidOperationException("El número de cotización versionado supera la longitud permitida.");

        var entity = new Quotation
        {
            CompanyId = source.CompanyId,
            QuotationNo = versionQuotationNo,
            RequestId = source.RequestId,
            ClientId = source.ClientId,
            VersionNo = maxVersion + 1,
            Status = QuotationStatuses.Draft,
            CurrencyCode = source.CurrencyCode,
            IssueDate = DateTime.Today,
            ValidUntil = request.ValidUntil?.Date ?? source.ValidUntil,
            Terms = source.Terms,
            InternalNotes = source.InternalNotes,
            ReferenceFile = source.ReferenceFile, ContentDescription = source.ContentDescription, CustomsRegime = source.CustomsRegime, CustomsOffice = source.CustomsOffice, CustomsRegimeId = source.CustomsRegimeId, CustomsOfficeId = source.CustomsOfficeId, EstimatedArrivalDate = source.EstimatedArrivalDate, ExchangeRate = source.ExchangeRate, ExwAmount = source.ExwAmount, FreightAmount = source.FreightAmount, InsuranceAmount = source.InsuranceAmount, CustomsValue = source.CustomsValue, NetWeightKg = source.NetWeightKg, PackageCount = source.PackageCount, VolumeM3 = source.VolumeM3, BankInstructions = source.BankInstructions, PublicNotes = source.PublicNotes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        db.Quotations.Add(entity);
        await db.SaveChangesAsync(ct);

        foreach (var sourceItem in source.Items.OrderBy(x => x.LineNo))
        {
            db.QuotationItems.Add(new QuotationItem
            {
                QuotationId = entity.QuotationId,
                LineNo = sourceItem.LineNo,
                ItemType = sourceItem.ItemType,
                Description = sourceItem.Description,
                Quantity = sourceItem.Quantity,
                UnitPrice = sourceItem.UnitPrice,
                DiscountAmount = sourceItem.DiscountAmount,
                TaxRate = sourceItem.TaxRate,
                TaxAmount = sourceItem.TaxAmount,
                LineTotal = sourceItem.LineTotal,
                ExpenseCategoryId = sourceItem.ExpenseCategoryId,
                ServiceCategoryId = sourceItem.ServiceCategoryId,
                IsRebillable = sourceItem.IsRebillable,
                Notes = sourceItem.Notes, QuotationConceptId = sourceItem.QuotationConceptId, IsTaxable = sourceItem.IsTaxable, DisplayOrder = sourceItem.DisplayOrder
            });
        }

        var supplierMap = new Dictionary<long,long>();
        foreach (var ss in source.Suppliers.OrderBy(x=>x.DisplayOrder)) { var ns=new QuotationSupplier{QuotationId=entity.QuotationId,SupplierId=ss.SupplierId,DisplayOrder=ss.DisplayOrder,Notes=ss.Notes}; db.QuotationSuppliers.Add(ns); await db.SaveChangesAsync(ct); supplierMap[ss.QuotationSupplierId]=ns.QuotationSupplierId; }
        foreach (var c in source.CargoItems.OrderBy(x=>x.LineNo)) db.QuotationCargoItems.Add(new QuotationCargoItem{QuotationId=entity.QuotationId,QuotationSupplierId=c.QuotationSupplierId.HasValue?supplierMap[c.QuotationSupplierId.Value]:null,LineNo=c.LineNo,Description=c.Description,Quantity=c.Quantity,UnitOfMeasure=c.UnitOfMeasure,NetWeightKg=c.NetWeightKg,GrossWeightKg=c.GrossWeightKg,VolumeM3=c.VolumeM3,DeclaredValue=c.DeclaredValue,CurrencyCode=c.CurrencyCode,OriginCountryCode=c.OriginCountryCode,Notes=c.Notes});
        foreach (var t in source.CustomsTaxes.OrderBy(x=>x.DisplayOrder)) db.QuotationCustomsTaxes.Add(new QuotationCustomsTax{QuotationId=entity.QuotationId,CustomsTaxConceptId=t.CustomsTaxConceptId,TaxCode=t.TaxCode,Description=t.Description,Amount=t.Amount,RateApplied=t.RateApplied,CalculationBase=t.CalculationBase,IsCalculated=t.IsCalculated,CurrencyCode=t.CurrencyCode,DisplayOrder=t.DisplayOrder,Notes=t.Notes});

        db.QuotationStatusHistories.Add(new QuotationStatusHistory
        {
            QuotationId = entity.QuotationId,
            OldStatus = null,
            NewStatus = QuotationStatuses.Draft,
            Comment = Clean(request.Comment) ?? $"Versión {entity.VersionNo} creada desde V{source.VersionNo}.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(entity.QuotationId, ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(entity.QuotationId, ct);
    }

    private async Task EnsureCanSendAsync(Quotation entity, CancellationToken ct)
    {
        if (!await db.QuotationItems.AsNoTracking().AnyAsync(x => x.QuotationId == entity.QuotationId, ct))
            throw new InvalidOperationException("No puede enviar una cotización sin detalle.");

        if (entity.TotalAmount <= 0)
            throw new InvalidOperationException("El total de la cotización debe ser mayor a cero.");

        if (entity.ValidUntil.HasValue && entity.ValidUntil.Value.Date < DateTime.Today)
            throw new InvalidOperationException("No puede enviar una cotización cuya vigencia ya venció.");

        var maxVersion = await db.Quotations.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.RequestId == entity.RequestId)
            .MaxAsync(x => x.VersionNo, ct);

        if (entity.VersionNo != maxVersion)
            throw new InvalidOperationException("Solo la última versión puede enviarse al cliente.");
    }

    private async Task ValidateHeaderAsync(string currencyCode, DateTime? issueDate, DateTime? validUntil, CancellationToken ct)
    {
        var currency = currencyCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("CurrencyCode es obligatorio.");

        if (!await db.Currencies.AsNoTracking().AnyAsync(x => x.CurrencyCode == currency && x.IsActive, ct))
            throw new ArgumentException("Moneda inválida.");

        var issue = (issueDate ?? DateTime.Today).Date;
        if (validUntil.HasValue && validUntil.Value.Date < issue)
            throw new ArgumentException("ValidUntil no puede ser anterior a IssueDate.");
    }

    private async Task<QuotationItem> BuildItemAsync(long quotationId, int lineNo, CreateQuotationItemRequest request, CancellationToken ct)
    {
        var item = new QuotationItem { QuotationId = quotationId, LineNo = lineNo };
        await ApplyItemAsync(item, request, ct);
        return item;
    }

    private async Task ApplyItemAsync(QuotationItem item, CreateQuotationItemRequest r, CancellationToken ct)
    {
        var itemType = NormalizeItemType(r.ItemType);
        var serviceCategoryId = r.ServiceCategoryId;
        var expenseCategoryId = r.ExpenseCategoryId;
        var isRebillable = r.IsRebillable;

        if (string.IsNullOrWhiteSpace(r.Description))
            throw new ArgumentException("La descripción del concepto es obligatoria.");

        if (r.Quantity <= 0) throw new ArgumentException("Quantity debe ser mayor a cero.");
        if (r.UnitPrice < 0) throw new ArgumentException("UnitPrice no puede ser negativo.");
        if (r.DiscountAmount < 0) throw new ArgumentException("DiscountAmount no puede ser negativo.");
        if (r.TaxRate < 0 || r.TaxRate > 100) throw new ArgumentException("TaxRate debe estar entre 0 y 100.");

        var gross = RoundMoney(r.Quantity * r.UnitPrice);
        var discount = RoundMoney(r.DiscountAmount);

        if (discount > gross)
            throw new ArgumentException("El descuento no puede superar el importe bruto de la línea.");

        QuotationConcept? concept = null;
        if (r.QuotationConceptId.HasValue)
        {
            concept = await db.QuotationConcepts.AsNoTracking().SingleOrDefaultAsync(x => x.QuotationConceptId == r.QuotationConceptId.Value && x.CompanyId == currentCompany.CompanyId && x.IsActive, ct)
                ?? throw new ArgumentException("QuotationConceptId inválido.");
            if (concept.ServiceCategoryId.HasValue == concept.ExpenseCategoryId.HasValue)
                throw new ArgumentException("El concepto de proforma no tiene una categoría válida. Configúralo antes de utilizarlo.");

            serviceCategoryId = concept.ServiceCategoryId;
            expenseCategoryId = concept.ExpenseCategoryId;
            itemType = serviceCategoryId.HasValue ? QuotationItemTypes.Service : QuotationItemTypes.EstimatedCost;
            isRebillable = concept.IsRebillableDefault;
        }

        switch (itemType)
        {
            case QuotationItemTypes.Service:
                if (!serviceCategoryId.HasValue)
                    throw new ArgumentException("SERVICE requiere ServiceCategoryId.");
                if (expenseCategoryId.HasValue)
                    throw new ArgumentException("SERVICE no puede tener ExpenseCategoryId.");
                await EnsureServiceCategoryAsync(serviceCategoryId.Value, ct);
                break;

            case QuotationItemTypes.EstimatedCost:
                if (!expenseCategoryId.HasValue)
                    throw new ArgumentException("ESTIMATED_COST requiere ExpenseCategoryId.");
                if (serviceCategoryId.HasValue)
                    throw new ArgumentException("ESTIMATED_COST no puede tener ServiceCategoryId.");
                await EnsureExpenseCategoryAsync(expenseCategoryId.Value, ct);
                break;

            case QuotationItemTypes.Other:
                if (serviceCategoryId.HasValue || expenseCategoryId.HasValue)
                    throw new ArgumentException("OTHER no debe asociarse a categoría de servicio ni gasto.");
                break;
        }

        var taxable = gross - discount;
        var effectiveTaxRate = concept?.DefaultTaxRate ?? r.TaxRate;
        var isTaxable = concept?.IsTaxable ?? (effectiveTaxRate > 0);
        var tax = QuotationProformaCalculator.LineTax(taxable, effectiveTaxRate, isTaxable);
        var total = RoundMoney(taxable + tax);

        item.ItemType = itemType;
        item.Description = r.Description.Trim();
        item.Quantity = RoundQty(r.Quantity);
        item.UnitPrice = RoundMoney(r.UnitPrice);
        item.DiscountAmount = discount;
        item.TaxRate = decimal.Round(effectiveTaxRate, 4, MidpointRounding.AwayFromZero);
        item.TaxAmount = tax;
        item.LineTotal = total;
        item.ExpenseCategoryId = expenseCategoryId;
        item.ServiceCategoryId = serviceCategoryId;
        item.IsRebillable = itemType == QuotationItemTypes.EstimatedCost ? isRebillable : false;
        item.Notes = Clean(r.Notes);
        item.QuotationConceptId = concept?.QuotationConceptId;
        item.IsTaxable = isTaxable;
    }

    private async Task RecalculateAsync(long quotationId, CancellationToken ct)
    {
        var quotation = await db.Quotations.SingleAsync(x => x.QuotationId == quotationId, ct);
        var items = await db.QuotationItems.AsNoTracking()
            .Where(x => x.QuotationId == quotationId)
            .ToListAsync(ct);

        quotation.Subtotal = RoundMoney(items.Sum(x => x.Quantity * x.UnitPrice));
        quotation.DiscountAmount = RoundMoney(items.Sum(x => x.DiscountAmount));
        quotation.TaxAmount = RoundMoney(items.Sum(x => x.TaxAmount));
        quotation.ExpenseAmount = RoundMoney(items.Sum(x => x.Quantity * x.UnitPrice - x.DiscountAmount));
        quotation.ExpenseTaxAmount = quotation.TaxAmount;
        quotation.CustomsDutiesAmount = RoundMoney(await db.QuotationCustomsTaxes.AsNoTracking().Where(x => x.QuotationId == quotationId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m);
        quotation.TotalAmount = QuotationProformaCalculator.Total(quotation.CustomsDutiesAmount.Value, quotation.ExpenseAmount.Value, quotation.ExpenseTaxAmount.Value);
        quotation.ModifiedAt = DateTime.UtcNow;
        quotation.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
    }

    private async Task<QuotationItemDto?> GetItemDtoAsync(long itemId, CancellationToken ct) =>
        await (
            from i in db.QuotationItems.AsNoTracking()
            join ec0 in db.ExpenseCategories.AsNoTracking() on i.ExpenseCategoryId equals ec0.ExpenseCategoryId into ecj
            from ec in ecj.DefaultIfEmpty()
            join sc0 in db.ServiceCategories.AsNoTracking() on i.ServiceCategoryId equals sc0.ServiceCategoryId into scj
            from sc in scj.DefaultIfEmpty()
            where i.QuotationItemId == itemId
            select new QuotationItemDto(
                i.QuotationItemId,
                i.LineNo,
                i.ItemType,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.DiscountAmount,
                i.TaxRate,
                i.TaxAmount,
                i.LineTotal,
                i.ExpenseCategoryId,
                ec != null ? ec.Name : null,
                i.ServiceCategoryId,
                sc != null ? sc.Name : null,
                i.IsRebillable,
                i.Notes,
                i.QuotationConceptId,
                i.IsTaxable,
                i.DisplayOrder))
            .SingleOrDefaultAsync(ct);

    private async Task ChangeStatusAsync(Quotation entity, string newStatus, string comment, CancellationToken ct)
    {
        var old = entity.Status;
        entity.Status = newStatus;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser.UserId;

        db.QuotationStatusHistories.Add(new QuotationStatusHistory
        {
            QuotationId = entity.QuotationId,
            OldStatus = old,
            NewStatus = newStatus,
            Comment = comment,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = currentUser.UserId
        });

        await Task.CompletedTask;
    }

    private async Task<Quotation?> GetOwnedTrackedAsync(long id, CancellationToken ct) =>
        await db.Quotations.SingleOrDefaultAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.QuotationId == id &&
            (!currentUser.IsClientUser ||
                (x.ClientId == currentUser.ClientId && x.SentAt != null)), ct);

    private async Task EnsureRequestOwnedAsync(long requestId, CancellationToken ct)
    {
        if (!await db.ImportRequests.AsNoTracking().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId && x.RequestId == requestId && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct))
            throw new ArgumentException("La solicitud no existe o pertenece a otra empresa.");
    }

    private async Task EnsureServiceCategoryAsync(int id, CancellationToken ct)
    {
        if (!await db.ServiceCategories.AsNoTracking().AnyAsync(x =>
            x.ServiceCategoryId == id && x.CompanyId == currentCompany.CompanyId && x.IsActive, ct))
            throw new ArgumentException("ServiceCategoryId inválido.");
    }

    private async Task EnsureExpenseCategoryAsync(int id, CancellationToken ct)
    {
        if (!await db.ExpenseCategories.AsNoTracking().AnyAsync(x =>
            x.ExpenseCategoryId == id && x.CompanyId == currentCompany.CompanyId && x.IsActive, ct))
            throw new ArgumentException("ExpenseCategoryId inválido.");
    }

    private async Task EnsureCurrentUserAsync(CancellationToken ct)
    {
        var ok = await db.AppUsers.AsNoTracking().AnyAsync(x =>
            x.UserId == currentUser.UserId &&
            x.CompanyId == currentCompany.CompanyId &&
            x.IsActive, ct);

        if (!ok)
            throw new InvalidOperationException("El usuario actual no existe, está inactivo o pertenece a otra empresa.");
    }

    private async Task<string> GetCurrentUserDisplayNameAsync(CancellationToken ct)
    {
        var user = await db.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.UserId == currentUser.UserId &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive &&
                !x.IsDeleted,
                ct)
            ?? throw new BusinessException(
                ErrorCodes.InvalidIdentity,
                "Usuario no habilitado.",
                401);

        return UserName(user);
    }

    private async Task<long> NextQuotationSequenceAsync(CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR sales.SEQ_QUOTATION_NO;";
            var value = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(value);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static void EnsureDraft(Quotation entity)
    {
        if (!string.Equals(entity.Status, QuotationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo una cotización DRAFT puede modificarse.");
    }

    private static string BaseQuotationNo(string quotationNo)
    {
        var index = quotationNo.LastIndexOf("-V", StringComparison.OrdinalIgnoreCase);
        if (index > 0 && int.TryParse(quotationNo[(index + 2)..], out _))
            return quotationNo[..index];

        return quotationNo;
    }

    private static string NormalizeStatus(string value)
    {
        var status = value.Trim().ToUpperInvariant();
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException($"Estado de cotización inválido: {value}.");
        return status;
    }

    private static string NormalizeItemType(string value)
    {
        var type = value.Trim().ToUpperInvariant();
        if (type is not (QuotationItemTypes.Service or QuotationItemTypes.EstimatedCost or QuotationItemTypes.Other))
            throw new ArgumentException("ItemType debe ser SERVICE, ESTIMATED_COST u OTHER.");
        return type;
    }

    private static void Require(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message);
    }

    private static string UserName(AppUser user)
    {
        var full = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? user.Username : full;
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal RoundQty(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
