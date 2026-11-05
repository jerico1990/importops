using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

public sealed class CustomsRegimeConfiguration : IEntityTypeConfiguration<CustomsRegime>
{
    public void Configure(EntityTypeBuilder<CustomsRegime> b)
    {
        b.ToTable("CUSTOMS_REGIME", "cfg"); b.HasKey(x => x.CustomsRegimeId);
        b.Property(x => x.CustomsRegimeId).HasColumnName("customs_regime_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120); b.Property(x => x.DisplayOrder).HasColumnName("display_order"); b.Property(x => x.IsActive).HasColumnName("is_active"); b.Property(x => x.CreatedAt).HasColumnName("created_at"); b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
    }
}
public sealed class CustomsOfficeCatalogConfiguration : IEntityTypeConfiguration<CustomsOfficeCatalog>
{
    public void Configure(EntityTypeBuilder<CustomsOfficeCatalog> b)
    {
        b.ToTable("CUSTOMS_OFFICE", "cfg"); b.HasKey(x => x.CustomsOfficeId);
        b.Property(x => x.CustomsOfficeId).HasColumnName("customs_office_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(30); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(160); b.Property(x => x.DisplayOrder).HasColumnName("display_order"); b.Property(x => x.IsActive).HasColumnName("is_active"); b.Property(x => x.CreatedAt).HasColumnName("created_at"); b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
    }
}
public sealed class CustomsTaxConceptConfiguration : IEntityTypeConfiguration<CustomsTaxConcept>
{
    public void Configure(EntityTypeBuilder<CustomsTaxConcept> b)
    {
        b.ToTable("CUSTOMS_TAX_CONCEPT", "cfg"); b.HasKey(x => x.CustomsTaxConceptId);
        b.Property(x => x.CustomsTaxConceptId).HasColumnName("customs_tax_concept_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(40); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120); b.Property(x => x.DisplayOrder).HasColumnName("display_order"); b.Property(x => x.CalculationMode).HasColumnName("calculation_mode").HasMaxLength(40).IsUnicode(false); b.Property(x => x.DefaultRate).HasColumnName("default_rate").HasColumnType("decimal(9,4)"); b.Property(x => x.IsActive).HasColumnName("is_active"); b.Property(x => x.CreatedAt).HasColumnName("created_at"); b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
    }
}
