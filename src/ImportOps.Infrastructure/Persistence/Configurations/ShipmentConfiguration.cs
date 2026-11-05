using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> b)
    {
        b.ToTable("SHIPMENT", "ops");
        b.HasKey(x => x.ShipmentId);

        b.Property(x => x.ShipmentId).HasColumnName("shipment_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.TransportModeId).HasColumnName("transport_mode_id");

        b.Property(x => x.CarrierName).HasColumnName("carrier_name").HasMaxLength(180);
        b.Property(x => x.VesselFlightName).HasColumnName("vessel_flight_name").HasMaxLength(180);
        b.Property(x => x.BookingNo).HasColumnName("booking_no").HasMaxLength(100);
        b.Property(x => x.MasterDocumentNo).HasColumnName("master_document_no").HasMaxLength(100);
        b.Property(x => x.HouseDocumentNo).HasColumnName("house_document_no").HasMaxLength(100);

        b.Property(x => x.ContainerNo).HasColumnName("container_no").HasMaxLength(50);
        b.Property(x => x.SealNo).HasColumnName("seal_no").HasMaxLength(50);

        b.Property(x => x.OriginPortId).HasColumnName("origin_port_id");
        b.Property(x => x.DestinationPortId).HasColumnName("destination_port_id");

        b.Property(x => x.Etd).HasColumnName("etd");
        b.Property(x => x.Eta).HasColumnName("eta");
        b.Property(x => x.ActualDepartureAt).HasColumnName("actual_departure_at");
        b.Property(x => x.ActualArrivalAt).HasColumnName("actual_arrival_at");

        b.Property(x => x.ShipmentStatus).HasColumnName("shipment_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Operation)
            .WithMany()
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Containers)
            .WithOne(x => x.Shipment)
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShipmentContainerConfiguration : IEntityTypeConfiguration<ShipmentContainer>
{
    public void Configure(EntityTypeBuilder<ShipmentContainer> b)
    {
        b.ToTable("SHIPMENT_CONTAINER", "ops");
        b.HasKey(x => x.ShipmentContainerId);

        b.Property(x => x.ShipmentContainerId).HasColumnName("shipment_container_id");
        b.Property(x => x.ShipmentId).HasColumnName("shipment_id");
        b.Property(x => x.ContainerNo).HasColumnName("container_no").HasMaxLength(50).IsRequired();
        b.Property(x => x.ContainerType).HasColumnName("container_type").HasMaxLength(30);
        b.Property(x => x.SealNo).HasColumnName("seal_no").HasMaxLength(50);
        b.Property(x => x.PackageCount).HasColumnName("package_count").HasColumnType("decimal(18,3)");
        b.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasColumnType("decimal(18,3)");
        b.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasColumnType("decimal(18,4)");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
    }
}
