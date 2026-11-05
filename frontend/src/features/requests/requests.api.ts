import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  CreateImportRequest,
  ImportRequestDetail,
  ImportRequestListItem,
  QuotationSummary,
  RequestDocument,
  UpdateImportRequest
} from "./types";

function qs(values: Record<string, string | number | boolean | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") {
      params.set(key, String(value));
    }
  });
  const value = params.toString();
  return value ? `?${value}` : "";
}

export const requestsApi = {
  list: (query: {
    search?: string;
    status?: string;
    clientId?: number;
    supplierId?: number;
    assignedUserId?: number;
    transportModeId?: number;
    dateFrom?: string;
    dateTo?: string;
    page: number;
    pageSize: number;
  }) =>
    api.get<PagedResult<ImportRequestListItem>>(
      `/api/import-requests${qs(query)}`
    ),

  get: (requestId: number) =>
    api.get<ImportRequestDetail>(`/api/import-requests/${requestId}`),

  create: (request: CreateImportRequest) =>
    api.post<ImportRequestDetail>("/api/import-requests", request),

  update: (requestId: number, request: UpdateImportRequest) =>
    api.put<ImportRequestDetail>(`/api/import-requests/${requestId}`, request),

  deleteDraft: (requestId: number) =>
    api.delete<void>(`/api/import-requests/${requestId}`),

  submitReview: (requestId: number, comment?: string) =>
    api.post<ImportRequestDetail>(
      `/api/import-requests/${requestId}/submit-review`,
      { comment: comment ?? "" }
    ),

  observe: (requestId: number, comment: string) =>
    api.post<ImportRequestDetail>(
      `/api/import-requests/${requestId}/observe`,
      { comment }
    ),

  reject: (requestId: number, comment: string) =>
    api.post<ImportRequestDetail>(
      `/api/import-requests/${requestId}/reject`,
      { comment }
    ),

  cancel: (requestId: number, comment: string) =>
    api.post<ImportRequestDetail>(
      `/api/import-requests/${requestId}/cancel`,
      { comment }
    ),

  assign: (requestId: number, assignedUserId?: number, comment?: string) =>
    api.post<ImportRequestDetail>(
      `/api/import-requests/${requestId}/assign`,
      {
        assignedUserId: assignedUserId ?? null,
        comment: comment ?? null
      }
    ),

  quotations: (requestId: number) =>
    api.get<QuotationSummary[]>(`/api/quotations/by-request/${requestId}`),

  documents: (requestId: number) =>
    api.get<PagedResult<RequestDocument>>(
      `/api/documents${qs({ requestId, page: 1, pageSize: 100 })}`
    ),

  uploadDocument: (
    requestId: number,
    form: {
      file: File;
      documentTypeId: number;
      documentDate?: string;
      expirationDate?: string;
      notes?: string;
      visibility: "CLIENT" | "BOTH";
    }
  ) => {
    const data = new FormData();
    data.append("file", form.file);
    data.append("requestId", String(requestId));
    data.append("documentTypeId", String(form.documentTypeId));
    if (form.documentDate) data.append("documentDate", form.documentDate);
    if (form.expirationDate) data.append("expirationDate", form.expirationDate);
    if (form.notes) data.append("notes", form.notes);
    data.append("visibility", form.visibility);

    return api.post<RequestDocument>("/api/documents", data);
  },

  downloadDocument: async (documentId: number, fileName: string) => {
    const response = await fetch(`/api/documents/${documentId}/download`, {
      method: "GET",
      credentials: "include"
    });
    if (!response.ok) throw new Error("No se pudo descargar el documento.");
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  }
};
