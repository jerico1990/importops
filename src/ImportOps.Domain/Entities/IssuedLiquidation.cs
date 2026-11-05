namespace ImportOps.Domain.Entities;

// Snapshot comercial independiente del cierre financiero operativo.
public sealed class IssuedLiquidation
{
    public long IssuedLiquidationId { get; set; }
    public long CompanyId { get; set; }
    public long OperationId { get; set; }
    public long ClientId { get; set; }
    public int SequenceNo { get; set; }
    public int YearNo { get; set; }
    public string Number { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PEN";
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public long IssuedBy { get; set; }
    public int FinancingDays { get; set; }
    public DateTime? SentAt { get; set; }
    public long? SentBy { get; set; }
    public string? SentVia { get; set; }
    public string? SentTo { get; set; }
    public DateTime? DueAt { get; set; }
}
