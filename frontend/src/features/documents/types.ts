export type DocumentRecord = {
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
  sha256Hash?: string | null;
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

export type DocumentObservation = {
  documentObservationId: number;
  documentId: number;
  description: string;
  status: string;
  observedAt: string;
  observedBy: number;
  resolvedAt?: string | null;
  resolvedBy?: number | null;
  resolutionNote?: string | null;
  operationTaskId?: number | null;
  rowVersion: string;
};

export type StageDocumentRequirement = {
  workflowStageId: number;
  stageCode: string;
  stageName: string;
  documentTypeId: number;
  documentTypeCode: string;
  documentTypeName: string;
  isMandatory: boolean;
  isSatisfied: boolean;
  validDocumentId?: number | null;
};

export type OperationDocumentStatus = {
  operationId: number;
  documentaryStatus: string;
  requiredCount: number;
  validCount: number;
  missingCount: number;
  observedCount: number;
  requirements: StageDocumentRequirement[];
};
