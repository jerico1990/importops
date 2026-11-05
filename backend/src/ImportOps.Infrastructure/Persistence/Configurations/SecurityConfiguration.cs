using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("ROLE", "sec");
        b.HasKey(x => x.RoleId);
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.IsSystem).HasColumnName("is_system");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("USER_ROLE", "sec");
        b.HasKey(x => new { x.UserId, x.RoleId });
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.AssignedAt).HasColumnName("assigned_at");
        b.Property(x => x.AssignedBy).HasColumnName("assigned_by");
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("PERMISSION", "sec");
        b.HasKey(x => x.PermissionId);
        b.Property(x => x.PermissionId).HasColumnName("permission_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
        b.Property(x => x.ModuleCode).HasColumnName("module_code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("ROLE_PERMISSION", "sec");
        b.HasKey(x => new { x.RoleId, x.PermissionId });
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.PermissionId).HasColumnName("permission_id");
        b.Property(x => x.CanView).HasColumnName("can_view");
        b.Property(x => x.CanCreate).HasColumnName("can_create");
        b.Property(x => x.CanUpdate).HasColumnName("can_update");
        b.Property(x => x.CanDelete).HasColumnName("can_delete");
        b.Property(x => x.CanApprove).HasColumnName("can_approve");
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AUDIT_LOG", "audit");
        b.HasKey(x => x.AuditLogId);
        b.Property(x => x.AuditLogId).HasColumnName("audit_log_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.EventAt).HasColumnName("event_at");
        b.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(128);
        b.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        b.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.OldValuesJson).HasColumnName("old_values_json");
        b.Property(x => x.NewValuesJson).HasColumnName("new_values_json");
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
    }
}
