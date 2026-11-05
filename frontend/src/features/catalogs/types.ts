export type Country = {
  countryCode: string;
  countryName: string;
  iso3Code?: string | null;
};

export type Currency = {
  currencyCode: string;
  currencyName: string;
  symbol?: string | null;
  decimalPlaces: number;
};

export type AppUserCatalog = {
  userId: number;
  username: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
};

export type Port = {
  portId: number;
  countryCode: string;
  portCode?: string | null;
  portName: string;
  cityName?: string | null;
  portType: string;
};

export type TransportMode = {
  transportModeId: number;
  code: string;
  name: string;
};

export type Incoterm = {
  incotermCode: string;
  description: string;
};

export type DocumentType = {
  documentTypeId: number;
  code: string;
  name: string;
  category: string;
  isRequiredDefault: boolean;
};

export type ImportRequestStatusCatalog = {
  code: string;
  name: string;
  isEditable: boolean;
  isTerminal: boolean;
};

export type ExpenseCategory = {
  expenseCategoryId: number;
  code: string;
  name: string;
  isRebillableDefault: boolean;
};

export type ServiceCategory = {
  serviceCategoryId: number;
  code: string;
  name: string;
};

export type QuotationStatusCatalog = {
  code: string;
  name: string;
  isTerminal: boolean;
};

export type QuotationItemTypeCatalog = {
  code: string;
  name: string;
  categorySource?: string | null;
};

export type CodeNameCatalog = { code:string; name:string };
export type IdCodeNameCatalog = { id:number; code:string; name:string };

export type WorkflowStage = {
  workflowStageId: number;
  code: string;
  name: string;
  displayOrder: number;
  isFinal: boolean;
};

export type PaymentMethod = {
  paymentMethodId: number;
  code: string;
  name: string;
};
