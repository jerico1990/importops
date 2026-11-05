namespace ImportOps.Domain.Entities;

public sealed class AppUser
{
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public string? ExternalAuthId { get; set; }
    public string UserType { get; set; } = UserTypes.Internal;
    public long? ClientId { get; set; }
    public string? PasswordHash { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}


public static class UserTypes
{
    public const string Internal = "INTERNAL";
    public const string Client = "CLIENT";
}
