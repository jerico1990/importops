import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  DocumentObservation,
  DocumentRecord,
  OperationDocumentStatus
} from "./types";

function qs(values: Record<string, string | number | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") {
      params.set(key, String(value));
    }
  });
  const text = params.toString();
  return text ? `?${text}` : "";
}

export const documentsApi = {
  list: (query: {
    requestId?: number;
    quotationId?: number;
    operationId?: number;
    documentTypeId?: number;
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) => api.get<PagedResult<DocumentRecord>>(`/api/documents${qs(query)}`),

  get: (documentId: number) =>
    api.get<DocumentRecord>(`/api/documents/${documentId}`),

  observations: (documentId: number) =>
    api.get<DocumentObservation[]>(
      `/api/documents/${documentId}/observations`
    ),

  requirements: (operationId: number) =>
    api.get<OperationDocumentStatus>(
      `/api/documents/operation/${operationId}/requirements`
    ),

  refreshRequirements: (operationId: number) =>
    api.post<OperationDocumentStatus>(
      `/api/documents/operation/${operationId}/refresh-status`,
      {}
    ),

  upload: (
    operationId: number,
    payload: {
      file: File;
      documentTypeId: number;
      documentDate?: string;
      expirationDate?: string;
      notes?: string;
      visibility: string;
    }
  ) => {
    const data = new FormData();
    data.append("file", payload.file);
    data.append("operationId", String(operationId));
    data.append("documentTypeId", String(payload.documentTypeId));
    if (payload.documentDate) data.append("documentDate", payload.documentDate);
    if (payload.expirationDate) data.append("expirationDate", payload.expirationDate);
    if (payload.notes) data.append("notes", payload.notes);
    data.append("visibility", payload.visibility);
    return api.post<DocumentRecord>("/api/documents", data);
  },

  replace: (
    documentId: number,
    file: File,
    notes?: string
  ) => {
    const data = new FormData();
    data.append("file", file);
    if (notes) data.append("notes", notes);
    return api.post<DocumentRecord>(
      `/api/documents/${documentId}/replace`,
      data
    );
  },

  validate: (documentId: number, rowVersion: string, comment?: string) =>
    api.post<DocumentRecord>(`/api/documents/${documentId}/validate`, {
      rowVersion,
      comment: comment ?? null
    }),

  observe: (
    documentId: number,
    rowVersion: string,
    observation: string
  ) =>
    api.post<DocumentObservation>(
      `/api/documents/${documentId}/observe`,
      { rowVersion, observation }
    ),

  resolveObservation: (
    documentId: number,
    observationId: number,
    rowVersion: string,
    resolutionNote: string
  ) =>
    api.post<DocumentObservation>(
      `/api/documents/${documentId}/observations/${observationId}/resolve`,
      { rowVersion, resolutionNote }
    ),

  void: (documentId: number, rowVersion: string, reason: string) =>
    api.post<DocumentRecord>(`/api/documents/${documentId}/void`, {
      rowVersion,
      reason
    }),

  download: async (documentId: number, fileName: string) => {
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
