namespace ImportOps.Domain.Entities;
public sealed class ClientContact
{
    public long ClientContactId { get; set; }
    public long ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Client Client { get; set; } = null!;
}