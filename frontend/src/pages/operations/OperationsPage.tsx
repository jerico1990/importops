import { ChevronRight, Search, SlidersHorizontal } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { Skeleton } from "@/components/ui/Skeleton";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { EmptyState } from "@/components/feedback/EmptyState";
import { useAuth } from "@/features/auth/AuthContext";
import { clientsApi } from "@/features/clients/clients.api";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { AppUserCatalog, CodeNameCatalog, WorkflowStage } from "@/features/catalogs/types";
import type { ClientListItem } from "@/features/clients/types";
import { operationsApi } from "@/features/operations/operations.api";
import type { OperationListItem } from "@/features/operations/types";
import { HttpError } from "@/lib/api/http";
import { formatDate } from "@/lib/format";
import type { PagedResult } from "@/types/common";

function statusTone(status: string) {
  if (["CLOSED", "DELIVERED", "COMPLETE"].includes(status)) return "green" as const;
  if (["CANCELLED", "OBSERVED"].includes(status)) return "red" as const;
  if (["ON_HOLD", "PENDING"].includes(status)) return "amber" as const;
  if (status === "IN_PROGRESS") return "blue" as const;
  return "neutral" as const;
}

function userName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function OperationsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [result, setResult] = useState<PagedResult<OperationListItem>>({
    items: [], page: 1, pageSize: 20, total: 0, totalPages: 0
  });
  const [opStatuses, setOpStatuses] = useState<CodeNameCatalog[]>([]);
  const [docStatuses, setDocStatuses] = useState<CodeNameCatalog[]>([]);
  const [finStatuses, setFinStatuses] = useState<CodeNameCatalog[]>([]);
  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [responsibles, setResponsibles] = useState<AppUserCatalog[]>([]);
  const [stages, setStages] = useState<WorkflowStage[]>([]);

  const [search, setSearch] = useState("");
  const [searchApplied, setSearchApplied] = useState("");
  const [operationalStatus, setOperationalStatus] = useState<string>();
  const [documentaryStatus, setDocumentaryStatus] = useState<string>();
  const [financialStatus, setFinancialStatus] = useState<string>();
  const [clientId, setClientId] = useState<string>();
  const [responsibleUserId, setResponsibleUserId] = useState<string>();
  const [stageId, setStageId] = useState<string>();
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearchApplied(search.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    Promise.all([
      catalogsApi.operationStatuses(),
      catalogsApi.documentaryStatuses(),
      catalogsApi.workflowStages()
    ]).then(([a, b, c]) => {
      setOpStatuses(a);
      setDocStatuses(b);
      setStages(c);
    }).catch(() => {});

    if (!clientMode) {
      Promise.all([
        catalogsApi.financialStatuses(),
        clientsApi.list({ page: 1, pageSize: 100 }),
        catalogsApi.operationResponsibles()
      ]).then(([f, c, u]) => {
        setFinStatuses(f);
        setClients(c.items);
        setResponsibles(u);
      }).catch(() => {});
    }
  }, [clientMode]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const data = await operationsApi.list({
        search: searchApplied || undefined,
        operationalStatus,
        documentaryStatus,
        financialStatus: clientMode ? undefined : financialStatus,
        clientId: clientMode ? undefined : clientId ? Number(clientId) : undefined,
        responsibleUserId: clientMode ? undefined : responsibleUserId ? Number(responsibleUserId) : undefined,
        stageId: stageId ? Number(stageId) : undefined,
        page,
        pageSize: 20
      });
      setResult(data);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudieron cargar las operaciones.");
    } finally {
      setLoading(false);
    }
  }, [searchApplied, operationalStatus, documentaryStatus, financialStatus, clientId, responsibleUserId, stageId, page, clientMode]);

  useEffect(() => { void load(); }, [load]);

  const label = (items: CodeNameCatalog[], code?: string | null) =>
    code ? items.find((x) => x.code === code)?.name ?? code : "—";

  return (
    <>
      <PageHeader
        eyebrow={clientMode ? "Portal cliente" : "Operación"}
        title={clientMode ? "Mis importaciones" : "Operaciones de importación"}
        description={
          clientMode
            ? "Seguimiento de los expedientes de tu empresa."
            : "Expedientes creados desde cotizaciones aprobadas."
        }
      />

      <Card className="overflow-visible">
        <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="relative md:col-span-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Operación, mercancía, cotización o cliente..."
            />
          </div>

          <SearchableSelect
            value={operationalStatus}
            onChange={(v) => { setOperationalStatus(v); setPage(1); }}
            options={opStatuses.map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Estado operativo"
          />

          <SearchableSelect
            value={documentaryStatus}
            onChange={(v) => { setDocumentaryStatus(v); setPage(1); }}
            options={docStatuses.map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Estado documental"
          />

          <SearchableSelect
            value={stageId}
            onChange={(v) => { setStageId(v); setPage(1); }}
            options={stages.map((x) => ({
              value: String(x.workflowStageId),
              label: x.name,
              description: x.code
            }))}
            placeholder="Todas las etapas"
          />

          {!clientMode && (
            <>
              <SearchableSelect
                value={financialStatus}
                onChange={(v) => { setFinancialStatus(v); setPage(1); }}
                options={finStatuses.map((x) => ({ value: x.code, label: x.name }))}
                placeholder="Estado financiero"
              />
              <SearchableSelect
                value={clientId}
                onChange={(v) => { setClientId(v); setPage(1); }}
                options={clients.map((x) => ({
                  value: String(x.clientId),
                  label: x.legalName,
                  description: x.taxId ?? undefined
                }))}
                placeholder="Todos los clientes"
              />
              <SearchableSelect
                value={responsibleUserId}
                onChange={(v) => { setResponsibleUserId(v); setPage(1); }}
                options={responsibles.map((x) => ({
                  value: String(x.userId),
                  label: userName(x),
                  description: x.email ?? x.username
                }))}
                placeholder="Todos los responsables"
              />
            </>
          )}

          <Button
            variant="secondary"
            onClick={() => {
              setSearch("");
              setSearchApplied("");
              setOperationalStatus(undefined);
              setDocumentaryStatus(undefined);
              setFinancialStatus(undefined);
              setClientId(undefined);
              setResponsibleUserId(undefined);
              setStageId(undefined);
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
            title="No hay operaciones"
            description={
              clientMode
                ? "Tu empresa todavía no tiene operaciones."
                : "Las operaciones se crean desde cotizaciones aprobadas."
            }
          />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[1050px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {[
                      "Operación",
                      ...(clientMode ? [] : ["Cliente"]),
                      "Etapa",
                      "Estado",
                      "Documentos",
                      ...(clientMode ? [] : ["Finanzas"]),
                      "Responsable",
                      "Apertura",
                      ""
                    ].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((item) => (
                    <tr
                      key={item.operationId}
                      className="cursor-pointer border-b border-slate-100 transition last:border-0 hover:bg-slate-50/70"
                      onClick={() => navigate(`/operaciones/${item.operationId}`)}
                    >
                      <td className="px-4 py-4 font-semibold text-slate-900">{item.operationNo}</td>
                      {!clientMode && <td className="px-4 py-4 text-sm text-slate-700">{item.clientName}</td>}
                      <td className="px-4 py-4">
                        <div className="text-sm font-medium text-slate-800">{item.stageName}</div>
                        <div className="mt-1 text-xs text-slate-400">{item.stageCode}</div>
                      </td>
                      <td className="px-4 py-4">
                        <Badge tone={statusTone(item.operationalStatus)}>
                          {label(opStatuses, item.operationalStatus)}
                        </Badge>
                      </td>
                      <td className="px-4 py-4">
                        <Badge tone={statusTone(item.documentaryStatus)}>
                          {label(docStatuses, item.documentaryStatus)}
                        </Badge>
                      </td>
                      {!clientMode && (
                        <td className="px-4 py-4 text-sm text-slate-600">
                          {label(finStatuses, item.financialStatus)}
                        </td>
                      )}
                      <td className="px-4 py-4 text-sm text-slate-600">{item.responsibleName}</td>
                      <td className="px-4 py-4 text-sm text-slate-500">{formatDate(item.openedAt)}</td>
                      <td className="px-4 py-4"><ChevronRight className="ml-auto size-4 text-slate-400" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {result.items.map((item) => (
                <button
                  key={item.operationId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() => navigate(`/operaciones/${item.operationId}`)}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.operationNo}</div>
                      {!clientMode && <div className="mt-1 text-xs text-slate-500">{item.clientName}</div>}
                    </div>
                    <Badge tone={statusTone(item.operationalStatus)}>
                      {label(opStatuses, item.operationalStatus)}
                    </Badge>
                  </div>
                  <div className="mt-3 text-sm text-slate-700">{item.stageName}</div>
                  <div className="mt-2 text-xs text-slate-500">{item.responsibleName}</div>
                </button>
              ))}
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 px-4 py-3">
              <div className="text-xs text-slate-500">{result.total} operación{result.total === 1 ? "" : "es"}</div>
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
