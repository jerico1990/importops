import {
  ChevronRight,
  ClipboardList,
  Plus,
  Search,
  SlidersHorizontal
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useMemo,
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
import { PermissionGate } from "@/features/auth/PermissionGate";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { clientsApi } from "@/features/clients/clients.api";
import { suppliersApi } from "@/features/suppliers/suppliers.api";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type {
  AppUserCatalog,
  ImportRequestStatusCatalog,
  TransportMode
} from "@/features/catalogs/types";
import { requestsApi } from "@/features/requests/requests.api";
import { RequestFormDialog } from "@/features/requests/RequestFormDialog";
import type {
  ImportRequestDetail,
  ImportRequestListItem
} from "@/features/requests/types";
import type { ClientListItem } from "@/features/clients/types";
import type { SupplierListItem } from "@/features/suppliers/types";
import { HttpError } from "@/lib/api/http";
import { formatDate } from "@/lib/format";

function userName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

function tone(status: string) {
  if (status === "APPROVED") return "green" as const;
  if (status === "REJECTED" || status === "CANCELLED") return "red" as const;
  if (status === "OBSERVED") return "amber" as const;
  if (status === "QUOTED") return "violet" as const;
  if (status === "UNDER_REVIEW") return "blue" as const;
  return "neutral" as const;
}

export function RequestsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [items, setItems] = useState<ImportRequestListItem[]>([]);
  const [statuses, setStatuses] = useState<ImportRequestStatusCatalog[]>([]);
  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierListItem[]>([]);
  const [assignees, setAssignees] = useState<AppUserCatalog[]>([]);
  const [modes, setModes] = useState<TransportMode[]>([]);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState<string>();
  const [clientId, setClientId] = useState<string>();
  const [supplierId, setSupplierId] = useState<string>();
  const [assignedUserId, setAssignedUserId] = useState<string>();
  const [transportModeId, setTransportModeId] = useState<string>();
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [formOpen, setFormOpen] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const loadCatalogs = async () => {
      try {
        const [statusData, modesData] = await Promise.all([
          catalogsApi.importRequestStatuses(),
          catalogsApi.transportModes()
        ]);
        setStatuses(statusData);
        setModes(modesData);

        if (!clientMode) {
          const [clientPage, supplierPage, userData] = await Promise.all([
            clientsApi.list({ page: 1, pageSize: 100 }),
            suppliersApi.list({ isActive: true, page: 1, pageSize: 100 }),
            catalogsApi.requestAssignees()
          ]);
          setClients(clientPage.items);
          setSuppliers(supplierPage.items);
          setAssignees(userData);
        }
      } catch {
        // The list can still work; individual catalog errors should not hide requests.
      }
    };
    loadCatalogs();
  }, [clientMode]);

  const statusLabel = useCallback(
    (code: string) => statuses.find((x) => x.code === code)?.name ?? code,
    [statuses]
  );

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await requestsApi.list({
        search: debouncedSearch || undefined,
        status,
        clientId: clientMode ? undefined : clientId ? Number(clientId) : undefined,
        supplierId: clientMode ? undefined : supplierId ? Number(supplierId) : undefined,
        assignedUserId: clientMode ? undefined : assignedUserId ? Number(assignedUserId) : undefined,
        transportModeId: transportModeId ? Number(transportModeId) : undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        page,
        pageSize
      });
      setItems(result.items);
      setTotal(result.total);
      setTotalPages(Math.max(result.totalPages, 1));
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudieron consultar las solicitudes.");
    } finally {
      setLoading(false);
    }
  }, [
    debouncedSearch,
    status,
    clientId,
    supplierId,
    assignedUserId,
    transportModeId,
    dateFrom,
    dateTo,
    page,
    pageSize,
    clientMode
  ]);

  useEffect(() => {
    load();
  }, [load]);

  const filtersActive = useMemo(
    () =>
      Boolean(
        debouncedSearch ||
          status ||
          clientId ||
          supplierId ||
          assignedUserId ||
          transportModeId ||
          dateFrom ||
          dateTo
      ),
    [
      debouncedSearch,
      status,
      clientId,
      supplierId,
      assignedUserId,
      transportModeId,
      dateFrom,
      dateTo
    ]
  );

  return (
    <>
      <PageHeader
        eyebrow={clientMode ? "Portal cliente" : "Comercial"}
        title={clientMode ? "Mis solicitudes" : "Solicitudes de importación"}
        description={
          clientMode
            ? "Solicitudes registradas para tu empresa y su estado de revisión."
            : "Ingreso, revisión, asignación y trazabilidad previa a la cotización."
        }
        actions={
          <PermissionGate permission={Permissions.RequestsCreate}>
            <Button onClick={() => setFormOpen(true)}>
              <Plus className="size-4" />
              Nueva solicitud
            </Button>
          </PermissionGate>
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
              placeholder="N° solicitud, cliente, proveedor o mercancía..."
            />
          </div>

          <SearchableSelect
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
            options={statuses.map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Todos los estados"
          />

          <SearchableSelect
            value={transportModeId}
            onChange={(value) => {
              setTransportModeId(value);
              setPage(1);
            }}
            options={modes.map((x) => ({
              value: String(x.transportModeId),
              label: x.name,
              description: x.code
            }))}
            placeholder="Todas las modalidades"
          />

          {!clientMode && (
            <>
              <SearchableSelect
                value={clientId}
                onChange={(value) => {
                  setClientId(value);
                  setPage(1);
                }}
                options={clients.map((x) => ({
                  value: String(x.clientId),
                  label: x.legalName,
                  description: x.taxId ?? undefined
                }))}
                placeholder="Todos los clientes"
                searchPlaceholder="Buscar cliente..."
              />

              <SearchableSelect
                value={supplierId}
                onChange={(value) => {
                  setSupplierId(value);
                  setPage(1);
                }}
                options={suppliers.map((x) => ({
                  value: String(x.supplierId),
                  label: x.legalName,
                  description: x.countryCode ?? undefined
                }))}
                placeholder="Todos los proveedores"
                searchPlaceholder="Buscar proveedor..."
              />

              <SearchableSelect
                value={assignedUserId}
                onChange={(value) => {
                  setAssignedUserId(value);
                  setPage(1);
                }}
                options={assignees.map((x) => ({
                  value: String(x.userId),
                  label: userName(x),
                  description: x.email ?? x.username
                }))}
                placeholder="Todos los responsables"
              />
            </>
          )}

          <div className="grid grid-cols-2 gap-2">
            <Input type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} />
            <Input type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} />
          </div>

          <Button
            variant="secondary"
            className="xl:col-span-1"
            onClick={() => {
              setSearch("");
              setStatus(undefined);
              setClientId(undefined);
              setSupplierId(undefined);
              setAssignedUserId(undefined);
              setTransportModeId(undefined);
              setDateFrom("");
              setDateTo("");
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
            {Array.from({ length: 7 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            title={filtersActive ? "Sin resultados" : "No hay solicitudes"}
            description={
              filtersActive
                ? "No existen solicitudes que coincidan con los filtros."
                : clientMode
                  ? "Tu empresa todavía no tiene solicitudes registradas."
                  : "Registra la primera solicitud para iniciar el flujo comercial."
            }
            action={
              !filtersActive ? (
                <PermissionGate permission={Permissions.RequestsCreate}>
                  <Button onClick={() => setFormOpen(true)}>
                    <Plus className="size-4" />
                    Nueva solicitud
                  </Button>
                </PermissionGate>
              ) : undefined
            }
          />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[1050px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {[
                      "Solicitud",
                      ...(clientMode ? [] : ["Cliente"]),
                      "Mercancía",
                      "Proveedor",
                      "Transporte",
                      "Responsable",
                      "Estado",
                      ""
                    ].map((h) => (
                      <th
                        key={h}
                        className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500"
                      >
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr
                      key={item.requestId}
                      className="cursor-pointer border-b border-slate-100 transition last:border-0 hover:bg-slate-50/70"
                      onClick={() => navigate(`/solicitudes/${item.requestId}`)}
                    >
                      <td className="px-4 py-4">
                        <div className="font-semibold text-slate-900">{item.requestNo}</div>
                        <div className="mt-1 text-xs text-slate-500">{formatDate(item.requestDate)}</div>
                      </td>
                      {!clientMode && (
                        <td className="px-4 py-4 text-sm font-medium text-slate-700">
                          {item.clientName}
                        </td>
                      )}
                      <td className="max-w-[300px] px-4 py-4">
                        <div className="line-clamp-2 text-sm text-slate-700">
                          {item.merchandiseDescription}
                        </div>
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-600">
                        {item.supplierName || "Sin asignar"}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-600">
                        {item.transportMode || "—"}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-600">
                        {item.assignedTo || "Sin asignar"}
                      </td>
                      <td className="px-4 py-4">
                        <Badge tone={tone(item.status)}>{statusLabel(item.status)}</Badge>
                      </td>
                      <td className="px-4 py-4 text-right">
                        <ChevronRight className="ml-auto size-4 text-slate-400" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {items.map((item) => (
                <button
                  key={item.requestId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() => navigate(`/solicitudes/${item.requestId}`)}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.requestNo}</div>
                      {!clientMode && <div className="mt-1 text-xs text-slate-500">{item.clientName}</div>}
                    </div>
                    <Badge tone={tone(item.status)}>{statusLabel(item.status)}</Badge>
                  </div>
                  <p className="mt-3 line-clamp-2 text-sm text-slate-600">
                    {item.merchandiseDescription}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                    <span>{item.transportMode || "Sin modalidad"}</span>
                    <span>{item.assignedTo || "Sin responsable"}</span>
                  </div>
                </button>
              ))}
            </div>

            <div className="flex flex-col gap-3 border-t border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="text-xs text-slate-500">
                {total} solicitud{total === 1 ? "" : "es"}
              </div>
              <div className="flex items-center gap-1">
                <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((x) => x - 1)}>Anterior</Button>
                <span className="px-3 text-sm font-semibold text-slate-700">{page} / {totalPages}</span>
                <Button variant="ghost" disabled={page >= totalPages} onClick={() => setPage((x) => x + 1)}>Siguiente</Button>
              </div>
            </div>
          </>
        )}
      </Card>

      <RequestFormDialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSaved={(saved: ImportRequestDetail) => {
          setFormOpen(false);
          navigate(`/solicitudes/${saved.requestId}`);
        }}
      />
    </>
  );
}
