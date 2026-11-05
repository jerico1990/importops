import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type { CustomsDetail, CustomsListItem, CustomsObservation, CustomsTax } from "./types";

function qs(values: Record<string, string | number | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([k, v]) => {
    if (v !== undefined && v !== null && String(v).trim() !== "") params.set(k, String(v));
  });
  const q = params.toString();
  return q ? `?${q}` : "";
}

export const customsApi = {
  list: (query: { operationId?: number; shipmentId?: number; status?: string; channel?: string; search?: string; page?: number; pageSize?: number } = {}) =>
    api.get<PagedResult<CustomsListItem>>(`/api/customs${qs(query)}`),
  byOperation: (operationId: number) => api.get<CustomsListItem[]>(`/api/customs/by-operation/${operationId}`),
  get: (id: number) => api.get<CustomsDetail>(`/api/customs/${id}`),
  create: (operationId: number, request: {
    shipmentId?: number | null; customsRegimeId?: number | null; declarationNo?: string | null;
    declarationDate?: string | null; customsOfficeId?: number | null; dutiesCurrencyCode?: string | null;
    dutiesExchangeRate?: number | null; observation?: string | null;
  }) => api.post<CustomsDetail>(`/api/customs/from-operation/${operationId}`, request),
  update: (id: number, request: {
    rowVersion: string; customsRegimeId?: number | null; declarationNo?: string | null; declarationDate?: string | null;
    customsOffice?: string | null; dutiesCurrencyCode?: string | null; dutiesExchangeRate?: number | null; observation?: string | null;
  }) => api.put<CustomsDetail>(`/api/customs/${id}`, request),
  startReview: (id: number, rowVersion: string, comment?: string) =>
    api.post<CustomsDetail>(`/api/customs/${id}/start-review`, { rowVersion, comment: comment ?? null }),
  assignChannel: (id: number, rowVersion: string, channel: string, comment?: string) =>
    api.post<CustomsDetail>(`/api/customs/${id}/channel`, { rowVersion, channel, comment: comment ?? null }),
  addObservation: (id: number, observationType: string, severity: string, description: string) =>
    api.post<CustomsObservation>(`/api/customs/${id}/observations`, { observationType, severity, description }),
  resolveObservation: (id: number, observationId: number, rowVersion: string, resolutionNote: string) =>
    api.post<CustomsObservation>(`/api/customs/${id}/observations/${observationId}/resolve`, { rowVersion, resolutionNote }),
  addTax: (id: number, request: { customsTaxConceptId: number; amount: number; currencyCode: string; exchangeRate?: number | null; notes?: string | null }) =>
    api.post<CustomsTax>(`/api/customs/${id}/taxes`, request),
  updateTax: (id: number, taxId: number, request: { rowVersion: string; customsTaxConceptId: number; amount: number; currencyCode: string; exchangeRate?: number | null; notes?: string | null }) =>
    api.put<CustomsTax>(`/api/customs/${id}/taxes/${taxId}`, request),
  changeTaxStatus: (id: number, taxId: number, request: { rowVersion: string; paymentStatus: string; paymentReference?: string | null; paidAt?: string | null; comment?: string | null }) =>
    api.post<CustomsTax>(`/api/customs/${id}/taxes/${taxId}/status`, request),
  release: (id: number, rowVersion: string, releaseDate: string, comment?: string) =>
    api.post<CustomsDetail>(`/api/customs/${id}/release`, { rowVersion, releaseDate, comment: comment ?? null }),
  cancel: (id: number, rowVersion: string, reason: string) =>
    api.post<CustomsDetail>(`/api/customs/${id}/cancel`, { rowVersion, reason })
};
