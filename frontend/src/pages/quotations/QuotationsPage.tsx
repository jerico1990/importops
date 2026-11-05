import {
  ChevronRight,
  FileCheck2,
  Search,
  SlidersHorizontal
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { useAuth } from "@/features/auth/AuthContext";
import { clientsApi } from "@/features/clients/clients.api";
import type { ClientListItem } from "@/features/clients/types";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { QuotationStatusCatalog } from "@/features/catalogs/types";
import { quotationsApi } from "@/features/quotations/quotations.api";
import type { QuotationListItem } from "@/features/quotations/types";
import { HttpError } from "@/lib/api/http";
import { formatDate, formatMoney } from "@/lib/format";

function tone(status: string) {
  if (status === "APPROVED") return "green" as const;
  if (status === "REJECTED" || status === "CANCELLED") return "red" as const;
  if (status === "EXPIRED") return "amber" as const;
  if (status === "SENT") return "blue" as const;
  return "neutral" as const;
}

export function QuotationsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [items, setItems] = useState<QuotationListItem[]>([]);
  const [statuses, setStatuses] = useState<QuotationStatusCatalog[]>([]);
  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState<string>();
  const [clientId, setClientId] = useState<string>();
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [onlyLatest, setOnlyLatest] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    catalogsApi.quotationStatuses().then(setStatuses).catch(() => setStatuses([]));
    if (!clientMode) {
      clientsApi.list({ page: 1, pageSize: 100 })
        .then((x) => setClients(x.items))
        .catch(() => setClients([]));
    }
  }, [clientMode]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await quotationsApi.list({
        search: debouncedSearch || undefined,
        status,
        clientId: clientMode ? undefined : clientId ? Number(clientId) : undefined,
        issueDateFrom: dateFrom || undefined,
        issueDateTo: dateTo || undefined,
        onlyLatestVersion: onlyLatest,
        page,
        pageSize
      });
      setItems(result.items);
      setTotal(result.total);
      setTotalPages(Math.max(result.totalPages, 1));
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudieron consultar las cotizaciones.");
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, status, clientId, dateFrom, dateTo, onlyLatest, page, pageSize, clientMode]);

  useEffect(() => { load(); }, [load]);

  const label = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  return (
    <>
      <PageHeader
        eyebrow={clientMode ? "Portal cliente" : "Comercial"}
        title={clientMode ? "Mis cotizaciones" : "Cotizaciones"}
        description={
          clientMode
            ? "Cotizaciones enviadas a tu empresa. Los borradores internos nunca se muestran."
            : "Versionado, conceptos, importes, envío y decisión comercial vinculados a solicitudes."
        }
      />

      <Card className="overflow-visible">
        <div className="grid gap-3 border-b border-slate-100 p-4 lg:grid-cols-2 xl:grid-cols-4">
          <div className="relative lg:col-span-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="N° cotización, solicitud o cliente..."
            />
          </div>

          <SearchableSelect
            value={status}
            onChange={(value) => { setStatus(value); setPage(1); }}
            options={statuses
              .filter((x) => !clientMode || x.code !== "DRAFT")
              .map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Todos los estados"
          />

          {!clientMode && (
            <SearchableSelect
              value={clientId}
              onChange={(value) => { setClientId(value); setPage(1); }}
              options={clients.map((x) => ({
                value: String(x.clientId),
                label: x.legalName,
                description: x.taxId ?? undefined
              }))}
              placeholder="Todos los clientes"
              searchPlaceholder="Buscar cliente..."
            />
          )}

          <div className="grid grid-cols-2 gap-2">
            <Input type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} />
            <Input type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} />
          </div>

          <label className="flex min-h-10 items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-700 shadow-sm">
            <input
              type="checkbox"
              checked={onlyLatest}
              onChange={(e) => { setOnlyLatest(e.target.checked); setPage(1); }}
              className="size-4 accent-blue-700"
            />
            Solo última versión
          </label>

          <Button
            variant="secondary"
            onClick={() => {
              setSearch("");
              setStatus(undefined);
              setClientId(undefined);
              setDateFrom("");
              setDateTo("");
              setOnlyLatest(true);
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
        ) : items.length === 0 ? (
          <EmptyState
            title="No hay cotizaciones"
            description={
              clientMode
                ? "No tienes cotizaciones enviadas por el momento."
                : "Las cotizaciones se crean desde una solicitud que se encuentre en revisión."
            }
          />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[980px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {[
                      "Cotización",
                      "Solicitud",
                      ...(clientMode ? [] : ["Cliente"]),
                      "Emisión",
                      "Vigencia",
                      "Total",
                      "Estado",
                      ""
                    ].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr
                      key={item.quotationId}
                      className="cursor-pointer border-b border-slate-100 transition last:border-0 hover:bg-slate-50/70"
                      onClick={() => navigate(`/cotizaciones/${item.quotationId}`)}
                    >
                      <td className="px-4 py-4">
                        <div className="font-semibold text-slate-900">{item.quotationNo}</div>
                        <div className="mt-1 text-xs text-slate-500">Versión {item.versionNo}</div>
                      </td>
                      <td className="px-4 py-4 text-sm font-medium text-slate-700">{item.requestNo}</td>
                      {!clientMode && <td className="px-4 py-4 text-sm text-slate-700">{item.clientName}</td>}
                      <td className="px-4 py-4 text-sm text-slate-600">{formatDate(item.issueDate)}</td>
                      <td className="px-4 py-4 text-sm text-slate-600">{formatDate(item.validUntil)}</td>
                      <td className="px-4 py-4 font-semibold tabular-nums text-slate-900">
                        {formatMoney(item.totalAmount, item.currencyCode)}
                      </td>
                      <td className="px-4 py-4"><Badge tone={tone(item.status)}>{label(item.status)}</Badge></td>
                      <td className="px-4 py-4"><ChevronRight className="ml-auto size-4 text-slate-400" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {items.map((item) => (
                <button
                  key={item.quotationId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() => navigate(`/cotizaciones/${item.quotationId}`)}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.quotationNo}</div>
                      <div className="mt-1 text-xs text-slate-500">{item.requestNo} · V{item.versionNo}</div>
                    </div>
                    <Badge tone={tone(item.status)}>{label(item.status)}</Badge>
                  </div>
                  <div className="mt-3 flex items-center justify-between text-sm">
                    <span className="text-slate-500">{formatDate(item.issueDate)}</span>
                    <span className="font-semibold text-slate-900">{formatMoney(item.totalAmount, item.currencyCode)}</span>
                  </div>
                </button>
              ))}
            </div>

            <div className="flex flex-col gap-3 border-t border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="text-xs text-slate-500">{total} cotización{total === 1 ? "" : "es"}</div>
              <div className="flex items-center gap-1">
                <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((x) => x - 1)}>Anterior</Button>
                <span className="px-3 text-sm font-semibold text-slate-700">{page} / {totalPages}</span>
                <Button variant="ghost" disabled={page >= totalPages} onClick={() => setPage((x) => x + 1)}>Siguiente</Button>
              </div>
            </div>
          </>
        )}
      </Card>
    </>
  );
}
