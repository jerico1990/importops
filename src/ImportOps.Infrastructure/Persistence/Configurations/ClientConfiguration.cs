using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ImportOps.Infrastructure.Persistence.Configurations;
internal sealed class ClientConfiguration:IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("CLIENT","crm"); b.HasKey(x=>x.ClientId);
        b.Property(x=>x.ClientId).HasColumnName("client_id");
        b.Property(x=>x.CompanyId).HasColumnName("company_id");
        b.Property(x=>x.ClientType).HasColumnName("client_type").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x=>x.LegalName).HasColumnName("legal_name").HasMaxLength(200).IsRequired();
        b.Property(x=>x.TradeName).HasColumnName("trade_name").HasMaxLength(200);
        b.Property(x=>x.TaxId).HasColumnName("tax_id").HasMaxLength(30);
        b.Property(x=>x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x=>x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x=>x.CreditDays).HasColumnName("credit_days");
        b.Property(x=>x.PreferredCurrencyCode).HasColumnName("preferred_currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(x=>x.AccountManagerUserId).HasColumnName("account_manager_user_id");
        b.Property(x=>x.IsActive).HasColumnName("is_active");
        b.Property(x=>x.CreatedAt).HasColumnName("created_at");
        b.Property(x=>x.CreatedBy).HasColumnName("created_by");
        b.Property(x=>x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x=>x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x=>x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x=>x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasQueryFilter(x=>!x.IsDeleted);
        b.HasMany(x=>x.Contacts).WithOne(x=>x.Client).HasForeignKey(x=>x.ClientId);
        b.HasMany(x=>x.Addresses).WithOne(x=>x.Client).HasForeignKey(x=>x.ClientId);
    }
}