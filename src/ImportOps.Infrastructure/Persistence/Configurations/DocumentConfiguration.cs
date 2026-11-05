using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class DocumentRecordConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
    public void Configure(EntityTypeBuilder<DocumentRecord> b)
    {
        b.ToTable("DOCUMENT", "doc");
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.QuotationId).HasColumnName("quotation_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(260);
        b.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260);
        b.Property(x => x.FileExtension).HasColumnName("file_extension").HasMaxLength(20);
        b.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(150);
        b.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(600);
        b.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
        b.Property(x => x.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(64).IsFixedLength();
        b.Property(x => x.VersionNo).HasColumnName("version_no");
        b.Property(x => x.DocumentStatus).HasColumnName("document_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.DocumentDate).HasColumnName("document_date").HasColumnType("date");
        b.Property(x => x.ExpirationDate).HasColumnName("expiration_date").HasColumnType("date");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        b.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        b.Property(x => x.ReplacedDocumentId).HasColumnName("replaced_document_id");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class DocumentObservationConfiguration : IEntityTypeConfiguration<DocumentObservation>
{
    public void Configure(EntityTypeBuilder<DocumentObservation> b)
    {
        b.ToTable("DOCUMENT_OBSERVATION", "doc");
        b.HasKey(x => x.DocumentObservationId);
        b.Property(x => x.DocumentObservationId).HasColumnName("document_observation_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1500);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.ObservedAt).HasColumnName("observed_at");
        b.Property(x => x.ObservedBy).HasColumnName("observed_by");
        b.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        b.Property(x => x.ResolvedBy).HasColumnName("resolved_by");
        b.Property(x => x.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1500);
        b.Property(x => x.OperationTaskId).HasColumnName("operation_task_id");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
    }
}
