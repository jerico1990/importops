namespace ImportOps.Domain.Entities;

public sealed class Role
{
    public int RoleId { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UserRole
{
    public long UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public long? AssignedBy { get; set; }
}

public sealed class Permission
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}

public sealed class AuditLog
{
    public long AuditLogId { get; set; }
    public long? CompanyId { get; set; }
    public long? UserId { get; set; }
    public DateTime EventAt { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string ActionType { get; set; } = "OTHER";
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }
}
