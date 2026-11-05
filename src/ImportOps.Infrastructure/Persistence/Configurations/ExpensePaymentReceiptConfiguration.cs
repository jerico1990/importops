using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class ExpensePaymentReceiptConfiguration : IEntityTypeConfiguration<ExpensePaymentReceipt>
{
    public void Configure(EntityTypeBuilder<ExpensePaymentReceipt> b)
    {
        b.ToTable("EXPENSE_PAYMENT_RECEIPT", "fin");
        b.HasKey(x => x.ExpensePaymentReceiptId);
        b.Property(x => x.ExpensePaymentReceiptId).HasColumnName("expense_payment_receipt_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.OperationExpenseId).HasColumnName("operation_expense_id");
        b.Property(x => x.PaymentDate).HasColumnName("payment_date");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(19,2);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        b.Property(x => x.ReferenceNo).HasColumnName("reference_no").HasMaxLength(150);
        b.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        b.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(600);
        b.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(50);
        b.Property(x => x.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64);
        b.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
        b.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        b.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        b.Property(x => x.IncludeInClientLiquidation).HasColumnName("include_in_client_liquidation");
        b.Property(x => x.IsVoid).HasColumnName("is_void");
        b.Property(x => x.VoidedAt).HasColumnName("voided_at");
        b.Property(x => x.VoidedBy).HasColumnName("voided_by");
        b.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        b.HasIndex(x => new { x.CompanyId, x.OperationId, x.OperationExpenseId });
    }
}
