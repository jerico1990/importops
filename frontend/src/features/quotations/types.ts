export type QuotationStatus =
  | "DRAFT"
  | "SENT"
  | "APPROVED"
  | "REJECTED"
  | "EXPIRED"
  | "CANCELLED";

export type QuotationListItem = {
  quotationId: number;
  quotationNo: string;
  versionNo: number;
  status: QuotationStatus;
  requestId: number;
  requestNo: string;
  clientId: number;
  clientName: string;
  currencyCode: string;
  issueDate: string;
  validUntil?: string | null;
  totalAmount: number;
  createdAt: string;
};

export type QuotationItem = {
  quotationItemId: number;
  lineNo: number;
  itemType: string;
  description: string;
  quantity: number;
  unitPrice: number;
  grossAmount: number;
  discountAmount: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  expenseCategoryId?: number | null;
  expenseCategoryName?: string | null;
  serviceCategoryId?: number | null;
  serviceCategoryName?: string | null;
  isRebillable: boolean;
  notes?: string | null;
  quotationConceptId?: number | null;
  isTaxable?: boolean | null;
  displayOrder?: number | null;
};

export type QuotationHistory = {
  quotationStatusHistoryId: number;
  oldStatus?: string | null;
  newStatus: QuotationStatus;
  comment?: string | null;
  changedAt: string;
  changedBy: number;
  changedByName: string;
};

export type QuotationDetail = {
  quotationId: number;
  quotationNo: string;
  versionNo: number;
  status: QuotationStatus;
  requestId: number;
  requestNo: string;
  clientId: number;
  clientName: string;
  currencyCode: string;
  issueDate: string;
  validUntil?: string | null;
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  totalAmount: number;
  terms?: string | null;
  internalNotes?: string | null;
  sentAt?: string | null;
  approvedAt?: string | null;
  approvedByName?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  createdAt: string;
  modifiedAt?: string | null;
  rowVersion: string;
  items: QuotationItem[];
  history: QuotationHistory[];
};

export type CreateQuotation = {
  currencyCode: string;
  issueDate?: string | null;
  validUntil?: string | null;
  terms?: string | null;
  internalNotes?: string | null;
  items: CreateQuotationItem[];
};

export type UpdateQuotation = {
  rowVersion: string;
  currencyCode: string;
  issueDate?: string | null;
  validUntil?: string | null;
  terms?: string | null;
  internalNotes?: string | null;
};

export type CreateQuotationItem = {
  itemType: string;
  description: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  taxRate: number;
  expenseCategoryId?: number | null;
  serviceCategoryId?: number | null;
  isRebillable: boolean;
  notes?: string | null;
  quotationConceptId?: number | null;
};

export type NewQuotationVersion = {
  validUntil?: string | null;
  comment?: string | null;
};

export type QuotationSupplier = { quotationSupplierId:number; supplierId:number; supplierName:string; displayOrder:number; notes?:string|null };
export type QuotationCargoItem = { quotationCargoItemId:number; quotationSupplierId?:number|null; lineNo:number; description:string; quantity?:number|null; unitOfMeasure?:string|null; netWeightKg?:number|null; grossWeightKg?:number|null; volumeM3?:number|null; declaredValue?:number|null; currencyCode?:string|null; originCountryCode?:string|null; notes?:string|null };
export type QuotationCustomsTax = { quotationCustomsTaxId:number; customsTaxConceptId?:number|null; taxCode:string; description:string; amount:number; currencyCode:string; displayOrder:number; notes?:string|null; rateApplied?:number|null; calculationBase?:number|null; isCalculated:boolean; calculationMode:string };
export type QuotationConcept = { quotationConceptId:number; code:string; name:string; groupCode:string; groupName:string; serviceCategoryId?:number|null; expenseCategoryId?:number|null; defaultTaxRate:number; isTaxable:boolean; isRebillableDefault:boolean; displayOrder:number };
export type QuotationSelectableCatalog = { id:number; code:string; name:string; displayOrder:number };
export type CompanyBankAccount = { companyBankAccountId:number; bankName:string; currencyCode:string; accountNumber:string; cci?:string|null; accountHolder?:string|null; displayOrder:number };
export type QuotationProforma = { referenceFile?:string|null; contentDescription?:string|null; customsRegimeId?:number|null; customsRegimeCode?:string|null; customsRegimeName?:string|null; customsOfficeId?:number|null; customsOfficeCode?:string|null; customsOfficeName?:string|null; estimatedArrivalDate?:string|null; exchangeRate?:number|null; exwAmount:number; freightAmount:number; insuranceAmount:number; customsValue:number; netWeightKg?:number|null; packageCount?:number|null; volumeM3?:number|null; customsDutiesAmount:number; expenseAmount:number; expenseTaxAmount:number; totalProforma:number; bankInstructions?:string|null; publicNotes?:string|null; suppliers:QuotationSupplier[]; cargoItems:QuotationCargoItem[]; customsTaxes:QuotationCustomsTax[]; bankAccounts:CompanyBankAccount[] };
export type UpdateQuotationProforma = { rowVersion:string; referenceFile?:string|null; contentDescription?:string|null; customsRegimeId?:number|null; customsOfficeId?:number|null; estimatedArrivalDate?:string|null; exchangeRate?:number|null; exwAmount:number; freightAmount:number; insuranceAmount:number; netWeightKg?:number|null; packageCount?:number|null; volumeM3?:number|null; bankInstructions?:string|null; publicNotes?:string|null };
