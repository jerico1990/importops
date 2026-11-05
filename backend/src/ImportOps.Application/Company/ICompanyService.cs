namespace ImportOps.Application.Company;

public interface ICompanyService
{
    Task<CompanyDto?> GetCurrentAsync(CancellationToken ct);
    Task<CompanyDto?> UpdateCurrentAsync(UpdateCompanyRequest request, CancellationToken ct);
}
