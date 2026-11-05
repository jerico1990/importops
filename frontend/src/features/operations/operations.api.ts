import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  OperationAvailableStage,
  OperationDetail,
  OperationEvent,
  OperationListItem,
  OperationNote,
  OperationQuery,
  OperationStageHistory,
  OperationStatusHistory,
  OperationTask
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

export const operationsApi = {
  list: (query: OperationQuery = {}) =>
    api.get<PagedResult<OperationListItem>>(
      `/api/operations${queryString(query as Record<string, string | number | undefined | null>)}`
    ),

  get: (operationId: number) =>
    api.get<OperationDetail>(`/api/operations/${operationId}`),

  createFromQuotation: (
    quotationId: number,
    request: {
      responsibleUserId: number;
      estimatedCloseDate?: string | null;
      notes?: string | null;
    }
  ) =>
    api.post<OperationDetail>(
      `/api/operations/from-quotation/${quotationId}`,
      request
    ),

  assign: (operationId: number, responsibleUserId: number, comment?: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/assign`, {
      responsibleUserId,
      comment: comment ?? null
    }),

  availableStages: (operationId: number) =>
    api.get<OperationAvailableStage[]>(
      `/api/operations/${operationId}/available-stages`
    ),

  changeStage: (operationId: number, toStageId: number, comment?: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/change-stage`, {
      toStageId,
      comment: comment ?? null
    }),

  stageHistory: (operationId: number) =>
    api.get<OperationStageHistory[]>(`/api/operations/${operationId}/stage-history`),

  statusHistory: (operationId: number) =>
    api.get<OperationStatusHistory[]>(`/api/operations/${operationId}/status-history`),

  hold: (operationId: number, comment: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/hold`, { comment }),

  resume: (operationId: number, comment: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/resume`, { comment }),

  cancel: (operationId: number, comment: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/cancel`, { comment }),

  close: (operationId: number, comment: string) =>
    api.post<OperationDetail>(`/api/operations/${operationId}/close`, { comment }),

  tasks: (operationId: number) =>
    api.get<OperationTask[]>(`/api/operations/${operationId}/tasks`),

  createTask: (operationId: number, request: {
    workflowStageId?: number | null;
    title: string;
    description?: string | null;
    priority: string;
    assignedUserId?: number | null;
    dueAt?: string | null;
    isMandatory: boolean;
  }) => api.post<OperationTask>(`/api/operations/${operationId}/tasks`, request),

  updateTask: (operationId: number, taskId: number, request: {
    title: string;
    description?: string | null;
    priority: string;
    assignedUserId?: number | null;
    dueAt?: string | null;
    isMandatory: boolean;
  }) =>
    api.put<OperationTask>(`/api/operations/${operationId}/tasks/${taskId}`, request),

  changeTaskStatus: (
    operationId: number,
    taskId: number,
    status: string,
    comment?: string
  ) =>
    api.post<OperationTask>(
      `/api/operations/${operationId}/tasks/${taskId}/status`,
      { status, comment: comment ?? null }
    ),

  events: (operationId: number) =>
    api.get<OperationEvent[]>(`/api/operations/${operationId}/events`),

  addEvent: (operationId: number, request: {
    eventType: string;
    eventDate?: string | null;
    title: string;
    description?: string | null;
    visibility: string;
  }) => api.post<OperationEvent>(`/api/operations/${operationId}/events`, request),

  notes: (operationId: number) =>
    api.get<OperationNote[]>(`/api/operations/${operationId}/notes`),

  addNote: (operationId: number, request: {
    noteType: string;
    noteText: string;
    isPinned: boolean;
  }) => api.post<OperationNote>(`/api/operations/${operationId}/notes`, request)
};
