export type CustomsListItem = {
  customsProcessId: number;
  operationId: number;
  operationNo: string;
  shipmentId: number;
  shipmentStatus: string;
  declarationNo?: string | null;
  declarationDate?: string | null;
  customsRegimeId?: number | null;
  customsRegime?: string | null;
  customsRegimeCode?: string | null;
  customsOfficeId?: number | null;
  customsOffice?: string | null;
  customsOfficeCode?: string | null;
  customsChannel?: string | null;
  customsStatus: string;
  dutiesAmount?: number | null;
  dutiesCurrencyCode?: string | null;
  releaseDate?: string | null;
  openObservationCount: number;
};

export type CustomsObservation = {
  customsObservationId: number;
  observationType: string;
  severity: string;
  description: string;
  observationStatus: string;
  observedAt: string;
  observedBy: number;
  resolvedAt?: string | null;
  resolvedBy?: number | null;
  resolutionNote?: string | null;
  operationTaskId?: number | null;
  rowVersion: string;
};

export type CustomsTax = {
  customsTaxId: number;
  customsTaxConceptId?: number | null;
  taxType: string;
  description: string;
  amount: number;
  currencyCode: string;
  exchangeRate?: number | null;
  baseAmount: number;
  paymentStatus: string;
  paymentReference?: string | null;
  paidAt?: string | null;
  notes?: string | null;
  rowVersion: string;
};

export type CustomsStatusHistory = {
  customsStatusHistoryId: number;
  oldStatus?: string | null;
  newStatus: string;
  comment?: string | null;
  changedAt: string;
  changedBy: number;
};

export type CustomsDetail = {
  customsProcessId: number;
  operationId: number;
  operationNo: string;
  shipmentId: number;
  shipmentStatus: string;
  bookingNo?: string | null;
  masterDocumentNo?: string | null;
  houseDocumentNo?: string | null;
  customsRegimeId?: number | null;
  customsRegime?: string | null;
  customsRegimeCode?: string | null;
  declarationNo?: string | null;
  declarationDate?: string | null;
  releaseDate?: string | null;
  customsOfficeId?: number | null;
  customsOffice?: string | null;
  customsOfficeCode?: string | null;
  customsChannel?: string | null;
  customsStatus: string;
  dutiesAmount?: number | null;
  dutiesCurrencyCode?: string | null;
  dutiesExchangeRate?: number | null;
  dutiesBaseAmount?: number | null;
  observation?: string | null;
  createdAt: string;
  modifiedAt?: string | null;
  rowVersion: string;
  observations: CustomsObservation[];
  taxes: CustomsTax[];
  history: CustomsStatusHistory[];
};
