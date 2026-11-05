namespace ImportOps.Application.Finance;

public sealed record FinancialSummaryDto(
    long OperationId,
    string FinancialStatus,
    decimal TotalClientFunds,
    decimal TotalExpenses,
    decimal TotalExpensePayments,
    decimal TotalServices,
    decimal TotalCollections,
    decimal TotalRefunds,
    decimal ManagedFundBalance,
    decimal PendingServiceCollection,
    decimal ClientBalance);

public sealed record BudgetDto(long OperationBudgetId,int VersionNo,string Status,string BaseCurrencyCode,decimal EstimatedCost,decimal EstimatedService,decimal Total,string RowVersion);
public sealed record FundDto(long ClientFundId,string Status,decimal? RequestedAmount,decimal? ReceivedAmount,string CurrencyCode,decimal? BaseAmount,string? ReferenceNo,string RowVersion,decimal? ExchangeRate=null,int? PaymentMethodId=null,string? Notes=null);
public sealed record ExpenseDto(long OperationExpenseId,string Status,string Description,decimal Amount,string CurrencyCode,decimal BaseAmount,bool IsRebillable,decimal PaidBaseAmount,string RowVersion,int ExpenseCategoryId=0,long? SupplierId=null,string? DocumentReference=null,DateTime? ExpenseDate=null,DateTime? DueDate=null,decimal? ExchangeRate=null,decimal? RebillAmount=null,string? Notes=null,string PayerType="UNREVIEWED");
public sealed record ServiceDto(long OperationServiceId,string Status,string Description,decimal Amount,string CurrencyCode,decimal BaseAmount,decimal CollectedAmount,string RowVersion,int ServiceCategoryId=0,decimal? ExchangeRate=null,string? Notes=null);
public sealed record SaveServiceRequest(int ServiceCategoryId,string Description,string CurrencyCode,decimal Amount,decimal? ExchangeRate,string? Notes);
public sealed record EditServiceRequest(string RowVersion,SaveServiceRequest Data);
public sealed record CollectionDto(long ClientCollectionId,string Type,string Status,decimal Amount,string CurrencyCode,decimal BaseAmount,string? ReferenceNo,string RowVersion,DateTime? CollectionDate=null,decimal? ExchangeRate=null,int? PaymentMethodId=null,string? Notes=null);
public sealed record RefundDto(long ClientRefundId,string Status,decimal Amount,string CurrencyCode,decimal BaseAmount,string? ReferenceNo,string RowVersion,decimal? ExchangeRate=null,int? PaymentMethodId=null,string? Reason=null,string? Notes=null);
public sealed record SettlementDto(long OperationSettlementId,string Status,string BaseCurrencyCode,decimal TotalClientFunds,decimal TotalExpenses,decimal TotalCompanyServices,decimal TotalCollections,decimal TotalRefunds,decimal TotalAdjustments,decimal ClientBalance,string RowVersion);

public sealed class RegisterFundRequest
{
    public decimal? RequestedAmount { get; init; }
    public decimal ReceivedAmount { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal? ExchangeRate { get; init; }
    public int? PaymentMethodId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Notes { get; init; }
}

public sealed class RegisterExpenseRequest
{
    public int ExpenseCategoryId { get; init; }
    public long? SupplierId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? DocumentReference { get; init; }
    public DateTime ExpenseDate { get; init; }
    public DateTime? DueDate { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal Amount { get; init; }
    public decimal? ExchangeRate { get; init; }
    public bool IsRebillable { get; init; }
    public string PayerType { get; init; } = "COMPANY";
    public decimal? RebillAmount { get; init; }
    public string? Notes { get; init; }
}

public sealed class PayExpenseRequest
{
    public DateTime PaymentDate { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal? ExchangeRate { get; init; }
    public string PayerType { get; init; } = "COMPANY";
    public int? PaymentMethodId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Notes { get; init; }
}

public sealed class RegisterCollectionRequest
{
    public string CollectionType { get; init; } = "SERVICE";
    public DateTime CollectionDate { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal? ExchangeRate { get; init; }
    public int? PaymentMethodId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? Notes { get; init; }
}

public sealed class AllocateCollectionRequest
{
    public long OperationServiceId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class RegisterAdjustmentRequest
{
    public string AdjustmentType { get; init; } = "CORRECTION";
    public string Reason { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal? ExchangeRate { get; init; }
    public DateTime AdjustmentDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class RegisterRefundRequest
{
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "PEN";
    public decimal? ExchangeRate { get; init; }
    public int? PaymentMethodId { get; init; }
    public string? ReferenceNo { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public sealed class ApproveSettlementRequest
{
    public string RowVersion { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public sealed record EditFundRequest(string RowVersion, RegisterFundRequest Data);
public sealed record EditExpenseRequest(string RowVersion, RegisterExpenseRequest Data);
public sealed record EditCollectionRequest(string RowVersion, RegisterCollectionRequest Data);
public sealed record EditRefundRequest(string RowVersion, RegisterRefundRequest Data);
public sealed record VoidFinanceRequest(string RowVersion,string Reason);

// Registro interno: no exponer en endpoints del portal cliente.
public sealed record ExpensePaymentDto(long ExpensePaymentId,long OperationExpenseId,DateTime PaymentDate,string CurrencyCode,decimal Amount,decimal BaseAmount,string PayerType,int? PaymentMethodId,string? ReferenceNo,string? Notes,bool IsVoid,string RowVersion);
public sealed record VoidExpensePaymentRequest(string RowVersion,string Reason);
