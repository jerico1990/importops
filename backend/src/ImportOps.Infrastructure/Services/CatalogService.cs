using ImportOps.Application.Catalogs;
using ImportOps.Application.Common;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class CatalogService(ImportOpsDbContext db, ICurrentCompany currentCompany) : ICatalogService
{
    public async Task<IReadOnlyCollection<AppUserCatalogDto>> GetUsersAsync(string? purpose, CancellationToken ct)
    {
        var q = db.AppUsers.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.UserType == UserTypes.Internal &&
                !x.Username.StartsWith("system."));

        var normalizedPurpose = string.IsNullOrWhiteSpace(purpose)
            ? "INTERNAL"
            : purpose.Trim().ToUpperInvariant();

        if (normalizedPurpose == "ACCOUNT_MANAGER")
        {
            q = q.Where(u =>
                (from ur in db.UserRoles
                 join r in db.Roles on ur.RoleId equals r.RoleId
                 where ur.UserId == u.UserId &&
                       r.CompanyId == currentCompany.CompanyId &&
                       r.IsActive &&
                       (r.Code == "ADMIN" || r.Code == "COMMERCIAL")
                 select ur.UserId).Any());
        }
        else if (normalizedPurpose == "OPERATION_RESPONSIBLE")
        {
            q = q.Where(u =>
                (from ur in db.UserRoles
                 join r in db.Roles on ur.RoleId equals r.RoleId
                 where ur.UserId == u.UserId &&
                       r.CompanyId == currentCompany.CompanyId &&
                       r.IsActive &&
                       (r.Code == "ADMIN" || r.Code == "OPERATIONS")
                 select ur.UserId).Any());
        }
        else if (normalizedPurpose == "REQUEST_ASSIGNEE")
        {
            q = q.Where(u =>
                (from ur in db.UserRoles
                 join r in db.Roles on ur.RoleId equals r.RoleId
                 where ur.UserId == u.UserId &&
                       r.CompanyId == currentCompany.CompanyId &&
                       r.IsActive &&
                       (r.Code == "ADMIN" || r.Code == "COMMERCIAL" || r.Code == "OPERATIONS")
                 select ur.UserId).Any());
        }
        else if (normalizedPurpose != "INTERNAL")
        {
            throw new BusinessException(ErrorCodes.Validation, "Purpose de usuarios inválido.", 400);
        }

        return await q
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.Username)
            .Select(x => new AppUserCatalogDto(x.UserId, x.Username, x.FirstName, x.LastName, x.Email))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct) =>
        await db.Currencies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CurrencyCode)
            .Select(x => new CurrencyDto(x.CurrencyCode, x.CurrencyName, x.Symbol, x.DecimalPlaces)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(CancellationToken ct) =>
        await db.Countries.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CountryName)
            .Select(x => new CountryDto(x.CountryCode, x.CountryName, x.Iso3Code)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<PortDto>> GetPortsAsync(string? countryCode, string? portType, CancellationToken ct)
    {
        var q = db.Ports.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var country = countryCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.CountryCode == country);
        }
        if (!string.IsNullOrWhiteSpace(portType))
        {
            var type = portType.Trim().ToUpperInvariant();
            q = q.Where(x => x.PortType == type);
        }
        return await q.OrderBy(x => x.PortName)
            .Select(x => new PortDto(x.PortId, x.CountryCode, x.PortCode, x.PortName, x.CityName, x.PortType)).ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<TransportModeDto>> GetTransportModesAsync(CancellationToken ct) =>
        await db.TransportModes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new TransportModeDto(x.TransportModeId, x.Code, x.Name)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<IncotermDto>> GetIncotermsAsync(CancellationToken ct) =>
        await db.Incoterms.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.IncotermCode)
            .Select(x => new IncotermDto(x.IncotermCode, x.Description)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken ct) =>
        await db.PaymentMethods.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new PaymentMethodDto(x.PaymentMethodId, x.Code, x.Name)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct) =>
        await db.DocumentTypes.AsNoTracking()
            .Where(x => x.IsActive && (x.CompanyId == null || x.CompanyId == currentCompany.CompanyId))
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .Select(x => new DocumentTypeDto(x.DocumentTypeId, x.Code, x.Name, x.Category, x.IsRequiredDefault)).ToListAsync(ct);

    public Task<IReadOnlyCollection<CodeNameDto>> GetDocumentCategoriesAsync(CancellationToken ct)
    {
        IReadOnlyCollection<CodeNameDto> items = new[]
        {
            new CodeNameDto("COMMERCIAL", "Comercial"),
            new CodeNameDto("LOGISTICS", "Logística"),
            new CodeNameDto("CUSTOMS", "Aduanas"),
            new CodeNameDto("FINANCIAL", "Financiera"),
            new CodeNameDto("OTHER", "Otros")
        };
        return Task.FromResult(items);
    }

    public async Task<IReadOnlyCollection<ExpenseCategoryDto>> GetExpenseCategoriesAsync(CancellationToken ct) =>
        await db.ExpenseCategories.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ExpenseCategoryDto(x.ExpenseCategoryId, x.Code, x.Name, x.IsRebillableDefault)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<ServiceCategoryDto>> GetServiceCategoriesAsync(CancellationToken ct) =>
        await db.ServiceCategories.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServiceCategoryDto(x.ServiceCategoryId, x.Code, x.Name)).ToListAsync(ct);

    public async Task<IReadOnlyCollection<WorkflowStageDto>> GetWorkflowStagesAsync(CancellationToken ct) =>
        await db.WorkflowStages.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new WorkflowStageDto(x.WorkflowStageId, x.Code, x.Name, x.DisplayOrder, x.IsFinal)).ToListAsync(ct);

    public Task<IReadOnlyCollection<ImportRequestStatusDto>> GetImportRequestStatusesAsync(CancellationToken ct)
    {
        IReadOnlyCollection<ImportRequestStatusDto> items = new[]
        {
            new ImportRequestStatusDto(ImportRequestStatuses.New, "Nueva", true, false),
            new ImportRequestStatusDto(ImportRequestStatuses.UnderReview, "En revisión", false, false),
            new ImportRequestStatusDto(ImportRequestStatuses.Observed, "Observada", true, false),
            new ImportRequestStatusDto(ImportRequestStatuses.Quoted, "Cotizada", false, false),
            new ImportRequestStatusDto(ImportRequestStatuses.Approved, "Aprobada", false, true),
            new ImportRequestStatusDto(ImportRequestStatuses.Rejected, "Rechazada", false, true),
            new ImportRequestStatusDto(ImportRequestStatuses.Cancelled, "Cancelada", false, true)
        };

        return Task.FromResult(items);
    }


    public Task<IReadOnlyCollection<QuotationStatusDto>> GetQuotationStatusesAsync(CancellationToken ct)
    {
        IReadOnlyCollection<QuotationStatusDto> items = new[]
        {
            new QuotationStatusDto(QuotationStatuses.Draft, "Borrador", false),
            new QuotationStatusDto(QuotationStatuses.Sent, "Enviada", false),
            new QuotationStatusDto(QuotationStatuses.Approved, "Aprobada", true),
            new QuotationStatusDto(QuotationStatuses.Rejected, "Rechazada", true),
            new QuotationStatusDto(QuotationStatuses.Expired, "Vencida", true),
            new QuotationStatusDto(QuotationStatuses.Cancelled, "Cancelada", true)
        };
        return Task.FromResult(items);
    }

    public Task<IReadOnlyCollection<QuotationItemTypeDto>> GetQuotationItemTypesAsync(CancellationToken ct)
    {
        IReadOnlyCollection<QuotationItemTypeDto> items = new[]
        {
            new QuotationItemTypeDto(QuotationItemTypes.Service, "Servicio", "SERVICE_CATEGORY"),
            new QuotationItemTypeDto(QuotationItemTypes.EstimatedCost, "Gasto estimado", "EXPENSE_CATEGORY"),
            new QuotationItemTypeDto(QuotationItemTypes.Other, "Otro concepto", null)
        };
        return Task.FromResult(items);
    }

    public Task<IReadOnlyCollection<CodeNameDto>> GetOperationStatusesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(OperationStatuses.Open, "Abierta"),
            new CodeNameDto(OperationStatuses.InProgress, "En proceso"),
            new CodeNameDto(OperationStatuses.OnHold, "En pausa"),
            new CodeNameDto(OperationStatuses.Delivered, "Entregada"),
            new CodeNameDto(OperationStatuses.Cancelled, "Cancelada"),
            new CodeNameDto(OperationStatuses.Closed, "Cerrada")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetDocumentaryStatusesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(DocumentaryStatuses.Pending, "Pendiente"),
            new CodeNameDto(DocumentaryStatuses.Observed, "Observada"),
            new CodeNameDto(DocumentaryStatuses.Complete, "Completa")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetFinancialStatusesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(FinancialStatuses.NoMovements, "Sin movimientos"),
            new CodeNameDto(FinancialStatuses.Funded, "Fondos recibidos"),
            new CodeNameDto(FinancialStatuses.InExecution, "En ejecución"),
            new CodeNameDto(FinancialStatuses.PendingCollection, "Cobro pendiente"),
            new CodeNameDto(FinancialStatuses.PendingSettlement, "Rendición pendiente"),
            new CodeNameDto(FinancialStatuses.PendingRefund, "Devolución pendiente"),
            new CodeNameDto(FinancialStatuses.Closed, "Cerrado")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetOperationTaskStatusesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(OperationTaskStatuses.Pending, "Pendiente"),
            new CodeNameDto(OperationTaskStatuses.InProgress, "En proceso"),
            new CodeNameDto(OperationTaskStatuses.Blocked, "Bloqueada"),
            new CodeNameDto(OperationTaskStatuses.Completed, "Completada"),
            new CodeNameDto(OperationTaskStatuses.Cancelled, "Cancelada")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetTaskPrioritiesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(TaskPriorities.Low, "Baja"),
            new CodeNameDto(TaskPriorities.Normal, "Normal"),
            new CodeNameDto(TaskPriorities.High, "Alta"),
            new CodeNameDto(TaskPriorities.Critical, "Crítica")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetOperationNoteTypesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(OperationNoteTypes.Internal, "Interna"),
            new CodeNameDto(OperationNoteTypes.Customer, "Cliente"),
            new CodeNameDto(OperationNoteTypes.Incident, "Incidencia")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetOperationEventTypesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(OperationEventTypes.Manual, "Evento manual"),
            new CodeNameDto(OperationEventTypes.Incident, "Incidencia")
        });

    public Task<IReadOnlyCollection<CodeNameDto>> GetRecordVisibilitiesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
        {
            new CodeNameDto(RecordVisibilities.Internal, "Solo interno"),
            new CodeNameDto(RecordVisibilities.Client, "Solo cliente"),
            new CodeNameDto(RecordVisibilities.Both, "Interno + cliente")
        });

public Task<IReadOnlyCollection<CodeNameDto>> GetShipmentStatusesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(ShipmentStatuses.Planned, "Planificado"),
        new CodeNameDto(ShipmentStatuses.Booked, "Booking confirmado"),
        new CodeNameDto(ShipmentStatuses.Departed, "Salida confirmada"),
        new CodeNameDto(ShipmentStatuses.InTransit, "En tránsito"),
        new CodeNameDto(ShipmentStatuses.Arrived, "Arribado"),
        new CodeNameDto(ShipmentStatuses.Cancelled, "Cancelado")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetShipmentContainerTypesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto("20GP", "20' Dry General Purpose"),
        new CodeNameDto("40GP", "40' Dry General Purpose"),
        new CodeNameDto("40HC", "40' High Cube"),
        new CodeNameDto("45HC", "45' High Cube"),
        new CodeNameDto("20RF", "20' Reefer"),
        new CodeNameDto("40RF", "40' Reefer"),
        new CodeNameDto("OPEN_TOP", "Open Top"),
        new CodeNameDto("FLAT_RACK", "Flat Rack"),
        new CodeNameDto("OTHER", "Otro")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetDocumentStatusesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(DocumentStatuses.PendingReview, "Pendiente de revisión"),
        new CodeNameDto(DocumentStatuses.Valid, "Válido"),
        new CodeNameDto(DocumentStatuses.Observed, "Observado"),
        new CodeNameDto(DocumentStatuses.Replaced, "Reemplazado"),
        new CodeNameDto(DocumentStatuses.Expired, "Vencido"),
        new CodeNameDto(DocumentStatuses.Void, "Anulado")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetDocumentObservationStatusesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(DocumentObservationStatuses.Open, "Abierta"),
        new CodeNameDto(DocumentObservationStatuses.Resolved, "Resuelta"),
        new CodeNameDto(DocumentObservationStatuses.Cancelled, "Cancelada")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetCustomsStatusesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(CustomsStatuses.Pending, "Pendiente"),
        new CodeNameDto(CustomsStatuses.InReview, "En revisión"),
        new CodeNameDto(CustomsStatuses.Observed, "Observado"),
        new CodeNameDto(CustomsStatuses.Released, "Levante autorizado"),
        new CodeNameDto(CustomsStatuses.Cancelled, "Cancelado")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetCustomsChannelsAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(CustomsChannels.Green, "Verde"),
        new CodeNameDto(CustomsChannels.Yellow, "Amarillo"),
        new CodeNameDto(CustomsChannels.Red, "Rojo"),
        new CodeNameDto(CustomsChannels.Other, "Otro")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetCustomsObservationTypesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(CustomsObservationTypes.Documentary, "Documentaria"),
        new CodeNameDto(CustomsObservationTypes.Valuation, "Valoración"),
        new CodeNameDto(CustomsObservationTypes.Classification, "Clasificación arancelaria"),
        new CodeNameDto(CustomsObservationTypes.Inspection, "Inspección / aforo"),
        new CodeNameDto(CustomsObservationTypes.AuthorityRequirement, "Requerimiento de autoridad"),
        new CodeNameDto(CustomsObservationTypes.Other, "Otra")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetCustomsObservationSeveritiesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(CustomsObservationSeverities.Low, "Baja"),
        new CodeNameDto(CustomsObservationSeverities.Normal, "Normal"),
        new CodeNameDto(CustomsObservationSeverities.High, "Alta"),
        new CodeNameDto(CustomsObservationSeverities.Critical, "Crítica")
    });

public async Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsRegimesAsync(CancellationToken ct) =>
    await db.CustomsRegimes.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new IdCodeNameDto(x.CustomsRegimeId,x.Code,x.Name)).ToListAsync(ct);

public async Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsOfficesAsync(CancellationToken ct) =>
    await db.CustomsOfficeCatalogs.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new IdCodeNameDto(x.CustomsOfficeId,x.Code,x.Name)).ToListAsync(ct);

public async Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsTaxConceptsAsync(CancellationToken ct) =>
    await db.CustomsTaxConcepts.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new IdCodeNameDto(x.CustomsTaxConceptId,x.Code,x.Name)).ToListAsync(ct);

public async Task<IReadOnlyCollection<CodeNameDto>> GetCustomsTaxTypesAsync(CancellationToken ct) =>
    (await GetCustomsTaxConceptsAsync(ct)).Select(x=>new CodeNameDto(x.Code,x.Name)).ToList();

public Task<IReadOnlyCollection<CodeNameDto>> GetCustomsTaxPaymentStatusesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto(CustomsTaxPaymentStatuses.Pending, "Pendiente"),
        new CodeNameDto(CustomsTaxPaymentStatuses.Paid, "Pagado"),
        new CodeNameDto(CustomsTaxPaymentStatuses.Exempt, "Exonerado"),
        new CodeNameDto(CustomsTaxPaymentStatuses.Void, "Anulado")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetFinanceCollectionTypesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto("SERVICE", "Cobro de servicios"),
        new CodeNameDto("FUND", "Reposición / fondo"),
        new CodeNameDto("OTHER", "Otro cobro")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetFinanceAdjustmentTypesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto("DEBIT", "Débito"),
        new CodeNameDto("CREDIT", "Crédito"),
        new CodeNameDto("CORRECTION", "Corrección")
    });

public Task<IReadOnlyCollection<CodeNameDto>> GetFinancePayerTypesAsync(CancellationToken ct) =>
    Task.FromResult<IReadOnlyCollection<CodeNameDto>>(new[]
    {
        new CodeNameDto("COMPANY", "ImportOps"),
        new CodeNameDto("CLIENT", "Cliente"),
        new CodeNameDto("THIRD_PARTY", "Tercero")
    });
}
