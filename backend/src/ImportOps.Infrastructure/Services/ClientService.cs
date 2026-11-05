using ImportOps.Application.Clients;
using ImportOps.Application.Common;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class ClientService(ImportOpsDbContext db, ICurrentCompany currentCompany, ICurrentUser currentUser) : IClientService
{
    public async Task<PagedResult<ClientListItemDto>> GetAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.Clients.AsNoTracking().Where(x => x.CompanyId == currentCompany.CompanyId);

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                return new(Array.Empty<ClientListItemDto>(), page, pageSize, 0);

            q = q.Where(x => x.ClientId == currentUser.ClientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x => x.LegalName.Contains(term)
                || (x.TradeName != null && x.TradeName.Contains(term))
                || (x.TaxId != null && x.TaxId.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.LegalName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ClientListItemDto(x.ClientId, x.ClientType, x.LegalName, x.TradeName, x.TaxId, x.Email, x.Phone, x.IsActive))
            .ToListAsync(ct);

        return new(items, page, pageSize, total);
    }

    public Task<ClientDetailDto?> GetByIdAsync(long id, CancellationToken ct) =>
        db.Clients.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId && x.ClientId == id && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId))
            .Select(x => new ClientDetailDto(
                x.ClientId, x.ClientType, x.LegalName, x.TradeName, x.TaxId, x.CountryCode,
                x.Email, x.Phone, x.CreditDays, x.PreferredCurrencyCode, x.Notes, x.AccountManagerUserId, x.IsActive, ConcurrencyToken.Encode(x.RowVer),
                x.Contacts.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.FullName)
                    .Select(c => new ClientContactDto(c.ClientContactId, c.FullName, c.PositionName, c.Email, c.Phone, c.Mobile, c.IsPrimary, c.IsActive)).ToList(),
                x.Addresses.OrderByDescending(a => a.IsPrimary)
                    .Select(a => new ClientAddressDto(a.ClientAddressId, a.AddressType, a.AddressLine, a.District, a.Province, a.Department, a.CountryCode, a.IsPrimary, a.IsActive)).ToList()))
            .SingleOrDefaultAsync(ct);

    public async Task<ClientDetailDto> CreateAsync(CreateClientRequest r, CancellationToken ct)
    {
        Validate(r);
        await EnsureAccountManagerValid(r.AccountManagerUserId, ct);
        await EnsureTaxIdUnique(r.TaxId, null, ct);

        var e = new Client
        {
            CompanyId = currentCompany.CompanyId,
            ClientType = NormalizeType(r.ClientType),
            LegalName = r.LegalName.Trim(),
            TradeName = Clean(r.TradeName),
            TaxId = Clean(r.TaxId),
            CountryCode = r.CountryCode.Trim().ToUpperInvariant(),
            Email = Clean(r.Email),
            Phone = Clean(r.Phone),
            CreditDays = r.CreditDays,
            PreferredCurrencyCode = Clean(r.PreferredCurrencyCode)?.ToUpperInvariant(),
            Notes = Clean(r.Notes),
            AccountManagerUserId = r.AccountManagerUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Clients.Add(e);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(e.ClientId, ct))!;
    }

    public async Task<ClientDetailDto?> UpdateAsync(long id, UpdateClientRequest r, CancellationToken ct)
    {
        Validate(r);
        await EnsureAccountManagerValid(r.AccountManagerUserId, ct);
        var e = await db.Clients.SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.ClientId == id, ct);
        if (e is null) return null;

        await EnsureTaxIdUnique(r.TaxId, id, ct);
        db.Entry(e).Property(x => x.RowVer).OriginalValue = ConcurrencyToken.Decode(r.RowVersion);

        e.ClientType = NormalizeType(r.ClientType);
        e.LegalName = r.LegalName.Trim();
        e.TradeName = Clean(r.TradeName);
        e.TaxId = Clean(r.TaxId);
        e.CountryCode = r.CountryCode.Trim().ToUpperInvariant();
        e.Email = Clean(r.Email);
        e.Phone = Clean(r.Phone);
        e.CreditDays = r.CreditDays;
        e.PreferredCurrencyCode = Clean(r.PreferredCurrencyCode)?.ToUpperInvariant();
        e.Notes = Clean(r.Notes);
        e.AccountManagerUserId = r.AccountManagerUserId;
        e.IsActive = r.IsActive;
        e.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var e = await db.Clients.SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId && x.ClientId == id, ct);
        if (e is null) return false;
        e.IsDeleted = true;
        e.IsActive = false;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ClientContactDto?> AddContactAsync(long clientId, CreateClientContactRequest r, CancellationToken ct)
    {
        ValidateContact(r.FullName);
        if (!await OwnsClient(clientId, ct)) return null;
        if (r.IsPrimary) await ClearPrimaryContact(clientId, null, ct);

        var e = new ClientContact
        {
            ClientId = clientId, FullName = r.FullName.Trim(), PositionName = Clean(r.PositionName),
            Email = Clean(r.Email), Phone = Clean(r.Phone), Mobile = Clean(r.Mobile),
            IsPrimary = r.IsPrimary, IsActive = true, CreatedAt = DateTime.UtcNow
        };

        db.ClientContacts.Add(e);
        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<ClientContactDto?> UpdateContactAsync(long clientId, long contactId, UpdateClientContactRequest r, CancellationToken ct)
    {
        ValidateContact(r.FullName);
        if (!await OwnsClient(clientId, ct)) return null;

        var e = await db.ClientContacts.SingleOrDefaultAsync(x => x.ClientId == clientId && x.ClientContactId == contactId, ct);
        if (e is null) return null;

        if (r.IsPrimary) await ClearPrimaryContact(clientId, contactId, ct);

        e.FullName = r.FullName.Trim();
        e.PositionName = Clean(r.PositionName);
        e.Email = Clean(r.Email);
        e.Phone = Clean(r.Phone);
        e.Mobile = Clean(r.Mobile);
        e.IsPrimary = r.IsPrimary;
        e.IsActive = r.IsActive;

        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<bool> DeleteContactAsync(long clientId, long contactId, CancellationToken ct)
    {
        if (!await OwnsClient(clientId, ct)) return false;
        var e = await db.ClientContacts.SingleOrDefaultAsync(x => x.ClientId == clientId && x.ClientContactId == contactId, ct);
        if (e is null) return false;
        e.IsActive = false;
        e.IsPrimary = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ClientAddressDto?> AddAddressAsync(long clientId, CreateClientAddressRequest r, CancellationToken ct)
    {
        ValidateAddress(r.AddressLine, r.AddressType);
        if (!await OwnsClient(clientId, ct)) return null;

        var type = NormalizeAddressType(r.AddressType);
        if (r.IsPrimary) await ClearPrimaryAddress(clientId, null, ct);

        var e = new ClientAddress
        {
            ClientId = clientId, AddressType = type, AddressLine = r.AddressLine.Trim(),
            District = Clean(r.District), Province = Clean(r.Province), Department = Clean(r.Department),
            CountryCode = r.CountryCode.Trim().ToUpperInvariant(), IsPrimary = r.IsPrimary, IsActive = true
        };

        db.ClientAddresses.Add(e);
        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<ClientAddressDto?> UpdateAddressAsync(long clientId, long addressId, UpdateClientAddressRequest r, CancellationToken ct)
    {
        ValidateAddress(r.AddressLine, r.AddressType);
        if (!await OwnsClient(clientId, ct)) return null;

        var e = await db.ClientAddresses.SingleOrDefaultAsync(x => x.ClientId == clientId && x.ClientAddressId == addressId, ct);
        if (e is null) return null;

        if (r.IsPrimary) await ClearPrimaryAddress(clientId, addressId, ct);

        e.AddressType = NormalizeAddressType(r.AddressType);
        e.AddressLine = r.AddressLine.Trim();
        e.District = Clean(r.District);
        e.Province = Clean(r.Province);
        e.Department = Clean(r.Department);
        e.CountryCode = r.CountryCode.Trim().ToUpperInvariant();
        e.IsPrimary = r.IsPrimary;
        e.IsActive = r.IsActive;

        await db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<bool> DeleteAddressAsync(long clientId, long addressId, CancellationToken ct)
    {
        if (!await OwnsClient(clientId, ct)) return false;
        var e = await db.ClientAddresses.SingleOrDefaultAsync(x => x.ClientId == clientId && x.ClientAddressId == addressId, ct);
        if (e is null) return false;
        e.IsActive = false;
        e.IsPrimary = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> OwnsClient(long id, CancellationToken ct) =>
        await db.Clients.AnyAsync(x => x.CompanyId == currentCompany.CompanyId && x.ClientId == id && (!currentUser.IsClientUser || x.ClientId == currentUser.ClientId), ct);

    private async Task ClearPrimaryContact(long clientId, long? exceptId, CancellationToken ct)
    {
        var rows = await db.ClientContacts
            .Where(x => x.ClientId == clientId && x.IsPrimary && (!exceptId.HasValue || x.ClientContactId != exceptId.Value))
            .ToListAsync(ct);
        rows.ForEach(x => x.IsPrimary = false);
    }

    private async Task ClearPrimaryAddress(long clientId, long? exceptId, CancellationToken ct)
    {
        var rows = await db.ClientAddresses
            .Where(x => x.ClientId == clientId && x.IsPrimary && (!exceptId.HasValue || x.ClientAddressId != exceptId.Value))
            .ToListAsync(ct);
        rows.ForEach(x => x.IsPrimary = false);
    }

    private async Task EnsureAccountManagerValid(long? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return;

        var valid = await db.AppUsers.AsNoTracking().AnyAsync(u =>
            u.UserId == userId.Value &&
            u.CompanyId == currentCompany.CompanyId &&
            u.IsActive &&
            !u.IsDeleted &&
            u.UserType == UserTypes.Internal &&
            !u.Username.StartsWith("system.") &&
            (from ur in db.UserRoles
             join r in db.Roles on ur.RoleId equals r.RoleId
             where ur.UserId == u.UserId &&
                   r.CompanyId == currentCompany.CompanyId &&
                   r.IsActive &&
                   (r.Code == "ADMIN" || r.Code == "COMMERCIAL")
             select ur.UserId).Any(), ct);

        if (!valid)
            throw new BusinessException(
                "INVALID_ACCOUNT_MANAGER",
                "El ejecutivo de cuenta debe ser un usuario interno activo con perfil ADMIN o COMMERCIAL.",
                400);
    }

    private async Task EnsureTaxIdUnique(string? taxId, long? exceptId, CancellationToken ct)
    {
        var clean = Clean(taxId);
        if (clean is null) return;
        var exists = await db.Clients.AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId && x.TaxId == clean &&
            (!exceptId.HasValue || x.ClientId != exceptId.Value), ct);
        if (exists) throw new InvalidOperationException("Ya existe un cliente con ese identificador tributario.");
    }

    private static void Validate(CreateClientRequest r)
    {
        _ = NormalizeType(r.ClientType);
        if (string.IsNullOrWhiteSpace(r.LegalName)) throw new ArgumentException("La razón social/nombre es obligatorio.");
        if (r.CreditDays < 0) throw new ArgumentException("CreditDays no puede ser negativo.");
    }

    private static void ValidateContact(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("El nombre del contacto es obligatorio.");
    }

    private static void ValidateAddress(string addressLine, string type)
    {
        if (string.IsNullOrWhiteSpace(addressLine)) throw new ArgumentException("La dirección es obligatoria.");
        _ = NormalizeAddressType(type);
    }

    private static string NormalizeType(string value)
    {
        var x = value.Trim().ToUpperInvariant();
        if (x is not ("COMPANY" or "PERSON")) throw new ArgumentException("ClientType debe ser COMPANY o PERSON.");
        return x;
    }

    private static string NormalizeAddressType(string value)
    {
        var x = value.Trim().ToUpperInvariant();
        if (x is not ("LEGAL" or "BILLING" or "DELIVERY" or "OTHER")) throw new ArgumentException("Tipo de dirección inválido.");
        return x;
    }

    private static ClientContactDto Map(ClientContact e) =>
        new(e.ClientContactId, e.FullName, e.PositionName, e.Email, e.Phone, e.Mobile, e.IsPrimary, e.IsActive);

    private static ClientAddressDto Map(ClientAddress e) =>
        new(e.ClientAddressId, e.AddressType, e.AddressLine, e.District, e.Province, e.Department, e.CountryCode, e.IsPrimary, e.IsActive);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
