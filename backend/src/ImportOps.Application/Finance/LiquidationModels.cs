namespace ImportOps.Application.Finance;

public sealed record LiquidationLineDto(string Source, long SourceId, string Description, string? Reference, decimal Amount, string CurrencyCode = "USD", string PayerType = "COMPANY");
public sealed record LiquidationCurrencyDto(
    string CurrencyCode, IReadOnlyList<LiquidationLineDto> Lines,
    decimal TotalExpenses, decimal TotalServices, decimal TotalCustomsTaxes, decimal TotalDue,
    decimal ClientAdvances, decimal ServiceCollections, decimal DirectClientTaxPayments,
    decimal TotalReceived, decimal TotalRefunded, decimal Balance);
public sealed record LiquidationPreviewDto(
    long OperationId, string OperationNo, string ClientName, string CompanyName,
    string Merchandise, string CurrencyCode, IReadOnlyList<LiquidationLineDto> Lines,
    decimal TotalExpenses, decimal TotalServices, decimal TotalDue,
    decimal ClientAdvances, decimal ServiceCollections, decimal TotalReceived,
    decimal TotalRefunded, decimal Balance,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<LiquidationCurrencyDto>? CurrencySections = null,
    string? Number = null, DateTime? IssuedAt = null);

public sealed record FinancingTermsDto(long OperationId, int FinancingDays, string Source, bool Issued);
public sealed record LiquidationDeliveryDto(long OperationId, string Number, int FinancingDays, DateTime IssuedAt, DateTime? SentAt, DateTime? DueAt, string? SentVia, string? SentTo, string Status);
public sealed record SendLiquidationRequest(string SentVia, string? SentTo, DateTime? SentAt = null);

public interface ILiquidationService
{
    Task<FinancingTermsDto> TermsAsync(long operationId, CancellationToken ct);
    Task<FinancingTermsDto> SetTermsAsync(long operationId, int days, CancellationToken ct);
    Task<LiquidationDeliveryDto?> DeliveryAsync(long operationId, CancellationToken ct);
    Task<LiquidationDeliveryDto> SendAsync(long operationId, SendLiquidationRequest request, CancellationToken ct);
    Task<LiquidationPreviewDto> PreviewAsync(long operationId, CancellationToken ct);
    Task<LiquidationPreviewDto> IssueAsync(long operationId, CancellationToken ct);
    Task<LiquidationPreviewDto?> IssuedAsync(long operationId, CancellationToken ct);
    Task<(byte[] Content, string FileName)?> PdfAsync(long operationId, CancellationToken ct);
}
