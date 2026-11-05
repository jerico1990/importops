using ImportOps.Application.Common;
using ImportOps.Application.Company;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class CompanyService(ImportOpsDbContext db, ICurrentCompany currentCompany) : ICompanyService
{
    public Task<CompanyDto?> GetCurrentAsync(CancellationToken ct) =>
        db.Companies.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId)
            .Select(x => new CompanyDto(x.CompanyId, x.LegalName, x.TradeName, x.TaxId, x.CountryCode,
                x.BaseCurrencyCode, x.Email, x.Phone, x.AddressLine, x.IsActive))
            .SingleOrDefaultAsync(ct);

    public async Task<CompanyDto?> UpdateCurrentAsync(UpdateCompanyRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.LegalName)) throw new ArgumentException("La razón social es obligatoria.");
        if (string.IsNullOrWhiteSpace(r.TaxId)) throw new ArgumentException("El identificador tributario es obligatorio.");

        var company = await db.Companies.SingleOrDefaultAsync(x => x.CompanyId == currentCompany.CompanyId, ct);
        if (company is null) return null;

        var tax = r.TaxId.Trim();
        if (await db.Companies.AnyAsync(x => x.TaxId == tax && x.CompanyId != company.CompanyId, ct))
            throw new InvalidOperationException("Ya existe otra empresa con ese identificador tributario.");

        company.LegalName = r.LegalName.Trim();
        company.TradeName = Clean(r.TradeName);
        company.TaxId = tax;
        company.CountryCode = r.CountryCode.Trim().ToUpperInvariant();
        company.BaseCurrencyCode = r.BaseCurrencyCode.Trim().ToUpperInvariant();
        company.Email = Clean(r.Email);
        company.Phone = Clean(r.Phone);
        company.AddressLine = Clean(r.AddressLine);
        company.IsActive = r.IsActive;
        company.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetCurrentAsync(ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
