import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  CreateSupplierRequest,
  SupplierContact,
  SupplierDetail,
  SupplierListItem,
  SupplierOperationSummary,
  SupplierRequestSummary,
  UpdateSupplierRequest,
  UpsertSupplierContactRequest
} from "./types";

function queryString(values: Record<string, string | number | boolean | undefined | null>) {
  const params = new URLSearchParams();

  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") {
      params.set(key, String(value));
    }
  });

  const text = params.toString();
  return text ? `?${text}` : "";
}

export const suppliersApi = {
  list: (query: {
    search?: string;
    isActive?: boolean;
    page: number;
    pageSize: number;
  }) =>
    api.get<PagedResult<SupplierListItem>>(
      `/api/suppliers${queryString(query)}`
    ),

  get: (supplierId: number) =>
    api.get<SupplierDetail>(`/api/suppliers/${supplierId}`),

  create: (request: CreateSupplierRequest) =>
    api.post<SupplierDetail>("/api/suppliers", request),

  update: (supplierId: number, request: UpdateSupplierRequest) =>
    api.put<SupplierDetail>(`/api/suppliers/${supplierId}`, request),

  addContact: (supplierId: number, request: UpsertSupplierContactRequest) =>
    api.post<SupplierContact>(
      `/api/suppliers/${supplierId}/contacts`,
      request
    ),

  updateContact: (
    supplierId: number,
    contactId: number,
    request: UpsertSupplierContactRequest
  ) =>
    api.put<SupplierContact>(
      `/api/suppliers/${supplierId}/contacts/${contactId}`,
      request
    ),

  deactivateContact: (supplierId: number, contactId: number) =>
    api.delete<void>(
      `/api/suppliers/${supplierId}/contacts/${contactId}`
    ),

  requests: (supplierId: number, page = 1, pageSize = 10) =>
    api.get<PagedResult<SupplierRequestSummary>>(
      `/api/import-requests${queryString({ supplierId, page, pageSize })}`
    ),

  operations: (supplierId: number, page = 1, pageSize = 10) =>
    api.get<PagedResult<SupplierOperationSummary>>(
      `/api/operations${queryString({ supplierId, page, pageSize })}`
    )
};
