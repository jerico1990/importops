import { ChevronRight, FileText, Search, SlidersHorizontal } from "lucide-react";
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
import type { CodeNameCatalog, DocumentType } from "@/features/catalogs/types";
import { documentsApi } from "@/features/documents/documents.api";
import type { DocumentRecord } from "@/features/documents/types";
import type { PagedResult } from "@/types/common";
import { formatDate } from "@/lib/format";

function tone(status: string) {
  if (status === "VALID") return "green" as const;
  if (status === "OBSERVED" || status === "EXPIRED" || status === "VOID") return "red" as const;
  if (status === "PENDING_REVIEW") return "amber" as const;
  if (status === "REPLACED") return "neutral" as const;
  return "neutral" as const;
}

export function DocumentsPage() {
  const navigate = useNavigate();
  const [result, setResult] = useState<PagedResult<DocumentRecord>>({
    items: [], page: 1, pageSize: 20, total: 0, totalPages: 0
  });
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [status, setStatus] = useState<string>();
  const [documentTypeId, setDocumentTypeId] = useState<string>();
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
      catalogsApi.documentStatuses(),
      catalogsApi.documentTypes()
    ]).then(([a, b]) => {
      setStatuses(a);
      setTypes(b);
    }).catch(() => {});
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setResult(await documentsApi.list({
        search: applied || undefined,
        status,
        documentTypeId: documentTypeId ? Number(documentTypeId) : undefined,
        page,
        pageSize: 20
      }));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudieron cargar los documentos.");
    } finally {
      setLoading(false);
    }
  }, [applied, status, documentTypeId, page]);

  useEffect(() => { void load(); }, [load]);

  const statusName = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  return (
    <>
      <PageHeader
        eyebrow="Expediente"
        title="Documentos"
        description="Versiones, revisión, observaciones y requisitos documentales asociados a solicitudes y operaciones."
      />

      <Card className="overflow-visible">
        <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="relative md:col-span-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Archivo o notas..."
            />
          </div>

          <SearchableSelect
            value={status}
            onChange={(v) => { setStatus(v); setPage(1); }}
            options={statuses.map((x) => ({ value: x.code, label: x.name }))}
            placeholder="Todos los estados"
          />

          <SearchableSelect
            value={documentTypeId}
            onChange={(v) => { setDocumentTypeId(v); setPage(1); }}
            options={types.map((x) => ({
              value: String(x.documentTypeId),
              label: x.name,
              description: `${x.category} · ${x.code}`
            }))}
            placeholder="Todos los tipos"
          />

          <Button
            variant="secondary"
            onClick={() => {
              setSearch("");
              setApplied("");
              setStatus(undefined);
              setDocumentTypeId(undefined);
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
          <EmptyState title="No hay documentos" description="No existen documentos visibles para los filtros seleccionados." />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[1000px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {["Documento","Archivo","Versión","Contexto","Fecha","Vencimiento","Estado",""].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((doc) => (
                    <tr
                      key={doc.documentId}
                      className="cursor-pointer border-b border-slate-100 transition hover:bg-slate-50/70"
                      onClick={() => navigate(`/documentos/${doc.documentId}`)}
                    >
                      <td className="px-4 py-4">
                        <div className="flex items-center gap-2">
                          <FileText className="size-4 text-slate-400" />
                          <span className="font-semibold text-slate-900">{doc.documentTypeName}</span>
                        </div>
                      </td>
                      <td className="max-w-[280px] px-4 py-4 text-sm text-slate-600">
                        <div className="truncate">{doc.originalFileName}</div>
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700">V{doc.versionNo}</td>
                      <td className="px-4 py-4 text-xs text-slate-500">
                        {doc.operationId ? `Operación #${doc.operationId}` :
                         doc.quotationId ? `Cotización #${doc.quotationId}` :
                         doc.requestId ? `Solicitud #${doc.requestId}` : "—"}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-500">{formatDate(doc.documentDate)}</td>
                      <td className="px-4 py-4 text-sm text-slate-500">{formatDate(doc.expirationDate)}</td>
                      <td className="px-4 py-4"><Badge tone={tone(doc.documentStatus)}>{statusName(doc.documentStatus)}</Badge></td>
                      <td className="px-4 py-4"><ChevronRight className="ml-auto size-4 text-slate-400" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {result.items.map((doc) => (
                <button
                  key={doc.documentId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() => navigate(`/documentos/${doc.documentId}`)}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{doc.documentTypeName}</div>
                      <div className="mt-1 text-xs text-slate-500">{doc.originalFileName} · V{doc.versionNo}</div>
                    </div>
                    <Badge tone={tone(doc.documentStatus)}>{statusName(doc.documentStatus)}</Badge>
                  </div>
                </button>
              ))}
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 px-4 py-3">
              <span className="text-xs text-slate-500">{result.total} documento{result.total === 1 ? "" : "s"}</span>
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
