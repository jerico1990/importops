using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("COMPANY", "cfg");
        b.HasKey(x => x.CompanyId);
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(200).IsRequired();
        b.Property(x => x.TradeName).HasColumnName("trade_name").HasMaxLength(200);
        b.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(30).IsRequired();
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(x => x.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x => x.AddressLine).HasColumnName("address_line").HasMaxLength(300);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
