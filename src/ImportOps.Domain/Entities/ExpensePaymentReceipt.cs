namespace ImportOps.Domain.Entities;

// Evidencia documental privada; NO es un movimiento contable ni se incluye en liquidaciones.
public sealed class ExpensePaymentReceipt
{
    public long ExpensePaymentReceiptId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long OperationExpenseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "PEN";
    public string? ReferenceNo { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public long UploadedBy { get; set; }
    public bool IncludeInClientLiquidation { get; set; }
    public bool IsVoid { get; set; }
    public DateTime? VoidedAt { get; set; }
    public long? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
}
