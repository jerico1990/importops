namespace ImportOps.Application.Catalogs;

public interface ICatalogService
{
    Task<IReadOnlyCollection<AppUserCatalogDto>> GetUsersAsync(string? purpose, CancellationToken ct);
    Task<IReadOnlyCollection<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<PortDto>> GetPortsAsync(string? countryCode, string? portType, CancellationToken ct);
    Task<IReadOnlyCollection<TransportModeDto>> GetTransportModesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<IncotermDto>> GetIncotermsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetDocumentCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<ExpenseCategoryDto>> GetExpenseCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<ServiceCategoryDto>> GetServiceCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<WorkflowStageDto>> GetWorkflowStagesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<ImportRequestStatusDto>> GetImportRequestStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetOperationStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetDocumentaryStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetFinancialStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetOperationTaskStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetTaskPrioritiesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetOperationNoteTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetOperationEventTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetRecordVisibilitiesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetShipmentStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetShipmentContainerTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetDocumentStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetDocumentObservationStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsChannelsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsObservationTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsObservationSeveritiesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsRegimesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsOfficesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<IdCodeNameDto>> GetCustomsTaxConceptsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsTaxTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetCustomsTaxPaymentStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetFinanceCollectionTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetFinanceAdjustmentTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<CodeNameDto>> GetFinancePayerTypesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<QuotationStatusDto>> GetQuotationStatusesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<QuotationItemTypeDto>> GetQuotationItemTypesAsync(CancellationToken ct);
}
