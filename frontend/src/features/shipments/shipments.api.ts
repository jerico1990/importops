import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type {
  ShipmentAvailableAction,
  ShipmentContainer,
  ShipmentContainerPayload,
  ShipmentDetail,
  ShipmentFormPayload,
  ShipmentListItem
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

export const shipmentsApi = {
  list: (query: {
    operationId?: number;
    status?: string;
    transportModeId?: number;
    search?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PagedResult<ShipmentListItem>>(`/api/shipments${qs(query)}`),

  byOperation: (operationId: number) =>
    api.get<ShipmentListItem[]>(`/api/shipments/by-operation/${operationId}`),

  get: (shipmentId: number) =>
    api.get<ShipmentDetail>(`/api/shipments/${shipmentId}`),

  availableActions: (shipmentId: number) =>
    api.get<ShipmentAvailableAction[]>(
      `/api/shipments/${shipmentId}/available-actions`
    ),

  create: (operationId: number, payload: ShipmentFormPayload) =>
    api.post<ShipmentDetail>(
      `/api/shipments/from-operation/${operationId}`,
      payload
    ),

  update: (
    shipmentId: number,
    payload: ShipmentFormPayload & { rowVersion: string }
  ) => api.put<ShipmentDetail>(`/api/shipments/${shipmentId}`, payload),

  deletePlanned: (shipmentId: number, rowVersion: string) =>
    api.delete<void>(
      `/api/shipments/${shipmentId}?rowVersion=${encodeURIComponent(rowVersion)}`
    ),

  addContainer: (
    shipmentId: number,
    payload: ShipmentContainerPayload
  ) =>
    api.post<ShipmentContainer>(
      `/api/shipments/${shipmentId}/containers`,
      payload
    ),

  updateContainer: (
    shipmentId: number,
    containerId: number,
    payload: ShipmentContainerPayload
  ) =>
    api.put<ShipmentContainer>(
      `/api/shipments/${shipmentId}/containers/${containerId}`,
      payload
    ),

  deleteContainer: (shipmentId: number, containerId: number) =>
    api.delete<void>(
      `/api/shipments/${shipmentId}/containers/${containerId}`
    ),

  action: (
    shipmentId: number,
    actionCode: string,
    eventAt?: string,
    comment?: string
  ) => {
    if (actionCode === "BOOK") {
      return api.post<ShipmentDetail>(
        `/api/shipments/${shipmentId}/book`,
        { comment: comment ?? null }
      );
    }
    if (actionCode === "DEPART") {
      return api.post<ShipmentDetail>(
        `/api/shipments/${shipmentId}/depart`,
        { eventAt: eventAt ?? null, comment: comment ?? null }
      );
    }
    if (actionCode === "IN_TRANSIT") {
      return api.post<ShipmentDetail>(
        `/api/shipments/${shipmentId}/in-transit`,
        { comment: comment ?? null }
      );
    }
    if (actionCode === "ARRIVE") {
      return api.post<ShipmentDetail>(
        `/api/shipments/${shipmentId}/arrive`,
        { eventAt: eventAt ?? null, comment: comment ?? null }
      );
    }
    if (actionCode === "CANCEL") {
      return api.post<ShipmentDetail>(
        `/api/shipments/${shipmentId}/cancel`,
        { reason: comment ?? "" }
      );
    }
    throw new Error(`Acción de embarque no soportada: ${actionCode}`);
  }
};
