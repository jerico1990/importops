import { api } from "@/lib/api/http";
import type {
  AppUserCatalog,
  Country,
  Currency,
  DocumentType,
  ImportRequestStatusCatalog,
  Incoterm,
  Port,
  TransportMode,
  ExpenseCategory,
  ServiceCategory,
  QuotationStatusCatalog,
  QuotationItemTypeCatalog,
  CodeNameCatalog,
  IdCodeNameCatalog,
  WorkflowStage,
  PaymentMethod
} from "./types";

export const catalogsApi = {
  countries: () => api.get<Country[]>("/api/catalogs/countries"),
  currencies: () => api.get<Currency[]>("/api/catalogs/currencies"),
  users: () => api.get<AppUserCatalog[]>("/api/catalogs/users"),
  accountManagers: () =>
    api.get<AppUserCatalog[]>("/api/catalogs/users?purpose=ACCOUNT_MANAGER"),
  operationResponsibles: () =>
    api.get<AppUserCatalog[]>("/api/catalogs/users?purpose=OPERATION_RESPONSIBLE"),
  requestAssignees: () =>
    api.get<AppUserCatalog[]>("/api/catalogs/users?purpose=REQUEST_ASSIGNEE"),
  transportModes: () =>
    api.get<TransportMode[]>("/api/catalogs/transport-modes"),
  incoterms: () =>
    api.get<Incoterm[]>("/api/catalogs/incoterms"),
  ports: (countryCode?: string, portType?: string) => {
    const params = new URLSearchParams();
    if (countryCode) params.set("countryCode", countryCode);
    if (portType) params.set("portType", portType);
    const query = params.toString();
    return api.get<Port[]>(`/api/catalogs/ports${query ? `?${query}` : ""}`);
  },
  documentTypes: () =>
    api.get<DocumentType[]>("/api/catalogs/document-types"),
  documentCategories: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/document-categories"),
  paymentMethods: () =>
    api.get<PaymentMethod[]>("/api/catalogs/payment-methods"),
  importRequestStatuses: () =>
    api.get<ImportRequestStatusCatalog[]>("/api/catalogs/import-request-statuses"),
  expenseCategories: () =>
    api.get<ExpenseCategory[]>("/api/catalogs/expense-categories"),
  serviceCategories: () =>
    api.get<ServiceCategory[]>("/api/catalogs/service-categories"),
  quotationStatuses: () =>
    api.get<QuotationStatusCatalog[]>("/api/catalogs/quotation-statuses"),
  quotationItemTypes: () =>
    api.get<QuotationItemTypeCatalog[]>("/api/catalogs/quotation-item-types"),
  operationStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/operation-statuses"),
  documentaryStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/documentary-statuses"),
  financialStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/financial-statuses"),
  operationTaskStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/operation-task-statuses"),
  taskPriorities: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/task-priorities"),
  operationNoteTypes: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/operation-note-types"),
  operationEventTypes: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/operation-event-types"),
  recordVisibilities: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/record-visibilities"),
  workflowStages: () =>
    api.get<WorkflowStage[]>("/api/catalogs/workflow-stages"),
  shipmentStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/shipment-statuses"),
  shipmentContainerTypes: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/shipment-container-types"),
  documentStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/document-statuses"),
  documentObservationStatuses: () =>
    api.get<CodeNameCatalog[]>("/api/catalogs/document-observation-statuses"),
  customsStatuses: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-statuses"),
  customsChannels: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-channels"),
  customsObservationTypes: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-observation-types"),
  customsObservationSeverities: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-observation-severities"),
  customsRegimes: () => api.get<IdCodeNameCatalog[]>("/api/catalogs/customs-regimes"),
  customsOffices: () => api.get<IdCodeNameCatalog[]>("/api/catalogs/customs-offices"),
  customsTaxConcepts: () => api.get<IdCodeNameCatalog[]>("/api/catalogs/customs-tax-concepts"),
  customsTaxTypes: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-tax-types"),
  customsTaxPaymentStatuses: () => api.get<CodeNameCatalog[]>("/api/catalogs/customs-tax-payment-statuses"),
  financeCollectionTypes: () => api.get<CodeNameCatalog[]>("/api/catalogs/finance-collection-types"),
  financeAdjustmentTypes: () => api.get<CodeNameCatalog[]>("/api/catalogs/finance-adjustment-types"),
  financePayerTypes: () => api.get<CodeNameCatalog[]>("/api/catalogs/finance-payer-types")
};
