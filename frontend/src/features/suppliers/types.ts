export type SupplierListItem = {
  supplierId: number;
  legalName: string;
  taxId?: string | null;
  countryCode?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
};

export type SupplierContact = {
  supplierContactId: number;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  positionName?: string | null;
  isPrimary: boolean;
  isActive: boolean;
};

export type SupplierDetail = {
  supplierId: number;
  legalName: string;
  taxId?: string | null;
  countryCode?: string | null;
  email?: string | null;
  phone?: string | null;
  addressLine?: string | null;
  notes?: string | null;
  isActive: boolean;
  rowVersion: string;
  contacts: SupplierContact[];
};

export type CreateSupplierRequest = {
  legalName: string;
  taxId?: string | null;
  countryCode?: string | null;
  email?: string | null;
  phone?: string | null;
  addressLine?: string | null;
  notes?: string | null;
};

export type UpdateSupplierRequest = CreateSupplierRequest & {
  isActive: boolean;
  rowVersion: string;
};

export type UpsertSupplierContactRequest = {
  fullName: string;
  email?: string | null;
  phone?: string | null;
  positionName?: string | null;
  isPrimary: boolean;
  isActive: boolean;
};

export type SupplierRequestSummary = {
  requestId: number;
  requestNo: string;
  requestDate: string;
  status: string;
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

export type SupplierOperationSummary = {
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
