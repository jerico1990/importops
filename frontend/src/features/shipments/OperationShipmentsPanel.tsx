import { Plus, Ship } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { shipmentsApi } from "./shipments.api";
import type { ShipmentDetail, ShipmentListItem } from "./types";
import { ShipmentFormDialog } from "./ShipmentFormDialog";
import { formatDate } from "@/lib/format";

function tone(status: string) {
  if (status === "ARRIVED") return "green" as const;
  if (status === "CANCELLED") return "red" as const;
  if (status === "DEPARTED" || status === "IN_TRANSIT") return "blue" as const;
  if (status === "BOOKED") return "violet" as const;
  return "neutral" as const;
}

export function OperationShipmentsPanel({
  operationId,
  canManage,
  clientMode
}: {
  operationId: number;
  canManage: boolean;
  clientMode: boolean;
}) {
  const navigate = useNavigate();
  const [items, setItems] = useState<ShipmentListItem[]>([]);
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [formOpen, setFormOpen] = useState(false);
  const [error, setError] = useState("");

  const load = async () => {
    try {
      const [rows, statusData] = await Promise.all([
        shipmentsApi.byOperation(operationId),
        catalogsApi.shipmentStatuses()
      ]);
      setItems(rows);
      setStatuses(statusData);
      setError("");
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudieron cargar los embarques.");
    }
  };

  useEffect(() => { void load(); }, [operationId]);

  const name = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  return (
    <>
      <Card className="overflow-hidden">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold text-slate-900">Embarques de la operación</h2>
            <p className="mt-1 text-sm text-slate-500">
              Booking, documentos de transporte, ETD/ETA, salida, arribo y contenedores.
            </p>
          </div>
          {!clientMode && canManage && (
            <Button onClick={() => setFormOpen(true)}>
              <Plus className="size-4" />
              Nuevo embarque
            </Button>
          )}
        </div>

        {error && <div className="p-4"><InlineAlert message={error} /></div>}

        {items.length === 0 ? (
          <EmptyState
            title="Sin embarques"
            description={
              clientMode
                ? "Aún no existe un embarque visible para esta operación."
                : "Crea el embarque cuando la coordinación logística esté definida."
            }
          />
        ) : (
          <div className="divide-y divide-slate-100">
            {items.map((item) => (
              <button
                key={item.shipmentId}
                type="button"
                className="flex w-full flex-col gap-3 px-5 py-4 text-left transition hover:bg-slate-50 lg:flex-row lg:items-center lg:justify-between"
                onClick={() => navigate(`/embarques/${item.shipmentId}`)}
              >
                <div className="flex min-w-0 gap-3">
                  <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-blue-50 text-blue-700">
                    <Ship className="size-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="font-semibold text-slate-900">
                      {item.transportMode} · {item.carrierName || "Carrier pendiente"}
                    </div>
                    <div className="mt-1 text-xs text-slate-500">
                      {item.bookingNo ? `Booking ${item.bookingNo}` : "Sin booking"}
                      {item.houseDocumentNo || item.masterDocumentNo
                        ? ` · ${item.houseDocumentNo || item.masterDocumentNo}`
                        : ""}
                    </div>
                    <div className="mt-1 text-xs text-slate-500">
                      {[item.originPort, item.destinationPort].filter(Boolean).join(" → ") || "Ruta pendiente"}
                      {item.eta ? ` · ETA ${formatDate(item.eta)}` : ""}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {item.containerCount > 0 && <Badge tone="neutral">{item.containerCount} cont.</Badge>}
                  <Badge tone={tone(item.shipmentStatus)}>{name(item.shipmentStatus)}</Badge>
                </div>
              </button>
            ))}
          </div>
        )}
      </Card>

      <ShipmentFormDialog
        open={formOpen}
        operationId={operationId}
        onClose={() => setFormOpen(false)}
        onSaved={(_saved: ShipmentDetail) => {
          setFormOpen(false);
          void load();
        }}
      />
    </>
  );
}
