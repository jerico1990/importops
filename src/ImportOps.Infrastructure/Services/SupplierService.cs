using ImportOps.Application.Common;
using ImportOps.Application.Suppliers;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class SupplierService(ImportOpsDbContext db, ICurrentCompany currentCompany) : ISupplierService
{
    public async Task<PagedResult<SupplierListItemDto>> GetAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.Suppliers.AsNoTracking().Where(x => x.CompanyId == currentCompany.CompanyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x => x.LegalName.Contains(term)
                || (x.TaxId != null && x.TaxId.Contains(term))
                || (x.Email != null && x.Email.Contains(term)));
        }

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.LegalName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SupplierListItemDto(x.SupplierId, x.LegalName, x.TaxId, x.CountryCode, x.Email, x.Phone, x.IsActive))
            .ToListAsync(ct);

        return new(items, page, pageSize, total);
    }

    public Task<SupplierDetailDto?> GetByIdAsync(long id, CancellationToken ct) =>
        db.Suppliers.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.SupplierId == id)
            .Select(x => new SupplierDetailDto(
                x.SupplierId, x.LegalName, x.TaxId, x.CountryCode, x.Email, x.Phone, x.AddressLine, x.Notes, x.IsActive, ConcurrencyToken.Encode(x.RowVer),
                x.Contacts.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.FullName)
                    .Select(c => new SupplierContactDto(c.SupplierContactId, c.FullName, c.Email, c.Phone, c.PositionName, c.IsPrimary, c.IsActive))
                    .ToList()))
            .SingleOrDefaultAsync(ct);

    public async Task<SupplierDetailDto> CreateAsync(CreateSupplierRequest r, CancellationToken ct)
    {
        Validate(r);
        await EnsureTaxIdUnique(r.TaxId, null, ct);

        var entity = new Supplier
        {
            CompanyId = currentCompany.CompanyId,
            LegalName = r.LegalName.Trim(),
            TaxId = Clean(r.TaxId),
            CountryCode = Clean(r.CountryCode)?.ToUpperInvariant(),
            Email = Clean(r.Email),
            Phone = Clean(r.Phone),
            AddressLine = Clean(r.AddressLine),
            Notes = Clean(r.Notes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Suppliers.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.SupplierId, ct))!;
    }

    public async Task<SupplierDetailDto?> UpdateAsync(long id, UpdateSupplierRequest r, CancellationToken ct)
    {
        Validate(r);
        var entity = await db.Suppliers.SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.SupplierId == id, ct);
        if (entity is null) return null;

        await EnsureTaxIdUnique(r.TaxId, id, ct);
        db.Entry(entity).Property(x => x.RowVer).OriginalValue = ConcurrencyToken.Decode(r.RowVersion);

        entity.LegalName = r.LegalName.Trim();
        entity.TaxId = Clean(r.TaxId);
        entity.CountryCode = Clean(r.CountryCode)?.ToUpperInvariant();
        entity.Email = Clean(r.Email);
        entity.Phone = Clean(r.Phone);
        entity.AddressLine = Clean(r.AddressLine);
        entity.Notes = Clean(r.Notes);
        entity.IsActive = r.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await db.Suppliers.SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.SupplierId == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SupplierContactDto?> AddContactAsync(long supplierId, UpsertSupplierContactRequest r, CancellationToken ct)
    {
        ValidateContact(r);
        if (!await OwnsSupplier(supplierId, ct)) return null;
        if (r.IsPrimary) await ClearPrimary(supplierId, null, ct);

        var e = new SupplierContact
        {
            SupplierId = supplierId,
            FullName = r.FullName.Trim(),
            Email = Clean(r.Email),
            Phone = Clean(r.Phone),
            PositionName = Clean(r.PositionName),
            IsPrimary = r.IsPrimary,
            IsActive = r.IsActive
        };

        db.SupplierContacts.Add(e);
        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<SupplierContactDto?> UpdateContactAsync(long supplierId, long contactId, UpsertSupplierContactRequest r, CancellationToken ct)
    {
        ValidateContact(r);
        if (!await OwnsSupplier(supplierId, ct)) return null;

        var e = await db.SupplierContacts.SingleOrDefaultAsync(x => x.SupplierId == supplierId && x.SupplierContactId == contactId, ct);
        if (e is null) return null;

        if (r.IsPrimary) await ClearPrimary(supplierId, contactId, ct);

        e.FullName = r.FullName.Trim();
        e.Email = Clean(r.Email);
        e.Phone = Clean(r.Phone);
        e.PositionName = Clean(r.PositionName);
        e.IsPrimary = r.IsPrimary;
        e.IsActive = r.IsActive;
        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<bool> DeleteContactAsync(long supplierId, long contactId, CancellationToken ct)
    {
        if (!await OwnsSupplier(supplierId, ct)) return false;
        var e = await db.SupplierContacts.SingleOrDefaultAsync(x => x.SupplierId == supplierId && x.SupplierContactId == contactId, ct);
        if (e is null) return false;
        e.IsActive = false;
        e.IsPrimary = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> OwnsSupplier(long id, CancellationToken ct) =>
        await db.Suppliers.AnyAsync(x => x.CompanyId == currentCompany.CompanyId && x.SupplierId == id, ct);

    private async Task ClearPrimary(long supplierId, long? exceptId, CancellationToken ct)
    {
        var rows = await db.SupplierContacts
            .Where(x => x.SupplierId == supplierId && x.IsPrimary && (!exceptId.HasValue || x.SupplierContactId != exceptId.Value))
            .ToListAsync(ct);
        rows.ForEach(x => x.IsPrimary = false);
    }

    private async Task EnsureTaxIdUnique(string? taxId, long? exceptId, CancellationToken ct)
    {
        var clean = Clean(taxId);
        if (clean is null) return;
        var exists = await db.Suppliers.AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId && x.TaxId == clean &&
            (!exceptId.HasValue || x.SupplierId != exceptId.Value), ct);
        if (exists) throw new InvalidOperationException("Ya existe un proveedor con ese identificador.");
    }

    private static void Validate(CreateSupplierRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.LegalName)) throw new ArgumentException("La razón social/nombre del proveedor es obligatoria.");
    }

    private static void ValidateContact(UpsertSupplierContactRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.FullName)) throw new ArgumentException("El nombre del contacto es obligatorio.");
    }

    private static SupplierContactDto Map(SupplierContact e) =>
        new(e.SupplierContactId, e.FullName, e.Email, e.Phone, e.PositionName, e.IsPrimary, e.IsActive);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
