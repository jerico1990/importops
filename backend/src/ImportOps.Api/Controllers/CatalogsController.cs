using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Catalogs;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
public sealed class CatalogsController(ICatalogService service) : ControllerBase
{
    [HttpGet("users")] public async Task<IActionResult> Users([FromQuery] string? purpose, CancellationToken ct) => Ok(await service.GetUsersAsync(purpose, ct));
    [HttpGet("currencies")] public async Task<IActionResult> Currencies(CancellationToken ct) => Ok(await service.GetCurrenciesAsync(ct));
    [HttpGet("countries")] public async Task<IActionResult> Countries(CancellationToken ct) => Ok(await service.GetCountriesAsync(ct));
    [HttpGet("ports")] public async Task<IActionResult> Ports([FromQuery] string? countryCode, [FromQuery] string? portType, CancellationToken ct) => Ok(await service.GetPortsAsync(countryCode, portType, ct));
    [HttpGet("transport-modes")] public async Task<IActionResult> TransportModes(CancellationToken ct) => Ok(await service.GetTransportModesAsync(ct));
    [HttpGet("incoterms")] public async Task<IActionResult> Incoterms(CancellationToken ct) => Ok(await service.GetIncotermsAsync(ct));
    [HttpGet("payment-methods")] public async Task<IActionResult> PaymentMethods(CancellationToken ct) => Ok(await service.GetPaymentMethodsAsync(ct));
    [HttpGet("document-types")] public async Task<IActionResult> DocumentTypes(CancellationToken ct) => Ok(await service.GetDocumentTypesAsync(ct));
    [HttpGet("document-categories")] public async Task<IActionResult> DocumentCategories(CancellationToken ct) => Ok(await service.GetDocumentCategoriesAsync(ct));
    [HttpGet("expense-categories")] public async Task<IActionResult> ExpenseCategories(CancellationToken ct) => Ok(await service.GetExpenseCategoriesAsync(ct));
    [HttpGet("service-categories")] public async Task<IActionResult> ServiceCategories(CancellationToken ct) => Ok(await service.GetServiceCategoriesAsync(ct));
    [HttpGet("workflow-stages")] public async Task<IActionResult> WorkflowStages(CancellationToken ct) => Ok(await service.GetWorkflowStagesAsync(ct));
    [HttpGet("import-request-statuses")] public async Task<IActionResult> ImportRequestStatuses(CancellationToken ct) => Ok(await service.GetImportRequestStatusesAsync(ct));
    [HttpGet("quotation-statuses")] public async Task<IActionResult> QuotationStatuses(CancellationToken ct) => Ok(await service.GetQuotationStatusesAsync(ct));
    [HttpGet("quotation-item-types")] public async Task<IActionResult> QuotationItemTypes(CancellationToken ct) => Ok(await service.GetQuotationItemTypesAsync(ct));
    [HttpGet("operation-statuses")] public async Task<IActionResult> OperationStatuses(CancellationToken ct) => Ok(await service.GetOperationStatusesAsync(ct));
    [HttpGet("documentary-statuses")] public async Task<IActionResult> DocumentaryStatuses(CancellationToken ct) => Ok(await service.GetDocumentaryStatusesAsync(ct));
    [HttpGet("financial-statuses")] public async Task<IActionResult> FinancialStatuses(CancellationToken ct) => Ok(await service.GetFinancialStatusesAsync(ct));
    [HttpGet("operation-task-statuses")] public async Task<IActionResult> OperationTaskStatuses(CancellationToken ct) => Ok(await service.GetOperationTaskStatusesAsync(ct));
    [HttpGet("task-priorities")] public async Task<IActionResult> TaskPriorities(CancellationToken ct) => Ok(await service.GetTaskPrioritiesAsync(ct));
    [HttpGet("operation-note-types")] public async Task<IActionResult> OperationNoteTypes(CancellationToken ct) => Ok(await service.GetOperationNoteTypesAsync(ct));
    [HttpGet("operation-event-types")] public async Task<IActionResult> OperationEventTypes(CancellationToken ct) => Ok(await service.GetOperationEventTypesAsync(ct));
    [HttpGet("record-visibilities")] public async Task<IActionResult> RecordVisibilities(CancellationToken ct) => Ok(await service.GetRecordVisibilitiesAsync(ct));

[HttpGet("shipment-statuses")] public async Task<IActionResult> ShipmentStatuses(CancellationToken ct) => Ok(await service.GetShipmentStatusesAsync(ct));
[HttpGet("shipment-container-types")] public async Task<IActionResult> ShipmentContainerTypes(CancellationToken ct) => Ok(await service.GetShipmentContainerTypesAsync(ct));
[HttpGet("document-statuses")] public async Task<IActionResult> DocumentStatuses(CancellationToken ct) => Ok(await service.GetDocumentStatusesAsync(ct));
[HttpGet("document-observation-statuses")] public async Task<IActionResult> DocumentObservationStatuses(CancellationToken ct) => Ok(await service.GetDocumentObservationStatusesAsync(ct));

[HttpGet("customs-statuses")] public async Task<IActionResult> CustomsStatuses(CancellationToken ct) => Ok(await service.GetCustomsStatusesAsync(ct));
[HttpGet("customs-channels")] public async Task<IActionResult> CustomsChannels(CancellationToken ct) => Ok(await service.GetCustomsChannelsAsync(ct));
[HttpGet("customs-observation-types")] public async Task<IActionResult> CustomsObservationTypes(CancellationToken ct) => Ok(await service.GetCustomsObservationTypesAsync(ct));
[HttpGet("customs-observation-severities")] public async Task<IActionResult> CustomsObservationSeverities(CancellationToken ct) => Ok(await service.GetCustomsObservationSeveritiesAsync(ct));
[HttpGet("customs-regimes")] public async Task<IActionResult> CustomsRegimes(CancellationToken ct) => Ok(await service.GetCustomsRegimesAsync(ct));
[HttpGet("customs-offices")] public async Task<IActionResult> CustomsOffices(CancellationToken ct) => Ok(await service.GetCustomsOfficesAsync(ct));
[HttpGet("customs-tax-concepts")] public async Task<IActionResult> CustomsTaxConcepts(CancellationToken ct) => Ok(await service.GetCustomsTaxConceptsAsync(ct));
[HttpGet("customs-tax-types")] public async Task<IActionResult> CustomsTaxTypes(CancellationToken ct) => Ok(await service.GetCustomsTaxTypesAsync(ct));
[HttpGet("customs-tax-payment-statuses")] public async Task<IActionResult> CustomsTaxPaymentStatuses(CancellationToken ct) => Ok(await service.GetCustomsTaxPaymentStatusesAsync(ct));
[HttpGet("finance-collection-types")] public async Task<IActionResult> FinanceCollectionTypes(CancellationToken ct) => Ok(await service.GetFinanceCollectionTypesAsync(ct));
[HttpGet("finance-adjustment-types")] public async Task<IActionResult> FinanceAdjustmentTypes(CancellationToken ct) => Ok(await service.GetFinanceAdjustmentTypesAsync(ct));
[HttpGet("finance-payer-types")] public async Task<IActionResult> FinancePayerTypes(CancellationToken ct) => Ok(await service.GetFinancePayerTypesAsync(ct));
}
