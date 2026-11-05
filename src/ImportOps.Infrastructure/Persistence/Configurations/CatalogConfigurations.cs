using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> b)
    {
        b.ToTable("CURRENCY", "cfg"); b.HasKey(x => x.CurrencyCode);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.CurrencyName).HasColumnName("currency_name").HasMaxLength(80);
        b.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(10);
        b.Property(x => x.DecimalPlaces).HasColumnName("decimal_places");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.ToTable("COUNTRY", "cfg"); b.HasKey(x => x.CountryCode);
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.CountryName).HasColumnName("country_name").HasMaxLength(120);
        b.Property(x => x.Iso3Code).HasColumnName("iso3_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class PortConfiguration : IEntityTypeConfiguration<Port>
{
    public void Configure(EntityTypeBuilder<Port> b)
    {
        b.ToTable("PORT", "cfg"); b.HasKey(x => x.PortId);
        b.Property(x => x.PortId).HasColumnName("port_id");
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.PortCode).HasColumnName("port_code").HasMaxLength(20);
        b.Property(x => x.PortName).HasColumnName("port_name").HasMaxLength(160);
        b.Property(x => x.CityName).HasColumnName("city_name").HasMaxLength(120);
        b.Property(x => x.PortType).HasColumnName("port_type").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class TransportModeConfiguration : IEntityTypeConfiguration<TransportMode>
{
    public void Configure(EntityTypeBuilder<TransportMode> b)
    {
        b.ToTable("TRANSPORT_MODE", "cfg"); b.HasKey(x => x.TransportModeId);
        b.Property(x => x.TransportModeId).HasColumnName("transport_mode_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class IncotermConfiguration : IEntityTypeConfiguration<Incoterm>
{
    public void Configure(EntityTypeBuilder<Incoterm> b)
    {
        b.ToTable("INCOTERM", "cfg"); b.HasKey(x => x.IncotermCode);
        b.Property(x => x.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(200);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> b)
    {
        b.ToTable("PAYMENT_METHOD", "cfg"); b.HasKey(x => x.PaymentMethodId);
        b.Property(x => x.PaymentMethodId).HasColumnName("payment_method_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> b)
    {
        b.ToTable("DOCUMENT_TYPE", "cfg"); b.HasKey(x => x.DocumentTypeId);
        b.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.IsRequiredDefault).HasColumnName("is_required_default");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> b)
    {
        b.ToTable("EXPENSE_CATEGORY", "cfg"); b.HasKey(x => x.ExpenseCategoryId);
        b.Property(x => x.ExpenseCategoryId).HasColumnName("expense_category_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
        b.Property(x => x.IsRebillableDefault).HasColumnName("is_rebillable_default");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> b)
    {
        b.ToTable("SERVICE_CATEGORY", "cfg"); b.HasKey(x => x.ServiceCategoryId);
        b.Property(x => x.ServiceCategoryId).HasColumnName("service_category_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class WorkflowStageConfiguration : IEntityTypeConfiguration<WorkflowStage>
{
    public void Configure(EntityTypeBuilder<WorkflowStage> b)
    {
        b.ToTable("WORKFLOW_STAGE", "cfg"); b.HasKey(x => x.WorkflowStageId);
        b.Property(x => x.WorkflowStageId).HasColumnName("workflow_stage_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsFinal).HasColumnName("is_final");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}
