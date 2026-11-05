export type ImportStatus =
  | "DRAFT"
  | "PENDING_SHIPMENT"
  | "IN_TRANSIT"
  | "ARRIVED"
  | "CUSTOMS"
  | "RELEASED"
  | "DELIVERY"
  | "SETTLEMENT"
  | "CLOSED";

export type DemoImportRow = {
  id: number;
  operationNo: string;
  client: string;
  origin: string;
  destination: string;
  transport: "SEA" | "AIR";
  carrier: string;
  eta: string;
  status: ImportStatus;
  customsChannel?: "GREEN" | "YELLOW" | "RED";
  responsible: string;
  amount: number;
  currency: "USD";
};
