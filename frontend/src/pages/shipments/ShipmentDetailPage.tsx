import {
  ArrowLeft,
  Box,
  CalendarClock,
  Pencil,
  Plus,
  Trash2
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { EmptyState } from "@/components/feedback/EmptyState";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog, TransportMode } from "@/features/catalogs/types";
import { shipmentsApi } from "@/features/shipments/shipments.api";
import type {
  ShipmentAvailableAction,
  ShipmentContainer,
  ShipmentDetail
} from "@/features/shipments/types";
import { ShipmentFormDialog } from "@/features/shipments/ShipmentFormDialog";
import { ShipmentContainerDialog } from "@/features/shipments/ShipmentContainerDialog";
import { ShipmentActionDialog } from "@/features/shipments/ShipmentActionDialog";
import { formatDate } from "@/lib/format";

function tone(status: string) {
  if (status === "ARRIVED") return "green" as const;
  if (status === "CANCELLED") return "red" as const;
  if (status === "DEPARTED" || status === "IN_TRANSIT") return "blue" as const;
  if (status === "BOOKED") return "violet" as const;
  return "neutral" as const;
}

export function ShipmentDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const shipmentId = Number(id);
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";
  const canManage = hasPermission(Permissions.ShipmentsManage);

  const [shipment, setShipment] = useState<ShipmentDetail | null>(null);
  const [actions, setActions] = useState<ShipmentAvailableAction[]>([]);
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [modes, setModes] = useState<TransportMode[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [containerOpen, setContainerOpen] = useState(false);
  const [editingContainer, setEditingContainer] = useState<ShipmentContainer | null>(null);
  const [selectedAction, setSelectedAction] = useState<ShipmentAvailableAction | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [detail, statusData, modeData] = await Promise.all([
        shipmentsApi.get(shipmentId),
        catalogsApi.shipmentStatuses(),
        catalogsApi.transportModes()
      ]);
      setShipment(detail);
      setStatuses(statusData);
      setModes(modeData);

      if (!clientMode && canManage) {
        setActions(await shipmentsApi.availableActions(shipmentId));
      } else {
        setActions([]);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar el embarque.");
    } finally {
      setLoading(false);
    }
  }, [shipmentId, clientMode, canManage]);

  useEffect(() => { void load(); }, [load]);

  const modeCode = useMemo(
    () => modes.find((x) => x.transportModeId === shipment?.transportModeId)?.code,
    [modes, shipment?.transportModeId]
  );

  const statusName = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  if (loading) return <div className="space-y-4"><Skeleton className="h-20" /><Skeleton className="h-96" /></div>;
  if (error && !shipment) return <InlineAlert message={error} />;
  if (!shipment) return null;

  const headerEditable = ["PLANNED", "BOOKED"].includes(shipment.shipmentStatus);
  const containersEditable = ["PLANNED", "BOOKED"].includes(shipment.shipmentStatus);

  const removeContainer = async (container: ShipmentContainer) => {
    if (!window.confirm(`¿Retirar el contenedor ${container.containerNo}?`)) return;
    try {
      await shipmentsApi.deleteContainer(shipment.shipmentId, container.shipmentContainerId);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo retirar el contenedor.");
    }
  };

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/embarques")}
      >
        <ArrowLeft className="size-4" />
        Volver a embarques
      </button>

      <PageHeader
        eyebrow="Embarque"
        title={`Shipment #${shipment.shipmentId}`}
        description={`${shipment.operationNo} · ${shipment.transportMode}`}
        actions={
          <>
            {!clientMode && canManage && headerEditable && (
              <Button variant="secondary" onClick={() => setFormOpen(true)}>
                <Pencil className="size-4" />
                Editar
              </Button>
            )}
            {!clientMode && canManage && actions.map((action) => (
              <Button
                key={action.code}
                variant={action.code === "CANCEL" ? "ghost" : "primary"}
                disabled={!action.isEnabled}
                title={action.blockedReason ?? undefined}
                onClick={() => setSelectedAction(action)}
              >
                {action.name}
              </Button>
            ))}
          </>
        }
      />

      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      <div className="mb-5 flex flex-wrap gap-2">
        <Badge tone={tone(shipment.shipmentStatus)}>{statusName(shipment.shipmentStatus)}</Badge>
        <Badge tone="blue">{shipment.transportMode}</Badge>
        {shipment.bookingNo && <Badge tone="neutral">Booking {shipment.bookingNo}</Badge>}
      </div>

      {!clientMode && canManage && actions.some((x) => !x.isEnabled) && (
        <div className="mb-5 rounded-2xl border border-amber-200 bg-amber-50 p-4">
          <div className="font-semibold text-amber-900">Datos pendientes para continuar</div>
          <div className="mt-2 space-y-1 text-sm text-amber-800">
            {actions.filter((x) => !x.isEnabled && x.blockedReason).map((x) => (
              <div key={x.code}>• {x.name}: {x.blockedReason}</div>
            ))}
          </div>
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-[1.2fr_0.8fr]">
        <Card className="p-5">
          <h2 className="font-semibold text-slate-900">Datos logísticos</h2>
          <div className="mt-5 grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
            {[
              ["Operación", shipment.operationNo],
              ["Carrier", shipment.carrierName || "—"],
              [modeCode === "AIR" ? "Vuelo" : modeCode === "SEA" ? "Nave / viaje" : "Referencia", shipment.vesselFlightName || "—"],
              ["Booking", shipment.bookingNo || "—"],
              [modeCode === "AIR" ? "MAWB" : modeCode === "SEA" ? "MBL" : "Documento maestro", shipment.masterDocumentNo || "—"],
              [modeCode === "AIR" ? "HAWB" : modeCode === "SEA" ? "HBL" : "Documento house", shipment.houseDocumentNo || "—"],
              ["Origen", shipment.originPort || "—"],
              ["Destino", shipment.destinationPort || "—"],
              ["ETD", formatDate(shipment.etd)],
              ["ETA", formatDate(shipment.eta)],
              ["Salida real", formatDate(shipment.actualDepartureAt)],
              ["Arribo real", formatDate(shipment.actualArrivalAt)]
            ].map(([k, v]) => (
              <div key={k}>
                <div className="text-xs font-medium text-slate-400">{k}</div>
                <div className="mt-1 text-sm font-medium text-slate-800">{v}</div>
              </div>
            ))}
          </div>

          {!clientMode && shipment.notes && (
            <div className="mt-6 border-t border-slate-100 pt-5">
              <div className="text-xs font-medium text-slate-400">Notas internas</div>
              <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">{shipment.notes}</p>
            </div>
          )}

          <Link
            to={`/operaciones/${shipment.operationId}`}
            className="mt-6 inline-flex text-sm font-semibold text-blue-700"
          >
            Ver expediente operativo
          </Link>
        </Card>

        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
            <div>
              <h2 className="font-semibold text-slate-900">Contenedores</h2>
              <p className="mt-1 text-sm text-slate-500">
                {modeCode === "SEA" ? "Detalle físico del embarque marítimo." : "Disponible solo para transporte marítimo."}
              </p>
            </div>
            {!clientMode && canManage && modeCode === "SEA" && containersEditable && (
              <Button onClick={() => { setEditingContainer(null); setContainerOpen(true); }}>
                <Plus className="size-4" />
                Agregar
              </Button>
            )}
          </div>

          {shipment.containers.length === 0 ? (
            <EmptyState
              title={modeCode === "SEA" ? "Sin contenedores" : "No aplica"}
              description={
                modeCode === "SEA"
                  ? "Registra los contenedores antes de confirmar la salida."
                  : "Este embarque no requiere gestión de contenedores."
              }
            />
          ) : (
            <div className="divide-y divide-slate-100">
              {shipment.containers.map((container) => (
                <div key={container.shipmentContainerId} className="px-5 py-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <Box className="size-4 text-slate-500" />
                        <span className="font-semibold text-slate-900">{container.containerNo}</span>
                        {!container.isActive && <Badge tone="red">Inactivo</Badge>}
                      </div>
                      <div className="mt-2 text-xs text-slate-500">
                        {container.containerType || "Tipo no indicado"}
                        {container.sealNo ? ` · Sello ${container.sealNo}` : ""}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        {container.packageCount != null ? `${container.packageCount} bultos` : ""}
                        {container.grossWeightKg != null ? ` · ${container.grossWeightKg} kg` : ""}
                        {container.volumeM3 != null ? ` · ${container.volumeM3} m³` : ""}
                      </div>
                    </div>
                    {!clientMode && canManage && containersEditable && (
                      <div className="flex gap-1">
                        <Button variant="ghost" onClick={() => { setEditingContainer(container); setContainerOpen(true); }}>
                          <Pencil className="size-4" />
                        </Button>
                        <Button variant="ghost" onClick={() => void removeContainer(container)}>
                          <Trash2 className="size-4" />
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      <ShipmentFormDialog
        open={formOpen}
        operationId={shipment.operationId}
        shipment={shipment}
        onClose={() => setFormOpen(false)}
        onSaved={(saved) => {
          setShipment(saved);
          setFormOpen(false);
          void load();
        }}
      />

      <ShipmentContainerDialog
        open={containerOpen}
        shipmentId={shipment.shipmentId}
        container={editingContainer}
        onClose={() => {
          setContainerOpen(false);
          setEditingContainer(null);
        }}
        onSaved={async () => {
          setContainerOpen(false);
          setEditingContainer(null);
          await load();
        }}
      />

      <ShipmentActionDialog
        open={selectedAction !== null}
        shipmentId={shipment.shipmentId}
        action={selectedAction}
        onClose={() => setSelectedAction(null)}
        onCompleted={(saved) => {
          setShipment(saved);
          setSelectedAction(null);
          void load();
        }}
      />
    </>
  );
}
