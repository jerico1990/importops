using ImportOps.Application.Common;

namespace ImportOps.Application.ImportRequests;

public interface IImportRequestService
{
    Task<PagedResult<ImportRequestListItemDto>> GetAsync(ImportRequestQuery query, CancellationToken ct);
    Task<ImportRequestDetailDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<ImportRequestDetailDto> CreateAsync(CreateImportRequestRequest request, CancellationToken ct);
    Task<ImportRequestDetailDto?> UpdateAsync(long id, UpdateImportRequestRequest request, CancellationToken ct);
    Task<bool> DeleteDraftAsync(long id, CancellationToken ct);

    Task<ImportRequestDetailDto?> SubmitForReviewAsync(long id, string? comment, CancellationToken ct);
    Task<ImportRequestDetailDto?> ObserveAsync(long id, string comment, CancellationToken ct);
    Task<ImportRequestDetailDto?> RejectAsync(long id, string reason, CancellationToken ct);
    Task<ImportRequestDetailDto?> CancelAsync(long id, string reason, CancellationToken ct);
    Task<ImportRequestDetailDto?> AssignAsync(long id, long? assignedUserId, string? comment, CancellationToken ct);

    // Uso interno de otros módulos: no se exponen directamente como endpoints públicos.
    Task<bool> MarkQuotedAsync(long id, string? comment, CancellationToken ct);
    Task<bool> MarkApprovedFromQuotationAsync(long id, string? comment, CancellationToken ct);
}
