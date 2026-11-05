using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class CustomsProcessConfiguration : IEntityTypeConfiguration<CustomsProcess>
{
    public void Configure(EntityTypeBuilder<CustomsProcess> b)
    {
        b.ToTable("CUSTOMS_PROCESS", "ops");
        b.HasKey(x => x.CustomsProcessId);

        b.Property(x => x.CustomsProcessId).HasColumnName("customs_process_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ShipmentId).HasColumnName("shipment_id");

        b.Property(x => x.CustomsRegime).HasColumnName("customs_regime").HasMaxLength(80);
        b.Property(x => x.CustomsRegimeId).HasColumnName("customs_regime_id");
        b.Property(x => x.DeclarationNo).HasColumnName("declaration_no").HasMaxLength(100);
        b.Property(x => x.DeclarationDate).HasColumnName("declaration_date").HasColumnType("date");
        b.Property(x => x.CustomsOffice).HasColumnName("customs_office").HasMaxLength(150);
        b.Property(x => x.CustomsOfficeId).HasColumnName("customs_office_id");

        b.Property(x => x.CustomsChannel).HasColumnName("customs_channel").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.CustomsStatus).HasColumnName("customs_status").HasMaxLength(30).IsUnicode(false);

        b.Property(x => x.DutiesAmount).HasColumnName("duties_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.DutiesCurrencyCode).HasColumnName("duties_currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.DutiesExchangeRate).HasColumnName("duties_exchange_rate").HasColumnType("decimal(18,8)");
        b.Property(x => x.DutiesBaseAmount).HasColumnName("duties_base_amount").HasColumnType("decimal(19,4)");

        b.Property(x => x.ReleaseDate).HasColumnName("release_date");
        b.Property(x => x.Observation).HasColumnName("observation").HasMaxLength(1500);

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

        b.HasOne(x => x.Operation).WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Shipment).WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CustomsRegimeCatalog).WithMany().HasForeignKey(x => x.CustomsRegimeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CustomsOfficeCatalog).WithMany().HasForeignKey(x => x.CustomsOfficeId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Observations).WithOne(x => x.CustomsProcess).HasForeignKey(x => x.CustomsProcessId);
        b.HasMany(x => x.Taxes).WithOne(x => x.CustomsProcess).HasForeignKey(x => x.CustomsProcessId);
        b.HasMany(x => x.StatusHistory).WithOne(x => x.CustomsProcess).HasForeignKey(x => x.CustomsProcessId);
    }
}

internal sealed class CustomsObservationConfiguration : IEntityTypeConfiguration<CustomsObservation>
{
    public void Configure(EntityTypeBuilder<CustomsObservation> b)
    {
        b.ToTable("CUSTOMS_OBSERVATION", "ops");
        b.HasKey(x => x.CustomsObservationId);

        b.Property(x => x.CustomsObservationId).HasColumnName("customs_observation_id");
        b.Property(x => x.CustomsProcessId).HasColumnName("customs_process_id");
        b.Property(x => x.ObservationType).HasColumnName("observation_type").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1500);
        b.Property(x => x.ObservationStatus).HasColumnName("observation_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.ObservedAt).HasColumnName("observed_at");
        b.Property(x => x.ObservedBy).HasColumnName("observed_by");
        b.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        b.Property(x => x.ResolvedBy).HasColumnName("resolved_by");
        b.Property(x => x.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1500);
        b.Property(x => x.OperationTaskId).HasColumnName("operation_task_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
    }
}

internal sealed class CustomsTaxConfiguration : IEntityTypeConfiguration<CustomsTax>
{
    public void Configure(EntityTypeBuilder<CustomsTax> b)
    {
        b.ToTable("CUSTOMS_TAX", "ops");
        b.HasKey(x => x.CustomsTaxId);

        b.Property(x => x.CustomsTaxId).HasColumnName("customs_tax_id");
        b.Property(x => x.CustomsProcessId).HasColumnName("customs_process_id");
        b.Property(x => x.TaxType).HasColumnName("tax_type").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.CustomsTaxConceptId).HasColumnName("customs_tax_concept_id");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(300);
        b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.ExchangeRate).HasColumnName("exchange_rate").HasColumnType("decimal(18,8)");
        b.Property(x => x.BaseAmount).HasColumnName("base_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(120);
        b.Property(x => x.PaidAt).HasColumnName("paid_at");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasOne(x => x.CustomsTaxConcept).WithMany().HasForeignKey(x => x.CustomsTaxConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomsStatusHistoryConfiguration : IEntityTypeConfiguration<CustomsStatusHistory>
{
    public void Configure(EntityTypeBuilder<CustomsStatusHistory> b)
    {
        b.ToTable("CUSTOMS_STATUS_HISTORY", "ops");
        b.HasKey(x => x.CustomsStatusHistoryId);

        b.Property(x => x.CustomsStatusHistoryId).HasColumnName("customs_status_history_id");
        b.Property(x => x.CustomsProcessId).HasColumnName("customs_process_id");
        b.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        b.Property(x => x.ChangedAt).HasColumnName("changed_at");
        b.Property(x => x.ChangedBy).HasColumnName("changed_by");
    }
}
