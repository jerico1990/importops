using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ImportOps.Infrastructure.Persistence.Configurations;
internal sealed class ClientContactConfiguration:IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> b)
    {
        b.ToTable("CLIENT_CONTACT","crm"); b.HasKey(x=>x.ClientContactId);
        b.Property(x=>x.ClientContactId).HasColumnName("client_contact_id");
        b.Property(x=>x.ClientId).HasColumnName("client_id");
        b.Property(x=>x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        b.Property(x=>x.PositionName).HasColumnName("position_name").HasMaxLength(120);
        b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x=>x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x=>x.Mobile).HasColumnName("mobile").HasMaxLength(50);
        b.Property(x=>x.IsPrimary).HasColumnName("is_primary");
        b.Property(x=>x.IsActive).HasColumnName("is_active");
        b.Property(x=>x.CreatedAt).HasColumnName("created_at");
    }
}