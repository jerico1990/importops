namespace ImportOps.Domain.Entities;

public sealed class SupplierContact
{
    public long SupplierContactId { get; set; }
    public long SupplierId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PositionName { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public Supplier Supplier { get; set; } = null!;
}
