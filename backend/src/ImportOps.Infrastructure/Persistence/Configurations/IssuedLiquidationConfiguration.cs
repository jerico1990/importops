using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class IssuedLiquidationConfiguration : IEntityTypeConfiguration<IssuedLiquidation>
{
    public void Configure(EntityTypeBuilder<IssuedLiquidation> b)
    {
        b.ToTable("ISSUED_LIQUIDATION", "fin");
        b.HasKey(x => x.IssuedLiquidationId);
        b.Property(x => x.IssuedLiquidationId).HasColumnName("issued_liquidation_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.SequenceNo).HasColumnName("sequence_no");
        b.Property(x => x.YearNo).HasColumnName("year_no");
        b.Property(x => x.Number).HasColumnName("number").HasMaxLength(24);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        b.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("nvarchar(max)");
        b.Property(x => x.IssuedAt).HasColumnName("issued_at");
        b.Property(x => x.IssuedBy).HasColumnName("issued_by");
        b.Property(x => x.FinancingDays).HasColumnName("financing_days");
        b.Property(x => x.SentAt).HasColumnName("sent_at");
        b.Property(x => x.SentBy).HasColumnName("sent_by");
        b.Property(x => x.SentVia).HasColumnName("sent_via").HasMaxLength(40);
        b.Property(x => x.SentTo).HasColumnName("sent_to").HasMaxLength(250);
        b.Property(x => x.DueAt).HasColumnName("due_at");
        b.HasIndex(x => new { x.CompanyId, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.YearNo, x.SequenceNo }).IsUnique();
    }
}
