using ImportOps.Application.Alerts;
using ImportOps.Application.Catalogs;
using ImportOps.Application.Clients;
using ImportOps.Application.Customs;
using ImportOps.Application.Documents;
using ImportOps.Application.Finance;
using ImportOps.Application.Company;
using ImportOps.Application.ImportRequests;
using ImportOps.Application.Quotations;
using ImportOps.Application.Security;
using ImportOps.Application.Shipments;
using ImportOps.Application.Operations;
using ImportOps.Application.Suppliers;
using ImportOps.Application.Workflow;
using ImportOps.Infrastructure.Persistence;
using ImportOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("ImportOpsDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:ImportOpsDb.");

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ImportOpsDbContext>((sp, options) =>
            options
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogAdministrationService, CatalogAdministrationService>();
        services.AddScoped<IImportRequestService, ImportRequestService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IQuotationProformaService, QuotationProformaService>();
        services.AddSingleton<IPasswordHasher, LocalPasswordHasher>();
        services.AddScoped<ILocalAuthService, LocalAuthService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<ICustomsService, CustomsService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<ILiquidationService, LiquidationService>();
        services.AddScoped<IImportOperationService, ImportOperationService>();
        services.AddScoped<IWorkflowConfigurationService, WorkflowConfigurationService>();

        return services;
    }
}
