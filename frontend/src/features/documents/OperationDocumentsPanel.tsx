import {
  CheckCircle2,
  FileText,
  Plus,
  TriangleAlert
} from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { documentsApi } from "./documents.api";
import type {
  DocumentRecord,
  OperationDocumentStatus
} from "./types";
import { DocumentUploadDialog } from "./DocumentUploadDialog";
import { formatDate } from "@/lib/format";

function tone(status: string) {
  if (status === "VALID") return "green" as const;
  if (["OBSERVED","EXPIRED","VOID"].includes(status)) return "red" as const;
  if (status === "PENDING_REVIEW") return "amber" as const;
  return "neutral" as const;
}

export function OperationDocumentsPanel({
  operationId,
  clientMode,
  canUpload,
  canManage
}: {
  operationId: number;
  clientMode: boolean;
  canUpload: boolean;
  canManage: boolean;
}) {
  const navigate = useNavigate();
  const [documents, setDocuments] = useState<DocumentRecord[]>([]);
  const [requirements, setRequirements] = useState<OperationDocumentStatus | null>(null);
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [preferredTypeId, setPreferredTypeId] = useState<number | null>(null);
  const [error, setError] = useState("");

  const load = async () => {
    try {
      const [docs, reqs, statusData] = await Promise.all([
        documentsApi.list({ operationId, page: 1, pageSize: 100 }),
        documentsApi.requirements(operationId),
        catalogsApi.documentStatuses()
      ]);
      setDocuments(docs.items);
      setRequirements(reqs);
      setStatuses(statusData);
      setError("");
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar el expediente documental.");
    }
  };

  useEffect(() => { void load(); }, [operationId]);

  const statusName = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;

  const openUpload = (documentTypeId?: number) => {
    setPreferredTypeId(documentTypeId ?? null);
    setUploadOpen(true);
  };

  return (
    <div className="space-y-4">
      {error && <InlineAlert message={error} />}

      <Card className="overflow-hidden">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold text-slate-900">Requisitos de la etapa actual</h2>
            <p className="mt-1 text-sm text-slate-500">
              El workflow usa documentos válidos, no simplemente archivos cargados.
            </p>
          </div>
          {canUpload && (
            <Button onClick={() => openUpload()}>
              <Plus className="size-4" />
              Cargar documento
            </Button>
          )}
        </div>

        {requirements && (
          <div className="grid gap-3 p-5 md:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-xl bg-slate-50 p-4">
              <div className="text-xs text-slate-400">Obligatorios</div>
              <div className="mt-1 text-2xl font-bold text-slate-900">{requirements.requiredCount}</div>
            </div>
            <div className="rounded-xl bg-emerald-50 p-4">
              <div className="text-xs text-emerald-700">Válidos</div>
              <div className="mt-1 text-2xl font-bold text-emerald-900">{requirements.validCount}</div>
            </div>
            <div className="rounded-xl bg-amber-50 p-4">
              <div className="text-xs text-amber-700">Faltantes</div>
              <div className="mt-1 text-2xl font-bold text-amber-900">{requirements.missingCount}</div>
            </div>
            <div className="rounded-xl bg-red-50 p-4">
              <div className="text-xs text-red-700">Observados</div>
              <div className="mt-1 text-2xl font-bold text-red-900">{requirements.observedCount}</div>
            </div>
          </div>
        )}

        {requirements?.requirements.length ? (
          <div className="divide-y divide-slate-100 border-t border-slate-100">
            {requirements.requirements.map((req) => (
              <div key={req.documentTypeId} className="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-3">
                  <div className={`grid size-9 place-items-center rounded-lg ${req.isSatisfied ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
                    {req.isSatisfied ? <CheckCircle2 className="size-4" /> : <TriangleAlert className="size-4" />}
                  </div>
                  <div>
                    <div className="font-semibold text-slate-900">{req.documentTypeName}</div>
                    <div className="mt-1 text-xs text-slate-500">
                      {req.documentTypeCode} · {req.isMandatory ? "Obligatorio" : "Opcional"}
                    </div>
                  </div>
                </div>
                <div className="flex gap-2">
                  <Badge tone={req.isSatisfied ? "green" : "amber"}>
                    {req.isSatisfied ? "Cumplido" : "Pendiente"}
                  </Badge>
                  {!req.isSatisfied && canUpload && (
                    <Button variant="secondary" onClick={() => openUpload(req.documentTypeId)}>
                      Cargar
                    </Button>
                  )}
                  {req.validDocumentId && (
                    <Button variant="ghost" onClick={() => navigate(`/documentos/${req.validDocumentId}`)}>
                      Ver
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="border-t border-slate-100 px-5 py-6 text-sm text-slate-500">
            La etapa actual no tiene requisitos documentales configurados.
          </div>
        )}
      </Card>

      <Card className="overflow-hidden">
        <div className="border-b border-slate-100 px-5 py-4">
          <h2 className="font-semibold text-slate-900">Expediente documental</h2>
          <p className="mt-1 text-sm text-slate-500">Todas las versiones visibles relacionadas con esta operación.</p>
        </div>

        {documents.length === 0 ? (
          <EmptyState title="Sin documentos" description="Todavía no existen documentos en la operación." />
        ) : (
          <div className="divide-y divide-slate-100">
            {documents.map((doc) => (
              <button
                key={doc.documentId}
                type="button"
                className="flex w-full flex-col gap-3 px-5 py-4 text-left transition hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"
                onClick={() => navigate(`/documentos/${doc.documentId}`)}
              >
                <div className="flex min-w-0 gap-3">
                  <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-slate-100 text-slate-600">
                    <FileText className="size-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="font-semibold text-slate-900">{doc.documentTypeName}</div>
                    <div className="mt-1 truncate text-xs text-slate-500">
                      {doc.originalFileName} · V{doc.versionNo} · {formatDate(doc.uploadedAt)}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Badge tone={tone(doc.documentStatus)}>{statusName(doc.documentStatus)}</Badge>
                  {!clientMode && <Badge tone="neutral">{doc.visibility}</Badge>}
                </div>
              </button>
            ))}
          </div>
        )}
      </Card>

      <DocumentUploadDialog
        open={uploadOpen}
        operationId={operationId}
        preferredDocumentTypeId={preferredTypeId}
        clientMode={clientMode}
        onClose={() => {
          setUploadOpen(false);
          setPreferredTypeId(null);
        }}
        onUploaded={() => {
          setUploadOpen(false);
          setPreferredTypeId(null);
          void load();
        }}
      />
    </div>
  );
}
