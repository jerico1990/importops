export type ShipmentListItem = {
  shipmentId: number;
  operationId: number;
  operationNo: string;
  shipmentStatus: string;
  transportModeId: number;
  transportMode: string;
  carrierName?: string | null;
  bookingNo?: string | null;
  masterDocumentNo?: string | null;
  houseDocumentNo?: string | null;
  originPortId?: number | null;
  originPort?: string | null;
  destinationPortId?: number | null;
  destinationPort?: string | null;
  etd?: string | null;
  eta?: string | null;
  actualDepartureAt?: string | null;
  actualArrivalAt?: string | null;
  containerCount: number;
};

export type ShipmentContainer = {
  shipmentContainerId: number;
  containerNo: string;
  containerType?: string | null;
  sealNo?: string | null;
  packageCount?: number | null;
  grossWeightKg?: number | null;
  volumeM3?: number | null;
  notes?: string | null;
  isActive: boolean;
};

export type ShipmentDetail = {
  shipmentId: number;
  operationId: number;
  operationNo: string;
  shipmentStatus: string;
  transportModeId: number;
  transportMode: string;
  carrierName?: string | null;
  vesselFlightName?: string | null;
  bookingNo?: string | null;
  masterDocumentNo?: string | null;
  houseDocumentNo?: string | null;
  originPortId?: number | null;
  originPort?: string | null;
  destinationPortId?: number | null;
  destinationPort?: string | null;
  etd?: string | null;
  eta?: string | null;
  actualDepartureAt?: string | null;
  actualArrivalAt?: string | null;
  notes?: string | null;
  createdAt: string;
  modifiedAt?: string | null;
  rowVersion: string;
  containers: ShipmentContainer[];
};

export type ShipmentAvailableAction = {
  code: string;
  name: string;
  requiresEventAt: boolean;
  requiresComment: boolean;
  isEnabled: boolean;
  blockedReason?: string | null;
};

export type ShipmentFormPayload = {
  transportModeId?: number | null;
  carrierName?: string | null;
  vesselFlightName?: string | null;
  bookingNo?: string | null;
  masterDocumentNo?: string | null;
  houseDocumentNo?: string | null;
  originPortId?: number | null;
  destinationPortId?: number | null;
  etd?: string | null;
  eta?: string | null;
  notes?: string | null;
};

export type ShipmentContainerPayload = {
  containerNo: string;
  containerType?: string | null;
  sealNo?: string | null;
  packageCount?: number | null;
  grossWeightKg?: number | null;
  volumeM3?: number | null;
  notes?: string | null;
  isActive: boolean;
};
