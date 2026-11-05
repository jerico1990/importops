export type UserType = "INTERNAL" | "CLIENT";

export type SessionUser = {
  userId: number;
  companyId: number;
  username: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  userType: UserType;
  clientId?: number | null;
  clientName?: string | null;
  mustChangePassword: boolean;
  roles: string[];
  permissions: string[];
};

export const Permissions = {
  ClientsView: "CLIENTS.VIEW",
  ClientsManage: "CLIENTS.MANAGE",

  SuppliersView: "SUPPLIERS.VIEW",
  SuppliersManage: "SUPPLIERS.MANAGE",

  RequestsView: "REQUESTS.VIEW",
  RequestsCreate: "REQUESTS.CREATE",
  RequestsManage: "REQUESTS.MANAGE",

  QuotationsView: "QUOTATIONS.VIEW",
  QuotationsManage: "QUOTATIONS.MANAGE",
  QuotationsApprove: "QUOTATIONS.APPROVE",

  OperationsView: "OPERATIONS.VIEW",
  OperationsManage: "OPERATIONS.MANAGE",
  OperationsClose: "OPERATIONS.CLOSE",

  ShipmentsView: "SHIPMENTS.VIEW",
  ShipmentsManage: "SHIPMENTS.MANAGE",

  CustomsView: "CUSTOMS.VIEW",
  CustomsManage: "CUSTOMS.MANAGE",
  CustomsRelease: "CUSTOMS.RELEASE",

  DocumentsView: "DOCUMENTS.VIEW",
  DocumentsUpload: "DOCUMENTS.UPLOAD",
  DocumentsManage: "DOCUMENTS.MANAGE",

  FinanceView: "FINANCE.VIEW",
  FinanceManage: "FINANCE.MANAGE",
  FinanceApprove: "FINANCE.APPROVE",

  AlertsView: "ALERTS.VIEW",
  AlertsManage: "ALERTS.MANAGE",

  ConfigurationView: "CONFIGURATION.VIEW",
  ConfigurationManage: "CONFIGURATION.MANAGE",

  SecurityManage: "SECURITY.MANAGE"
} as const;
