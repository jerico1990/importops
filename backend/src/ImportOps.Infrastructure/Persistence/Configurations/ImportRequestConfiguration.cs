using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class ImportRequestConfiguration : IEntityTypeConfiguration<ImportRequest>
{
    public void Configure(EntityTypeBuilder<ImportRequest> b)
    {
        b.ToTable("IMPORT_REQUEST", "sales");
        b.HasKey(x => x.RequestId);

        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.RequestNo).HasColumnName("request_no").HasMaxLength(30).IsRequired();

        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.ClientContactId).HasColumnName("client_contact_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
        b.Property(x => x.AssignedUserId).HasColumnName("assigned_user_id");

        b.Property(x => x.RequestDate).HasColumnName("request_date").HasColumnType("date");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsUnicode(false).IsRequired();

        b.Property(x => x.TransportModeId).HasColumnName("transport_mode_id");
        b.Property(x => x.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.DestinationCountryCode).HasColumnName("destination_country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.OriginPortId).HasColumnName("origin_port_id");
        b.Property(x => x.DestinationPortId).HasColumnName("destination_port_id");

        b.Property(x => x.MerchandiseDescription).HasColumnName("merchandise_description").HasMaxLength(1000).IsRequired();
        b.Property(x => x.PackageCount).HasColumnName("package_count").HasColumnType("decimal(18,3)");
        b.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasColumnType("decimal(18,3)");
        b.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasColumnType("decimal(18,4)");
        b.Property(x => x.DeclaredValue).HasColumnName("declared_value").HasColumnType("decimal(19,4)");
        b.Property(x => x.DeclaredCurrencyCode).HasColumnName("declared_currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.DesiredServiceDate).HasColumnName("desired_service_date").HasColumnType("date");

        b.Property(x => x.CustomerNotes).HasColumnName("customer_notes").HasMaxLength(2000);
        b.Property(x => x.InternalNotes).HasColumnName("internal_notes").HasMaxLength(2000);
        b.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        b.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id");

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ClientContact).WithMany().HasForeignKey(x => x.ClientContactId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.StatusHistory).WithOne(x => x.Request).HasForeignKey(x => x.RequestId);
    }
}

internal sealed class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> b)
    {
        b.ToTable("REQUEST_STATUS_HISTORY", "sales");
        b.HasKey(x => x.RequestStatusHistoryId);

        b.Property(x => x.RequestStatusHistoryId).HasColumnName("request_status_history_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        b.Property(x => x.ChangedAt).HasColumnName("changed_at");
        b.Property(x => x.ChangedBy).HasColumnName("changed_by");

        b.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
