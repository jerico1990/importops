using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Persistence;

public sealed class ImportOpsDbContext(DbContextOptions<ImportOpsDbContext> options) : DbContext(options)
{
    public DbSet<CustomsTaxFinanceItem> CustomsTaxFinanceItems => Set<CustomsTaxFinanceItem>();
    public DbSet<CustomsTaxFinancePayment> CustomsTaxFinancePayments => Set<CustomsTaxFinancePayment>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertRecord> Alerts => Set<AlertRecord>();
    public DbSet<AlertUserState> AlertUserStates => Set<AlertUserState>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportRequest> ImportRequests => Set<ImportRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<QuotationStatusHistory> QuotationStatusHistories => Set<QuotationStatusHistory>();
    public DbSet<QuotationSupplier> QuotationSuppliers => Set<QuotationSupplier>();
    public DbSet<QuotationCargoItem> QuotationCargoItems => Set<QuotationCargoItem>();
    public DbSet<QuotationCustomsTax> QuotationCustomsTaxes => Set<QuotationCustomsTax>();
    public DbSet<QuotationConcept> QuotationConcepts => Set<QuotationConcept>();
    public DbSet<CompanyBankAccount> CompanyBankAccounts => Set<CompanyBankAccount>();
    public DbSet<ImportOperation> ImportOperations => Set<ImportOperation>();
    public DbSet<OperationStageHistory> OperationStageHistories => Set<OperationStageHistory>();
    public DbSet<OperationStatusHistory> OperationStatusHistories => Set<OperationStatusHistory>();
    public DbSet<OperationTask> OperationTasks => Set<OperationTask>();
    public DbSet<OperationEvent> OperationEvents => Set<OperationEvent>();
    public DbSet<OperationNote> OperationNotes => Set<OperationNote>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<StageTaskTemplate> StageTaskTemplates => Set<StageTaskTemplate>();
    public DbSet<StageDocumentRequirement> StageDocumentRequirements => Set<StageDocumentRequirement>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentContainer> ShipmentContainers => Set<ShipmentContainer>();
    public DbSet<CustomsProcess> CustomsProcesses => Set<CustomsProcess>();
    public DbSet<CustomsObservation> CustomsObservations => Set<CustomsObservation>();
    public DbSet<CustomsTax> CustomsTaxes => Set<CustomsTax>();
    public DbSet<CustomsStatusHistory> CustomsStatusHistories => Set<CustomsStatusHistory>();
    public DbSet<OperationBudget> OperationBudgets => Set<OperationBudget>();
    public DbSet<OperationBudgetItem> OperationBudgetItems => Set<OperationBudgetItem>();
    public DbSet<ClientFund> ClientFunds => Set<ClientFund>();
    public DbSet<OperationExpense> OperationExpenses => Set<OperationExpense>();
    public DbSet<ExpensePayment> ExpensePayments => Set<ExpensePayment>();
    public DbSet<ExpensePaymentReceipt> ExpensePaymentReceipts => Set<ExpensePaymentReceipt>();
    public DbSet<OperationServiceCharge> OperationServices => Set<OperationServiceCharge>();
    public DbSet<ClientCollection> ClientCollections => Set<ClientCollection>();
    public DbSet<ServiceCollectionAllocation> ServiceCollectionAllocations => Set<ServiceCollectionAllocation>();
    public DbSet<OperationAdjustment> OperationAdjustments => Set<OperationAdjustment>();
    public DbSet<ClientRefund> ClientRefunds => Set<ClientRefund>();
    public DbSet<OperationSettlement> OperationSettlements => Set<OperationSettlement>();
    public DbSet<IssuedLiquidation> IssuedLiquidations => Set<IssuedLiquidation>();
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();
    public DbSet<DocumentObservation> DocumentObservations => Set<DocumentObservation>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierContact> SupplierContacts => Set<SupplierContact>();

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Port> Ports => Set<Port>();
    public DbSet<TransportMode> TransportModes => Set<TransportMode>();
    public DbSet<Incoterm> Incoterms => Set<Incoterm>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
    public DbSet<CustomsRegime> CustomsRegimes => Set<CustomsRegime>();
    public DbSet<CustomsOfficeCatalog> CustomsOfficeCatalogs => Set<CustomsOfficeCatalog>();
    public DbSet<CustomsTaxConcept> CustomsTaxConcepts => Set<CustomsTaxConcept>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomsTaxFinanceItem>(e => {
            e.ToTable("CUSTOMS_TAX_ITEM", "fin"); e.HasKey(x=>x.CustomsTaxItemId);
            e.Property(x=>x.CustomsTaxItemId).HasColumnName("customs_tax_item_id");
            e.Property(x=>x.CompanyId).HasColumnName("company_id"); e.Property(x=>x.OperationId).HasColumnName("operation_id");
            e.Property(x=>x.TaxCode).HasColumnName("tax_code"); e.Property(x=>x.Description).HasColumnName("description");
            e.Property(x=>x.DocumentReference).HasColumnName("document_reference"); e.Property(x=>x.AssessedAt).HasColumnName("assessed_at");
            e.Property(x=>x.AmountPen).HasColumnName("amount_pen").HasPrecision(19,2); e.Property(x=>x.PayerType).HasColumnName("payer_type"); e.Property(x=>x.PaymentDate).HasColumnName("payment_date"); e.Property(x=>x.Notes).HasColumnName("notes");
            e.Property(x=>x.IsVoid).HasColumnName("is_void"); e.Property(x=>x.VoidReason).HasColumnName("void_reason");
            e.Property(x=>x.CreatedAt).HasColumnName("created_at"); e.Property(x=>x.CreatedBy).HasColumnName("created_by");
            e.Property(x=>x.ModifiedAt).HasColumnName("modified_at"); e.Property(x=>x.ModifiedBy).HasColumnName("modified_by");
            e.Property(x=>x.RowVer).HasColumnName("row_ver").IsRowVersion();
        });
        modelBuilder.Entity<CustomsTaxFinancePayment>(e => {
            e.ToTable("CUSTOMS_TAX_PAYMENT", "fin"); e.HasKey(x=>x.CustomsTaxPaymentId);
            e.Property(x=>x.CustomsTaxPaymentId).HasColumnName("customs_tax_payment_id");
            e.Property(x=>x.CompanyId).HasColumnName("company_id"); e.Property(x=>x.OperationId).HasColumnName("operation_id");
            e.Property(x=>x.CustomsTaxItemId).HasColumnName("customs_tax_item_id"); e.Property(x=>x.PaymentDate).HasColumnName("payment_date");
            e.Property(x=>x.AmountPen).HasColumnName("amount_pen").HasPrecision(19,2); e.Property(x=>x.PayerType).HasColumnName("payer_type"); e.Property(x=>x.FundingSource).HasColumnName("funding_source");
            e.Property(x=>x.PaymentReference).HasColumnName("payment_reference"); e.Property(x=>x.Notes).HasColumnName("notes");
            e.Property(x=>x.IsVoid).HasColumnName("is_void"); e.Property(x=>x.VoidReason).HasColumnName("void_reason");
            e.Property(x=>x.CreatedAt).HasColumnName("created_at"); e.Property(x=>x.CreatedBy).HasColumnName("created_by");
            e.Property(x=>x.RowVer).HasColumnName("row_ver").IsRowVersion();
        });
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImportOpsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
