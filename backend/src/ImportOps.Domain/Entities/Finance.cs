namespace ImportOps.Domain.Entities;

public sealed class OperationBudget
{
    public long OperationBudgetId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public int VersionNo { get; set; }
    public string BudgetStatus { get; set; } = FinanceStatuses.Draft;
    public string BaseCurrencyCode { get; set; } = "PEN";
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalEstimatedService { get; set; }
    public decimal TotalEstimatedAmount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class OperationBudgetItem
{
    public long OperationBudgetItemId { get; set; }
    public long OperationBudgetId { get; set; }
    public string ItemType { get; set; } = "COST";
    public int? ExpenseCategoryId { get; set; }
    public int? ServiceCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PEN";
    public decimal EstimatedAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal EstimatedBaseAmount { get; set; }
    public bool IsRebillable { get; set; }
    public string? Notes { get; set; }
}

public sealed class ClientFund
{
    public long ClientFundId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long ClientId { get; set; }
    public string FundStatus { get; set; } = "REQUESTED";
    public string FundNature { get; set; } = "CLIENT_ADVANCE";
    public decimal? RequestedAmount { get; set; }
    public decimal? ReceivedAmount { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal? ExchangeRate { get; set; }
    public decimal? BaseAmount { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class OperationExpense
{
    public long OperationExpenseId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public int ExpenseCategoryId { get; set; }
    public long? SupplierId { get; set; }
    public string ExpenseStatus { get; set; } = "PENDING";
    public string Description { get; set; } = string.Empty;
    public string? DocumentReference { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public string? PaidBy { get; set; }
    public bool IsRebillable { get; set; }
    public decimal? RebillAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class ExpensePayment
{
    public long ExpensePaymentId { get; set; }
    public long CompanyId { get; set; }
    public long OperationExpenseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public string PayerType { get; set; } = "COMPANY";
    public int? PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public bool IsVoid { get; set; }
    public DateTime? VoidedAt { get; set; }
    public long? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class OperationServiceCharge
{
    public long OperationServiceId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public int ServiceCategoryId { get; set; }
    public string ServiceStatus { get; set; } = "PENDING";
    public string Description { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class ClientCollection
{
    public long ClientCollectionId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long ClientId { get; set; }
    public string CollectionType { get; set; } = "SERVICE";
    public string CollectionStatus { get; set; } = "RECEIVED";
    public DateTime CollectionDate { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public bool IsVoid { get; set; }
    public DateTime? VoidedAt { get; set; }
    public long? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class ServiceCollectionAllocation
{
    public long ServiceCollectionAllocationId { get; set; }
    public long OperationServiceId { get; set; }
    public long ClientCollectionId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocatedAt { get; set; }
    public long CreatedBy { get; set; }
}

public sealed class OperationAdjustment
{
    public long OperationAdjustmentId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public string AdjustmentType { get; set; } = "CORRECTION";
    public string Reason { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public bool IsVoid { get; set; }
}

public sealed class ClientRefund
{
    public long ClientRefundId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long ClientId { get; set; }
    public string RefundStatus { get; set; } = "PENDING";
    public DateTime? RefundDate { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal BaseAmount { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public sealed class OperationSettlement
{
    public long OperationSettlementId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public string SettlementStatus { get; set; } = FinanceStatuses.Draft;
    public string BaseCurrencyCode { get; set; } = "PEN";
    public decimal TotalClientFunds { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalCompanyServices { get; set; }
    public decimal TotalCollections { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal ClientBalance { get; set; }
    public DateTime? SettlementDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}

public static class FinanceStatuses
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Cancelled = "CANCELLED";
}
