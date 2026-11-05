using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ImportOps.Infrastructure.Persistence.Configurations;
internal sealed class ClientAddressConfiguration:IEntityTypeConfiguration<ClientAddress>
{
    public void Configure(EntityTypeBuilder<ClientAddress> b)
    {
        b.ToTable("CLIENT_ADDRESS","crm"); b.HasKey(x=>x.ClientAddressId);
        b.Property(x=>x.ClientAddressId).HasColumnName("client_address_id");
        b.Property(x=>x.ClientId).HasColumnName("client_id");
        b.Property(x=>x.AddressType).HasColumnName("address_type").HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x=>x.AddressLine).HasColumnName("address_line").HasMaxLength(300).IsRequired();
        b.Property(x=>x.District).HasColumnName("district").HasMaxLength(120);
        b.Property(x=>x.Province).HasColumnName("province").HasMaxLength(120);
        b.Property(x=>x.Department).HasColumnName("department").HasMaxLength(120);
        b.Property(x=>x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(x=>x.IsPrimary).HasColumnName("is_primary");
        b.Property(x=>x.IsActive).HasColumnName("is_active");
    }
}