using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> b)
    {
        b.ToTable("QUOTATION", "sales");
        b.HasKey(x => x.QuotationId);

        b.Property(x => x.QuotationId).HasColumnName("quotation_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.QuotationNo).HasColumnName("quotation_no").HasMaxLength(30).IsRequired();
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.VersionNo).HasColumnName("version_no");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(x => x.IssueDate).HasColumnName("issue_date").HasColumnType("date");
        b.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("date");
        b.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(19,4)");
        b.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.Terms).HasColumnName("terms").HasMaxLength(2000);
        b.Property(x => x.InternalNotes).HasColumnName("internal_notes").HasMaxLength(2000);
        b.Property(x => x.ReferenceFile).HasColumnName("reference_file").HasMaxLength(100);
        b.Property(x => x.ContentDescription).HasColumnName("content_description").HasMaxLength(500);
        b.Property(x => x.CustomsRegime).HasColumnName("customs_regime").HasMaxLength(120);
        b.Property(x => x.CustomsOffice).HasColumnName("customs_office").HasMaxLength(160);
        b.Property(x => x.CustomsRegimeId).HasColumnName("customs_regime_id");
        b.Property(x => x.CustomsOfficeId).HasColumnName("customs_office_id");
        b.Property(x => x.EstimatedArrivalDate).HasColumnName("estimated_arrival_date").HasColumnType("date");
        b.Property(x => x.ExchangeRate).HasColumnName("exchange_rate").HasColumnType("decimal(19,6)");
        b.Property(x => x.ExwAmount).HasColumnName("exw_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.FreightAmount).HasColumnName("freight_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.InsuranceAmount).HasColumnName("insurance_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.CustomsValue).HasColumnName("customs_value").HasColumnType("decimal(19,4)");
        b.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").HasColumnType("decimal(18,3)");
        b.Property(x => x.PackageCount).HasColumnName("package_count").HasColumnType("decimal(18,3)");
        b.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasColumnType("decimal(18,4)");
        b.Property(x => x.CustomsDutiesAmount).HasColumnName("customs_duties_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.ExpenseAmount).HasColumnName("expense_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.ExpenseTaxAmount).HasColumnName("expense_tax_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.BankInstructions).HasColumnName("bank_instructions").HasMaxLength(1000);
        b.Property(x => x.PublicNotes).HasColumnName("public_notes").HasMaxLength(2000);
        b.Property(x => x.SentAt).HasColumnName("sent_at");
        b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        b.Property(x => x.ApprovedByName).HasColumnName("approved_by_name").HasMaxLength(160);
        b.Property(x => x.RejectedAt).HasColumnName("rejected_at");
        b.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CustomsRegimeCatalog).WithMany().HasForeignKey(x => x.CustomsRegimeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CustomsOfficeCatalog).WithMany().HasForeignKey(x => x.CustomsOfficeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Items).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Suppliers).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.CargoItems).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.CustomsTaxes).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.StatusHistory).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> b)
    {
        b.ToTable("QUOTATION_ITEM", "sales");
        b.HasKey(x => x.QuotationItemId);

        b.Property(x => x.QuotationItemId).HasColumnName("quotation_item_id");
        b.Property(x => x.QuotationId).HasColumnName("quotation_id");
        b.Property(x => x.LineNo).HasColumnName("line_no");
        b.Property(x => x.ItemType).HasColumnName("item_type").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        b.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(19,4)");
        b.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.TaxRate).HasColumnName("tax_rate").HasColumnType("decimal(9,4)");
        b.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(19,4)");
        b.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("decimal(19,4)");
        b.Property(x => x.ExpenseCategoryId).HasColumnName("expense_category_id");
        b.Property(x => x.ServiceCategoryId).HasColumnName("service_category_id");
        b.Property(x => x.IsRebillable).HasColumnName("is_rebillable");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        b.Property(x => x.QuotationConceptId).HasColumnName("quotation_concept_id");
        b.Property(x => x.IsTaxable).HasColumnName("is_taxable");
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
    }
}

internal sealed class QuotationStatusHistoryConfiguration : IEntityTypeConfiguration<QuotationStatusHistory>
{
    public void Configure(EntityTypeBuilder<QuotationStatusHistory> b)
    {
        b.ToTable("QUOTATION_STATUS_HISTORY", "sales");
        b.HasKey(x => x.QuotationStatusHistoryId);

        b.Property(x => x.QuotationStatusHistoryId).HasColumnName("quotation_status_history_id");
        b.Property(x => x.QuotationId).HasColumnName("quotation_id");
        b.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        b.Property(x => x.ChangedAt).HasColumnName("changed_at");
        b.Property(x => x.ChangedBy).HasColumnName("changed_by");

        b.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
