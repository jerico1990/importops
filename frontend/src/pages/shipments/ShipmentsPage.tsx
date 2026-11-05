import { ChevronRight, Search, SlidersHorizontal } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog, TransportMode } from "@/features/catalogs/types";
import { shipmentsApi } from "@/features/shipments/shipments.api";
import type { ShipmentListItem } from "@/features/shipments/types";
import type { PagedResult } from "@/types/common";
import { formatDate } from "@/lib/format";

function tone(status: string) {
  if (status === "ARRIVED") return "green" as const;
  if (status === "CANCELLED") return "red" as const;
  if (status === "IN_TRANSIT" || status === "DEPARTED") return "blue" as const;
  if (status === "BOOKED") return "violet" as const;
  return "neutral" as const;
}

export function ShipmentsPage() {
  const navigate = useNavigate();
  const [result, setResult] = useState<PagedResult<ShipmentListItem>>({
    items: [], page: 1, pageSize: 20, total: 0, totalPages: 0
  });
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [modes, setModes] = useState<TransportMode[]>([]);
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [status, setStatus] = useState<string>();
  const [transportModeId, setTransportModeId] = useState<string>();
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setApplied(search.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    Promise.all([
      catalogsApi.shipmentStatuses(),
      catalogsApi.transportModes()
    ]).then(([a, b]) => {
      setStatuses(a);
      setModes(b);
    }).catch(() => {});
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setResult(await shipmentsApi.list({
        search: applied || undefined,
        status,
        transportModeId: transportModeId ? Number(transportModeId) : undefined,
        page,
        pageSize: 20
      }));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudieron cargar los embarques.");
    } finally {
      setLoading(false);
    }
  }, [applied, status, transportModeId, page]);

  useEffect(() => { void load(); }, [load]);

  const statusName = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  return (
    <>
      <PageHeader
        eyebrow="Operación"
        title="Embarques"
        description="Booking, documentos de transporte, fechas reales, contenedores y tracking operativo."
      />

      <Card className="overflow-visible">
        <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="relative md:col-span-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Operación, booking, BL/AWB o carrier..."
            />
          </div>

          <SearchableSelect
            value={status}
            onChange={(v) => { setStatus(v); setPage(1); }}
            options={statuses.map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Todos los estados"
          />

          <SearchableSelect
            value={transportModeId}
            onChange={(v) => { setTransportModeId(v); setPage(1); }}
            options={modes.map((x) => ({
              value: String(x.transportModeId),
              label: x.name,
              description: x.code
            }))}
            placeholder="Todas las modalidades"
          />

          <Button
            variant="secondary"
            onClick={() => {
              setSearch("");
              setApplied("");
              setStatus(undefined);
              setTransportModeId(undefined);
              setPage(1);
            }}
          >
            <SlidersHorizontal className="size-4" />
            Limpiar filtros
          </Button>
        </div>

        {error && <div className="p-4"><InlineAlert message={error} /></div>}

        {loading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        ) : result.items.length === 0 ? (
          <EmptyState
            title="No hay embarques"
            description="Los embarques se crean dentro de una operación."
          />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[1100px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {["Operación","Modalidad","Carrier","Booking","Documento","Ruta","ETD / ETA","Estado",""].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((item) => (
                    <tr
                      key={item.shipmentId}
                      className="cursor-pointer border-b border-slate-100 transition hover:bg-slate-50/70"
                      onClick={() => navigate(`/embarques/${item.shipmentId}`)}
                    >
                      <td className="px-4 py-4 font-semibold text-slate-900">{item.operationNo}</td>
                      <td className="px-4 py-4 text-sm text-slate-700">{item.transportMode}</td>
                      <td className="px-4 py-4 text-sm text-slate-600">{item.carrierName || "—"}</td>
                      <td className="px-4 py-4 text-sm text-slate-600">{item.bookingNo || "—"}</td>
                      <td className="px-4 py-4 text-sm text-slate-600">{item.houseDocumentNo || item.masterDocumentNo || "—"}</td>
                      <td className="px-4 py-4 text-sm text-slate-600">
                        {[item.originPort, item.destinationPort].filter(Boolean).join(" → ") || "—"}
                      </td>
                      <td className="px-4 py-4 text-xs text-slate-500">
                        <div>{formatDate(item.etd)}</div>
                        <div className="mt-1">{formatDate(item.eta)}</div>
                      </td>
                      <td className="px-4 py-4">
                        <Badge tone={tone(item.shipmentStatus)}>{statusName(item.shipmentStatus)}</Badge>
                      </td>
                      <td className="px-4 py-4"><ChevronRight className="ml-auto size-4 text-slate-400" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {result.items.map((item) => (
                <button
                  key={item.shipmentId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() => navigate(`/embarques/${item.shipmentId}`)}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.operationNo}</div>
                      <div className="mt-1 text-xs text-slate-500">{item.transportMode} · {item.carrierName || "Sin carrier"}</div>
                    </div>
                    <Badge tone={tone(item.shipmentStatus)}>{statusName(item.shipmentStatus)}</Badge>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">
                    {[item.originPort, item.destinationPort].filter(Boolean).join(" → ") || "Ruta sin completar"}
                  </div>
                </button>
              ))}
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 px-4 py-3">
              <span className="text-xs text-slate-500">{result.total} embarque{result.total === 1 ? "" : "s"}</span>
              <div className="flex items-center gap-1">
                <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((x) => x - 1)}>Anterior</Button>
                <span className="px-3 text-sm font-semibold text-slate-700">{page} / {Math.max(result.totalPages, 1)}</span>
                <Button variant="ghost" disabled={page >= Math.max(result.totalPages, 1)} onClick={() => setPage((x) => x + 1)}>Siguiente</Button>
              </div>
            </div>
          </>
        )}
      </Card>
    </>
  );
}
