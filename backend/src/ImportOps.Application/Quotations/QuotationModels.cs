namespace ImportOps.Application.Quotations;

public sealed class QuotationQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public long? RequestId { get; init; }
    public long? ClientId { get; init; }
    public DateTime? IssueDateFrom { get; init; }
    public DateTime? IssueDateTo { get; init; }
    public bool? OnlyLatestVersion { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record QuotationListItemDto(
    long QuotationId,
    string QuotationNo,
    int VersionNo,
    string Status,
    long RequestId,
    string RequestNo,
    long ClientId,
    string ClientName,
    string CurrencyCode,
    DateTime IssueDate,
    DateTime? ValidUntil,
    decimal TotalAmount,
    DateTime CreatedAt);

public sealed record QuotationItemDto(
    long QuotationItemId,
    int LineNo,
    string ItemType,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal LineTotal,
    int? ExpenseCategoryId,
    string? ExpenseCategoryName,
    int? ServiceCategoryId,
    string? ServiceCategoryName,
    bool IsRebillable,
    string? Notes,
    long? QuotationConceptId,
    bool? IsTaxable,
    int? DisplayOrder);

public sealed record QuotationStatusHistoryDto(
    long QuotationStatusHistoryId,
    string? OldStatus,
    string NewStatus,
    string? Comment,
    DateTime ChangedAt,
    long ChangedBy,
    string ChangedByName);

public sealed record QuotationDetailDto(
    long QuotationId,
    string QuotationNo,
    int VersionNo,
    string Status,
    long RequestId,
    string RequestNo,
    long ClientId,
    string ClientName,
    string CurrencyCode,
    DateTime IssueDate,
    DateTime? ValidUntil,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Terms,
    string? InternalNotes,
    DateTime? SentAt,
    DateTime? ApprovedAt,
    string? ApprovedByName,
    DateTime? RejectedAt,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string RowVersion,
    IReadOnlyCollection<QuotationItemDto> Items,
    IReadOnlyCollection<QuotationStatusHistoryDto> History);

public sealed class CreateQuotationRequest
{
    public string CurrencyCode { get; init; } = "USD";
    public DateTime? IssueDate { get; init; }
    public DateTime? ValidUntil { get; init; }
    public string? Terms { get; init; }
    public string? InternalNotes { get; init; }
    public IReadOnlyCollection<CreateQuotationItemRequest> Items { get; init; } = Array.Empty<CreateQuotationItemRequest>();
}

public sealed class UpdateQuotationRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "USD";
    public DateTime? IssueDate { get; init; }
    public DateTime? ValidUntil { get; init; }
    public string? Terms { get; init; }
    public string? InternalNotes { get; init; }
}

public class CreateQuotationItemRequest
{
    public string ItemType { get; init; } = "SERVICE";
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxRate { get; init; } = 18;
    public int? ExpenseCategoryId { get; init; }
    public int? ServiceCategoryId { get; init; }
    public bool IsRebillable { get; init; }
    public string? Notes { get; init; }
    public long? QuotationConceptId { get; init; }
}

public sealed class UpdateQuotationItemRequest : CreateQuotationItemRequest
{
}

public sealed class QuotationActionRequest
{
    public string? Comment { get; init; }
}

public sealed class ApproveQuotationRequest
{
    public string ApprovedByName { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public sealed class RejectQuotationRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class NewQuotationVersionRequest
{
    public DateTime? ValidUntil { get; init; }
    public string? Comment { get; init; }
}
