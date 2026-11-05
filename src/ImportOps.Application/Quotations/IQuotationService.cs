using ImportOps.Application.Common;

namespace ImportOps.Application.Quotations;

public interface IQuotationService
{
    Task<PagedResult<QuotationListItemDto>> GetAsync(QuotationQuery query, CancellationToken ct);
    Task<QuotationDetailDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<QuotationListItemDto>> GetByRequestAsync(long requestId, CancellationToken ct);

    Task<QuotationDetailDto> CreateAsync(long requestId, CreateQuotationRequest request, CancellationToken ct);
    Task<QuotationDetailDto?> UpdateAsync(long id, UpdateQuotationRequest request, CancellationToken ct);
    Task<bool> DeleteDraftAsync(long id, CancellationToken ct);

    Task<QuotationItemDto?> AddItemAsync(long quotationId, CreateQuotationItemRequest request, CancellationToken ct);
    Task<QuotationItemDto?> UpdateItemAsync(long quotationId, long itemId, UpdateQuotationItemRequest request, CancellationToken ct);
    Task<bool> DeleteItemAsync(long quotationId, long itemId, CancellationToken ct);

    Task<QuotationDetailDto?> SendAsync(long id, string? comment, CancellationToken ct);
    Task<QuotationDetailDto?> ApproveAsync(long id, ApproveQuotationRequest request, CancellationToken ct);
    Task<QuotationDetailDto?> RejectAsync(long id, string reason, CancellationToken ct);
    Task<QuotationDetailDto?> CancelAsync(long id, string reason, CancellationToken ct);
    Task<QuotationDetailDto?> ExpireAsync(long id, string? comment, CancellationToken ct);

    Task<QuotationDetailDto?> CreateNewVersionAsync(long sourceQuotationId, NewQuotationVersionRequest request, CancellationToken ct);
}
