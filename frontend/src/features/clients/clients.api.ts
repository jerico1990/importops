import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  ClientAddress,
  ClientContact,
  ClientDetail,
  ClientListItem,
  CreateClientAddressRequest,
  CreateClientContactRequest,
  CreateClientRequest,
  ImportRequestSummary,
  OperationSummary,
  UpdateClientAddressRequest,
  UpdateClientContactRequest,
  UpdateClientRequest
} from "./types";

function queryString(values: Record<string, string | number | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") {
      params.set(key, String(value));
    }
  });
  const text = params.toString();
  return text ? `?${text}` : "";
}

export const clientsApi = {
  list: (query: { search?: string; page: number; pageSize: number }) =>
    api.get<PagedResult<ClientListItem>>(
      `/api/clients${queryString(query)}`
    ),

  get: (clientId: number) =>
    api.get<ClientDetail>(`/api/clients/${clientId}`),

  create: (request: CreateClientRequest) =>
    api.post<ClientDetail>("/api/clients", request),

  update: (clientId: number, request: UpdateClientRequest) =>
    api.put<ClientDetail>(`/api/clients/${clientId}`, request),

  addContact: (clientId: number, request: CreateClientContactRequest) =>
    api.post<ClientContact>(`/api/clients/${clientId}/contacts`, request),

  updateContact: (
    clientId: number,
    contactId: number,
    request: UpdateClientContactRequest
  ) =>
    api.put<ClientContact>(
      `/api/clients/${clientId}/contacts/${contactId}`,
      request
    ),

  deactivateContact: (clientId: number, contactId: number) =>
    api.delete<void>(`/api/clients/${clientId}/contacts/${contactId}`),

  addAddress: (clientId: number, request: CreateClientAddressRequest) =>
    api.post<ClientAddress>(`/api/clients/${clientId}/addresses`, request),

  updateAddress: (
    clientId: number,
    addressId: number,
    request: UpdateClientAddressRequest
  ) =>
    api.put<ClientAddress>(
      `/api/clients/${clientId}/addresses/${addressId}`,
      request
    ),

  deactivateAddress: (clientId: number, addressId: number) =>
    api.delete<void>(`/api/clients/${clientId}/addresses/${addressId}`),

  requests: (clientId: number, page = 1, pageSize = 10) =>
    api.get<PagedResult<ImportRequestSummary>>(
      `/api/import-requests${queryString({ clientId, page, pageSize })}`
    ),

  operations: (clientId: number, page = 1, pageSize = 10) =>
    api.get<PagedResult<OperationSummary>>(
      `/api/operations${queryString({ clientId, page, pageSize })}`
    )
};
