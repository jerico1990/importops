export type ClientType = "COMPANY" | "PERSON";
export type AddressType = "LEGAL" | "BILLING" | "DELIVERY" | "OTHER";

export type ClientListItem = {
  clientId: number;
  clientType: ClientType;
  legalName: string;
  tradeName?: string | null;
  taxId?: string | null;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
};

export type ClientContact = {
  clientContactId: number;
  fullName: string;
  positionName?: string | null;
  email?: string | null;
  phone?: string | null;
  mobile?: string | null;
  isPrimary: boolean;
  isActive: boolean;
};

export type ClientAddress = {
  clientAddressId: number;
  addressType: AddressType;
  addressLine: string;
  district?: string | null;
  province?: string | null;
  department?: string | null;
  countryCode: string;
  isPrimary: boolean;
  isActive: boolean;
};

export type ClientDetail = {
  clientId: number;
  clientType: ClientType;
  legalName: string;
  tradeName?: string | null;
  taxId?: string | null;
  countryCode: string;
  email?: string | null;
  phone?: string | null;
  creditDays?: number | null;
  preferredCurrencyCode?: string | null;
  notes?: string | null;
  accountManagerUserId?: number | null;
  isActive: boolean;
  rowVersion: string;
  contacts: ClientContact[];
  addresses: ClientAddress[];
};

export type CreateClientRequest = {
  clientType: ClientType;
  legalName: string;
  tradeName?: string | null;
  taxId?: string | null;
  countryCode: string;
  email?: string | null;
  phone?: string | null;
  creditDays?: number | null;
  preferredCurrencyCode?: string | null;
  notes?: string | null;
  accountManagerUserId?: number | null;
};

export type UpdateClientRequest = CreateClientRequest & {
  isActive: boolean;
  rowVersion: string;
};

export type CreateClientContactRequest = {
  fullName: string;
  positionName?: string | null;
  email?: string | null;
  phone?: string | null;
  mobile?: string | null;
  isPrimary: boolean;
};

export type UpdateClientContactRequest = CreateClientContactRequest & {
  isActive: boolean;
};

export type CreateClientAddressRequest = {
  addressType: AddressType;
  addressLine: string;
  district?: string | null;
  province?: string | null;
  department?: string | null;
  countryCode: string;
  isPrimary: boolean;
};

export type UpdateClientAddressRequest = CreateClientAddressRequest & {
  isActive: boolean;
};

export type ImportRequestSummary = {
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

export type OperationSummary = {
  operationId: number;
  operationNo: string;
  clientId: number;
  clientName: string;
  stageCode: string;
  stageName: string;
  operationalStatus: string;
  documentaryStatus: string;
  financialStatus: string;
  responsibleUserId: number;
  responsibleName: string;
  openedAt: string;
  estimatedCloseDate?: string | null;
};
