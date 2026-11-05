export type ImportRequestStatus =
  | "NEW"
  | "UNDER_REVIEW"
  | "OBSERVED"
  | "QUOTED"
  | "APPROVED"
  | "REJECTED"
  | "CANCELLED";

export type ImportRequestListItem = {
  requestId: number;
  requestNo: string;
  requestDate: string;
  status: ImportRequestStatus;
  clientId: number;
  clientName: string;
  supplierId?: number | null;
  supplierName?: string | null;
  requestedByUserId: number;
  requestedBy: string;
  assignedUserId?: number | null;
  assignedTo?: string | null;
  transportModeId?: number | null;
  transportMode?: string | null;
  merchandiseDescription: string;
  createdAt: string;
};

export type RequestStatusHistory = {
  requestStatusHistoryId: number;
  oldStatus?: string | null;
  newStatus: ImportRequestStatus;
  comment?: string | null;
  changedAt: string;
  changedBy: number;
  changedByName: string;
};

export type ImportRequestDetail = {
  requestId: number;
  requestNo: string;
  requestDate: string;
  status: ImportRequestStatus;
  clientId: number;
  clientName: string;
  clientContactId?: number | null;
  clientContactName?: string | null;
  supplierId?: number | null;
  supplierName?: string | null;
  requestedByUserId: number;
  requestedBy: string;
  assignedUserId?: number | null;
  assignedTo?: string | null;
  transportModeId?: number | null;
  transportMode?: string | null;
  incotermCode?: string | null;
  originCountryCode?: string | null;
  originCountry?: string | null;
  originPortId?: number | null;
  originPort?: string | null;
  destinationCountryCode?: string | null;
  destinationCountry?: string | null;
  destinationPortId?: number | null;
  destinationPort?: string | null;
  merchandiseDescription: string;
  packageCount?: number | null;
  grossWeightKg?: number | null;
  volumeM3?: number | null;
  declaredValue?: number | null;
  declaredCurrencyCode?: string | null;
  desiredServiceDate?: string | null;
  customerNotes?: string | null;
  internalNotes?: string | null;
  rejectionReason?: string | null;
  approvedAt?: string | null;
  approvedByUserId?: number | null;
  createdAt: string;
  modifiedAt?: string | null;
  rowVersion: string;
  history: RequestStatusHistory[];
};

export type CreateImportRequest = {
  clientId: number;
  clientContactId?: number | null;
  supplierId?: number | null;
  assignedUserId?: number | null;
  requestDate?: string | null;
  transportModeId?: number | null;
  incotermCode?: string | null;
  originCountryCode?: string | null;
  destinationCountryCode?: string | null;
  originPortId?: number | null;
  destinationPortId?: number | null;
  merchandiseDescription: string;
  packageCount?: number | null;
  grossWeightKg?: number | null;
  volumeM3?: number | null;
  declaredValue?: number | null;
  declaredCurrencyCode?: string | null;
  desiredServiceDate?: string | null;
  customerNotes?: string | null;
  internalNotes?: string | null;
  rowVersion: string;
};

export type UpdateImportRequest = CreateImportRequest;

export type QuotationSummary = {
  quotationId: number;
  quotationNo: string;
  versionNo: number;
  status: string;
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

export type RequestDocument = {
  documentId: number;
  requestId?: number | null;
  quotationId?: number | null;
  operationId?: number | null;
  documentTypeId: number;
  documentTypeCode: string;
  documentTypeName: string;
  originalFileName: string;
  mimeType?: string | null;
  fileSizeBytes?: number | null;
  versionNo: number;
  documentStatus: string;
  visibility: string;
  documentDate?: string | null;
  expirationDate?: string | null;
  notes?: string | null;
  uploadedAt: string;
  uploadedBy: number;
  replacedDocumentId?: number | null;
  rowVersion: string;
};
