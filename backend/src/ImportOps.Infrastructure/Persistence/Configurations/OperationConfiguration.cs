using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportOps.Infrastructure.Persistence.Configurations;

internal sealed class ImportOperationConfiguration : IEntityTypeConfiguration<ImportOperation>
{
    public void Configure(EntityTypeBuilder<ImportOperation> b)
    {
        b.ToTable("IMPORT_OPERATION", "ops");
        b.Property(x => x.FinancingDays).HasColumnName("financing_days");
        b.HasKey(x => x.OperationId);

        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationNo).HasColumnName("operation_no").HasMaxLength(30).IsRequired();
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.QuotationId).HasColumnName("quotation_id");
        b.Property(x => x.ClientId).HasColumnName("client_id");
        b.Property(x => x.ClientContactId).HasColumnName("client_contact_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.ResponsibleUserId).HasColumnName("responsible_user_id");

        b.Property(x => x.ClientNameSnapshot).HasColumnName("client_name_snapshot").HasMaxLength(200);
        b.Property(x => x.ClientContactNameSnapshot).HasColumnName("client_contact_name_snapshot").HasMaxLength(160);
        b.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(200);
        b.Property(x => x.QuotationNoSnapshot).HasColumnName("quotation_no_snapshot").HasMaxLength(30);
        b.Property(x => x.QuotationVersionSnapshot).HasColumnName("quotation_version_snapshot");
        b.Property(x => x.QuotationCurrencySnapshot).HasColumnName("quotation_currency_snapshot").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.QuotationTotalSnapshot).HasColumnName("quotation_total_snapshot").HasColumnType("decimal(19,4)");

        b.Property(x => x.CurrentStageId).HasColumnName("current_stage_id");
        b.Property(x => x.OperationalStatus).HasColumnName("operational_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.DocumentaryStatus).HasColumnName("documentary_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.FinancialStatus).HasColumnName("financial_status").HasMaxLength(30).IsUnicode(false);

        b.Property(x => x.TransportModeId).HasColumnName("transport_mode_id");
        b.Property(x => x.IncotermCode).HasColumnName("incoterm_code").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.DestinationCountryCode).HasColumnName("destination_country_code").HasMaxLength(2).IsFixedLength();
        b.Property(x => x.OriginPortId).HasColumnName("origin_port_id");
        b.Property(x => x.DestinationPortId).HasColumnName("destination_port_id");

        b.Property(x => x.MerchandiseDescription).HasColumnName("merchandise_description").HasMaxLength(1500).IsRequired();
        b.Property(x => x.PackageCount).HasColumnName("package_count").HasColumnType("decimal(18,3)");
        b.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasColumnType("decimal(18,3)");
        b.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").HasColumnType("decimal(18,3)");
        b.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasColumnType("decimal(18,4)");
        b.Property(x => x.DeclaredValue).HasColumnName("declared_value").HasColumnType("decimal(19,4)");
        b.Property(x => x.DeclaredCurrencyCode).HasColumnName("declared_currency_code").HasMaxLength(3).IsFixedLength();

        b.Property(x => x.OpenedAt).HasColumnName("opened_at");
        b.Property(x => x.EstimatedCloseDate).HasColumnName("estimated_close_date").HasColumnType("date");
        b.Property(x => x.ActualCloseDate).HasColumnName("actual_close_date").HasColumnType("date");
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.Property(x => x.ClosedByUserId).HasColumnName("closed_by_user_id");
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ClientContact).WithMany().HasForeignKey(x => x.ClientContactId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResponsibleUser).WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentStage).WithMany().HasForeignKey(x => x.CurrentStageId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OperationStageHistoryConfiguration : IEntityTypeConfiguration<OperationStageHistory>
{
    public void Configure(EntityTypeBuilder<OperationStageHistory> b)
    {
        b.ToTable("OPERATION_STAGE_HISTORY", "ops");
        b.HasKey(x => x.OperationStageHistoryId);
        b.Property(x => x.OperationStageHistoryId).HasColumnName("operation_stage_history_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.FromStageId).HasColumnName("from_stage_id");
        b.Property(x => x.ToStageId).HasColumnName("to_stage_id");
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        b.Property(x => x.ChangedAt).HasColumnName("changed_at");
        b.Property(x => x.ChangedBy).HasColumnName("changed_by");
    }
}

internal sealed class OperationStatusHistoryConfiguration : IEntityTypeConfiguration<OperationStatusHistory>
{
    public void Configure(EntityTypeBuilder<OperationStatusHistory> b)
    {
        b.ToTable("OPERATION_STATUS_HISTORY", "ops");
        b.HasKey(x => x.OperationStatusHistoryId);
        b.Property(x => x.OperationStatusHistoryId).HasColumnName("operation_status_history_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.StatusDimension).HasColumnName("status_dimension").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        b.Property(x => x.ChangedAt).HasColumnName("changed_at");
        b.Property(x => x.ChangedBy).HasColumnName("changed_by");
    }
}

internal sealed class OperationTaskConfiguration : IEntityTypeConfiguration<OperationTask>
{
    public void Configure(EntityTypeBuilder<OperationTask> b)
    {
        b.ToTable("OPERATION_TASK", "ops");
        b.HasKey(x => x.OperationTaskId);
        b.Property(x => x.OperationTaskId).HasColumnName("operation_task_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.WorkflowStageId).HasColumnName("workflow_stage_id");
        b.Property(x => x.StageTaskTemplateId).HasColumnName("stage_task_template_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(180);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        b.Property(x => x.TaskStatus).HasColumnName("task_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.AssignedUserId).HasColumnName("assigned_user_id");
        b.Property(x => x.DueAt).HasColumnName("due_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");
        b.Property(x => x.CompletedByUserId).HasColumnName("completed_by_user_id");
        b.Property(x => x.IsMandatory).HasColumnName("is_mandatory");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        b.Property(x => x.ModifiedBy).HasColumnName("modified_by");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.RowVer).HasColumnName("row_ver").IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class OperationEventConfiguration : IEntityTypeConfiguration<OperationEvent>
{
    public void Configure(EntityTypeBuilder<OperationEvent> b)
    {
        b.ToTable("OPERATION_EVENT", "ops");
        b.HasKey(x => x.OperationEventId);
        b.Property(x => x.OperationEventId).HasColumnName("operation_event_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.EventDate).HasColumnName("event_date");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(180);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        b.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
    }
}

internal sealed class OperationNoteConfiguration : IEntityTypeConfiguration<OperationNote>
{
    public void Configure(EntityTypeBuilder<OperationNote> b)
    {
        b.ToTable("OPERATION_NOTE", "ops");
        b.HasKey(x => x.OperationNoteId);
        b.Property(x => x.OperationNoteId).HasColumnName("operation_note_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.NoteType).HasColumnName("note_type").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.NoteText).HasColumnName("note_text").HasMaxLength(2000);
        b.Property(x => x.IsPinned).HasColumnName("is_pinned");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
    }
}

internal sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> b)
    {
        b.ToTable("WORKFLOW_TRANSITION", "cfg");
        b.HasKey(x => x.WorkflowTransitionId);
        b.Property(x => x.WorkflowTransitionId).HasColumnName("workflow_transition_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.FromStageId).HasColumnName("from_stage_id");
        b.Property(x => x.ToStageId).HasColumnName("to_stage_id");
        b.Property(x => x.RequiresComment).HasColumnName("requires_comment");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class StageTaskTemplateConfiguration : IEntityTypeConfiguration<StageTaskTemplate>
{
    public void Configure(EntityTypeBuilder<StageTaskTemplate> b)
    {
        b.ToTable("STAGE_TASK_TEMPLATE", "cfg");
        b.HasKey(x => x.StageTaskTemplateId);
        b.Property(x => x.StageTaskTemplateId).HasColumnName("stage_task_template_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.WorkflowStageId).HasColumnName("workflow_stage_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(180);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.IsMandatory).HasColumnName("is_mandatory");
        b.Property(x => x.DueDaysOffset).HasColumnName("due_days_offset");
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

internal sealed class StageDocumentRequirementConfiguration : IEntityTypeConfiguration<StageDocumentRequirement>
{
    public void Configure(EntityTypeBuilder<StageDocumentRequirement> b)
    {
        b.ToTable("STAGE_DOCUMENT_REQUIREMENT", "cfg");
        b.HasKey(x => x.StageDocumentRequirementId);
        b.Property(x => x.StageDocumentRequirementId).HasColumnName("stage_document_requirement_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.WorkflowStageId).HasColumnName("workflow_stage_id");
        b.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
        b.Property(x => x.IsMandatory).HasColumnName("is_mandatory");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}


internal sealed class DocumentRecordCoreReadConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
    public void Configure(EntityTypeBuilder<DocumentRecord> b)
    {
        b.ToTable("DOCUMENT", "doc");
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.DocumentTypeId).HasColumnName("document_type_id");
        b.Property(x => x.DocumentStatus).HasColumnName("document_status").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
    }
}
