using ImportOps.Application.Common;

namespace ImportOps.Application.Operations;

public interface IImportOperationService
{
    Task<PagedResult<OperationListItemDto>> GetAsync(OperationQuery query, CancellationToken ct);
    Task<OperationDetailDto?> GetByIdAsync(long id, CancellationToken ct);

    Task<OperationDetailDto> CreateFromQuotationAsync(long quotationId, CreateOperationFromQuotationRequest request, CancellationToken ct);
    Task<OperationDetailDto?> UpdateAsync(long id, UpdateOperationRequest request, CancellationToken ct);
    Task<OperationDetailDto?> AssignAsync(long id, AssignOperationRequest request, CancellationToken ct);

    Task<OperationDetailDto?> ChangeStageAsync(long id, ChangeStageRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<OperationAvailableStageDto>> GetAvailableStagesAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<OperationStageHistoryDto>> GetStageHistoryAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<OperationStatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken ct);

    Task<OperationDetailDto?> HoldAsync(long id, string comment, CancellationToken ct);
    Task<OperationDetailDto?> ResumeAsync(long id, string comment, CancellationToken ct);
    Task<OperationDetailDto?> CancelAsync(long id, string reason, CancellationToken ct);
    Task<OperationDetailDto?> CloseAsync(long id, string comment, CancellationToken ct);

    Task<IReadOnlyCollection<OperationTaskDto>> GetTasksAsync(long id, string? status, int? stageId, CancellationToken ct);
    Task<OperationTaskDto?> CreateTaskAsync(long id, CreateOperationTaskRequest request, CancellationToken ct);
    Task<OperationTaskDto?> UpdateTaskAsync(long id, long taskId, UpdateOperationTaskRequest request, CancellationToken ct);
    Task<OperationTaskDto?> ChangeTaskStatusAsync(long id, long taskId, ChangeTaskStatusRequest request, CancellationToken ct);

    Task<IReadOnlyCollection<OperationEventDto>> GetEventsAsync(long id, CancellationToken ct);
    Task<OperationEventDto?> AddManualEventAsync(long id, CreateOperationEventRequest request, CancellationToken ct);

    Task<IReadOnlyCollection<OperationNoteDto>> GetNotesAsync(long id, string? noteType, CancellationToken ct);
    Task<OperationNoteDto?> AddNoteAsync(long id, CreateOperationNoteRequest request, CancellationToken ct);
    Task<OperationNoteDto?> SetNotePinnedAsync(long id, long noteId, bool pinned, CancellationToken ct);

    // Para los próximos módulos Documents y Finance. No se exponen como endpoints manuales.
    Task<bool> SetDocumentaryStatusAsync(long id, string newStatus, string comment, CancellationToken ct);
    Task<bool> SetFinancialStatusAsync(long id, string newStatus, string comment, CancellationToken ct);
}
