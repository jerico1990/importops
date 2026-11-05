using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("SUPPLIER", "crm");
        b.HasKey(x => x.SupplierId);
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(50);
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x => x.AddressLine).HasColumnName("address_line").HasMaxLength(300);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted);
        b.HasMany(x => x.Contacts).WithOne(x => x.Supplier).HasForeignKey(x => x.SupplierId);
    }
}

internal sealed class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> b)
    {
        b.ToTable("SUPPLIER_CONTACT", "crm");
        b.HasKey(x => x.SupplierContactId);
        b.Property(x => x.SupplierContactId).HasColumnName("supplier_contact_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x => x.PositionName).HasColumnName("position_name").HasMaxLength(120);
        b.Property(x => x.IsPrimary).HasColumnName("is_primary");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}
