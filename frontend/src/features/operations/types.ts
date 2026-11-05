export type OperationListItem = {
  operationId: number;
  operationNo: string;
  clientId: number;
  clientName: string;
  stageCode: string;
  stageName: string;
  operationalStatus: string;
  documentaryStatus: string;
  financialStatus?: string | null;
  responsibleUserId: number;
  responsibleName: string;
  openedAt: string;
  estimatedCloseDate?: string | null;
};

export type OperationQuery = {
  search?: string;
  operationalStatus?: string;
  documentaryStatus?: string;
  financialStatus?: string;
  stageId?: number;
  clientId?: number;
  quotationId?: number;
  responsibleUserId?: number;
  openedFrom?: string;
  openedTo?: string;
  page?: number;
  pageSize?: number;
};

export type OperationSourceSnapshot = {
  clientName?: string | null;
  clientContactName?: string | null;
  supplierName?: string | null;
  quotationNo?: string | null;
  quotationVersion?: number | null;
  quotationCurrency?: string | null;
  quotationTotal?: number | null;
};

export type OperationDetail = {
  operationId: number;
  operationNo: string;
  requestId?: number | null;
  requestNo?: string | null;
  quotationId?: number | null;
  quotationNo?: string | null;
  quotationVersion?: number | null;
  clientId: number;
  clientName: string;
  clientContactId?: number | null;
  clientContactName?: string | null;
  supplierId?: number | null;
  supplierName?: string | null;
  responsibleUserId: number;
  responsibleName: string;
  currentStageId: number;
  currentStageCode: string;
  currentStageName: string;
  operationalStatus: string;
  documentaryStatus: string;
  financialStatus?: string | null;
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
  netWeightKg?: number | null;
  volumeM3?: number | null;
  declaredValue?: number | null;
  declaredCurrencyCode?: string | null;
  quotationCurrencySnapshot?: string | null;
  quotationTotalSnapshot?: number | null;
  openedAt: string;
  estimatedCloseDate?: string | null;
  actualCloseDate?: string | null;
  closedAt?: string | null;
  cancellationReason?: string | null;
  notes?: string | null;
  createdAt: string;
  modifiedAt?: string | null;
  rowVersion: string;
  sourceSnapshot: OperationSourceSnapshot;
};

export type OperationAvailableStage = {
  workflowStageId: number;
  code: string;
  name: string;
  displayOrder: number;
  requiresComment: boolean;
};

export type OperationStageHistory = {
  operationStageHistoryId: number;
  fromStageId?: number | null;
  fromStageName?: string | null;
  toStageId: number;
  toStageName: string;
  comment?: string | null;
  changedAt: string;
  changedBy: number;
  changedByName: string;
};

export type OperationStatusHistory = {
  operationStatusHistoryId: number;
  statusDimension: string;
  oldStatus?: string | null;
  newStatus: string;
  comment?: string | null;
  changedAt: string;
  changedBy: number;
  changedByName: string;
};

export type OperationTask = {
  operationTaskId: number;
  workflowStageId?: number | null;
  stageName?: string | null;
  title: string;
  description?: string | null;
  taskStatus: string;
  priority: string;
  assignedUserId?: number | null;
  assignedUserName?: string | null;
  dueAt?: string | null;
  completedAt?: string | null;
  completedByUserId?: number | null;
  isMandatory: boolean;
  isFromTemplate: boolean;
  allowedNextStatuses: string[];
};

export type OperationEvent = {
  operationEventId: number;
  eventType: string;
  eventDate: string;
  title: string;
  description?: string | null;
  sourceType: string;
  visibility: string;
  createdBy: number;
  createdByName: string;
};

export type OperationNote = {
  operationNoteId: number;
  noteType: string;
  noteText: string;
  isPinned: boolean;
  createdAt: string;
  createdBy: number;
  createdByName: string;
};
