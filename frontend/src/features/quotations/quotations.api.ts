import { api, downloadFile } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  CreateQuotation,
  CreateQuotationItem,
  NewQuotationVersion,
  QuotationDetail,
  QuotationItem,
  QuotationListItem,
  UpdateQuotation,
  QuotationProforma, QuotationConcept, QuotationSupplier, QuotationCargoItem, QuotationCustomsTax, UpdateQuotationProforma
} from "./types";

function qs(values: Record<string, string | number | boolean | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") {
      params.set(key, String(value));
    }
  });
  const text = params.toString();
  return text ? `?${text}` : "";
}

export const quotationsApi = {
  list: (query: {
    search?: string;
    status?: string;
    requestId?: number;
    clientId?: number;
    issueDateFrom?: string;
    issueDateTo?: string;
    onlyLatestVersion?: boolean;
    page: number;
    pageSize: number;
  }) =>
    api.get<PagedResult<QuotationListItem>>(
      `/api/quotations${qs(query)}`
    ),

  get: (quotationId: number) =>
    api.get<QuotationDetail>(`/api/quotations/${quotationId}`),

  byRequest: (requestId: number) =>
    api.get<QuotationListItem[]>(`/api/quotations/by-request/${requestId}`),

  createFromRequest: (requestId: number, request: CreateQuotation) =>
    api.post<QuotationDetail>(
      `/api/quotations/from-request/${requestId}`,
      request
    ),

  update: (quotationId: number, request: UpdateQuotation) =>
    api.put<QuotationDetail>(`/api/quotations/${quotationId}`, request),

  deleteDraft: (quotationId: number) =>
    api.delete<void>(`/api/quotations/${quotationId}`),

  addItem: (quotationId: number, request: CreateQuotationItem) =>
    api.post<QuotationItem>(`/api/quotations/${quotationId}/items`, request),

  updateItem: (
    quotationId: number,
    itemId: number,
    request: CreateQuotationItem
  ) =>
    api.put<QuotationItem>(
      `/api/quotations/${quotationId}/items/${itemId}`,
      request
    ),

  deleteItem: (quotationId: number, itemId: number) =>
    api.delete<void>(`/api/quotations/${quotationId}/items/${itemId}`),

  send: (quotationId: number, comment?: string) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/send`,
      { comment: comment ?? null }
    ),

  approve: (
    quotationId: number,
    approvedByName: string,
    comment?: string
  ) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/approve`,
      {
        approvedByName,
        comment: comment ?? null
      }
    ),

  reject: (quotationId: number, reason: string) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/reject`,
      { reason }
    ),

  cancel: (quotationId: number, reason: string) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/cancel`,
      { reason }
    ),

  expire: (quotationId: number, comment?: string) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/expire`,
      { comment: comment ?? null }
    ),

  proforma: (quotationId: number) => api.get<QuotationProforma>(`/api/quotations/${quotationId}/proforma`),
  downloadPdf: (quotationId: number) => downloadFile(`/api/quotations/${quotationId}/pdf`),
  updateProforma: (quotationId: number, request: UpdateQuotationProforma) => api.put<QuotationProforma>(`/api/quotations/${quotationId}/proforma`, request),
  concepts: () => api.get<QuotationConcept[]>(`/api/quotations/concepts`),
  addSupplier: (quotationId:number, request:{supplierId:number;displayOrder:number;notes?:string|null}) => api.post<QuotationSupplier>(`/api/quotations/${quotationId}/suppliers`, request),
  deleteSupplier: (quotationId:number, supplierId:number) => api.delete<void>(`/api/quotations/${quotationId}/suppliers/${supplierId}`),
  addCargo: (quotationId:number, request:Omit<QuotationCargoItem,"quotationCargoItemId"|"lineNo">) => api.post<QuotationCargoItem>(`/api/quotations/${quotationId}/cargo`, request),
  deleteCargo: (quotationId:number, cargoId:number) => api.delete<void>(`/api/quotations/${quotationId}/cargo/${cargoId}`),
  updateAdValorem: (quotationId:number, request:{amount:number;currencyCode:string;notes?:string|null}) => api.put<QuotationProforma>(`/api/quotations/${quotationId}/customs-taxes/ad-valorem`, request),
  addCustomsTax: (quotationId:number, request:{customsTaxConceptId:number;amount:number;currencyCode:string;notes?:string|null}) => api.post<QuotationCustomsTax>(`/api/quotations/${quotationId}/customs-taxes`, request),
  deleteCustomsTax: (quotationId:number, taxId:number) => api.delete<void>(`/api/quotations/${quotationId}/customs-taxes/${taxId}`),

  newVersion: (quotationId: number, request: NewQuotationVersion) =>
    api.post<QuotationDetail>(
      `/api/quotations/${quotationId}/new-version`,
      request
    )
};
