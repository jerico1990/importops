using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("APP_USER", "sec");
        b.HasKey(x => x.UserId);

        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ExternalAuthId).HasColumnName("external_auth_id").HasMaxLength(100);
        b.Property(x => x.UserType).HasColumnName("user_type").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
        b.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
        b.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

    }
}
