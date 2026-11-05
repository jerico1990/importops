import {
  ArrowLeft,
  CheckCircle2,
  Download,
  FileWarning,
  RefreshCw,
  RotateCcw,
  XCircle
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { documentsApi } from "@/features/documents/documents.api";
import type { DocumentObservation, DocumentRecord } from "@/features/documents/types";
import { DocumentReplaceDialog } from "@/features/documents/DocumentReplaceDialog";
import { DocumentActionDialog } from "@/features/documents/DocumentActionDialog";
import { formatDate } from "@/lib/format";

type ActionKey = "VALIDATE" | "OBSERVE" | "VOID" | null;

function tone(status: string) {
  if (status === "VALID") return "green" as const;
  if (["OBSERVED","EXPIRED","VOID"].includes(status)) return "red" as const;
  if (status === "PENDING_REVIEW") return "amber" as const;
  return "neutral" as const;
}

export function DocumentDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const documentId = Number(id);
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";
  const canUpload = hasPermission(Permissions.DocumentsUpload);
  const canManage = hasPermission(Permissions.DocumentsManage);

  const [document, setDocument] = useState<DocumentRecord | null>(null);
  const [observations, setObservations] = useState<DocumentObservation[]>([]);
  const [statuses, setStatuses] = useState<CodeNameCatalog[]>([]);
  const [observationStatuses, setObservationStatuses] = useState<CodeNameCatalog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [replaceOpen, setReplaceOpen] = useState(false);
  const [action, setAction] = useState<ActionKey>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [doc, obs, statusData, observationStatusData] = await Promise.all([
        documentsApi.get(documentId),
        documentsApi.observations(documentId),
        catalogsApi.documentStatuses(),
        catalogsApi.documentObservationStatuses()
      ]);
      setDocument(doc);
      setObservations(obs);
      setStatuses(statusData);
      setObservationStatuses(observationStatusData);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar el documento.");
    } finally {
      setLoading(false);
    }
  }, [documentId]);

  useEffect(() => { void load(); }, [load]);

  if (loading) return <div className="space-y-4"><Skeleton className="h-20" /><Skeleton className="h-96" /></div>;
  if (error && !document) return <InlineAlert message={error} />;
  if (!document) return null;

  const statusName = (code: string) =>
    statuses.find((x) => x.code === code)?.name ?? code;
  const observationStatusName = (code: string) =>
    observationStatuses.find((x) => x.code === code)?.name ?? code;

  const canReplace = canUpload && !["VOID","REPLACED"].includes(document.documentStatus);

  const performAction = async (comment: string) => {
    if (action === "VALIDATE") {
      await documentsApi.validate(document.documentId, document.rowVersion, comment || undefined);
    } else if (action === "OBSERVE") {
      await documentsApi.observe(document.documentId, document.rowVersion, comment);
    } else if (action === "VOID") {
      await documentsApi.void(document.documentId, document.rowVersion, comment);
    }
    setAction(null);
    await load();
  };

  const resolve = async (obs: DocumentObservation) => {
    const note = window.prompt("Nota de resolución:")?.trim();
    if (!note) return;
    try {
      await documentsApi.resolveObservation(
        document.documentId,
        obs.documentObservationId,
        obs.rowVersion,
        note
      );
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo resolver la observación.");
    }
  };

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/documentos")}
      >
        <ArrowLeft className="size-4" />
        Volver a documentos
      </button>

      <PageHeader
        eyebrow="Documento"
        title={document.documentTypeName}
        description={`${document.originalFileName} · V${document.versionNo}`}
        actions={
          <>
            <Button variant="secondary" onClick={() => void documentsApi.download(document.documentId, document.originalFileName)}>
              <Download className="size-4" />
              Descargar
            </Button>

            {canReplace && (
              <Button variant="secondary" onClick={() => setReplaceOpen(true)}>
                <RotateCcw className="size-4" />
                Reemplazar
              </Button>
            )}

            {!clientMode && canManage && !["VALID","VOID","REPLACED"].includes(document.documentStatus) && (
              <Button onClick={() => setAction("VALIDATE")}>
                <CheckCircle2 className="size-4" />
                Validar
              </Button>
            )}

            {!clientMode && canManage && !["VOID","REPLACED"].includes(document.documentStatus) && (
              <Button variant="secondary" onClick={() => setAction("OBSERVE")}>
                <FileWarning className="size-4" />
                Observar
              </Button>
            )}

            {!clientMode && canManage && !["VOID","REPLACED"].includes(document.documentStatus) && (
              <Button variant="ghost" onClick={() => setAction("VOID")}>
                <XCircle className="size-4" />
                Anular
              </Button>
            )}
          </>
        }
      />

      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      <div className="mb-5 flex flex-wrap gap-2">
        <Badge tone={tone(document.documentStatus)}>{statusName(document.documentStatus)}</Badge>
        <Badge tone="neutral">Visibilidad: {document.visibility}</Badge>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
        <Card className="p-5">
          <h2 className="font-semibold text-slate-900">Metadatos</h2>
          <div className="mt-5 grid gap-5 sm:grid-cols-2">
            {[
              ["Código", document.documentTypeCode],
              ["Archivo", document.originalFileName],
              ["Versión", `V${document.versionNo}`],
              ["Fecha documento", formatDate(document.documentDate)],
              ["Vencimiento", formatDate(document.expirationDate)],
              ["Carga", formatDate(document.uploadedAt)],
              ["Tamaño", document.fileSizeBytes != null ? `${(document.fileSizeBytes / 1024 / 1024).toFixed(2)} MB` : "—"],
              ["SHA-256", document.sha256Hash || "—"]
            ].map(([k, v]) => (
              <div key={k}>
                <div className="text-xs font-medium text-slate-400">{k}</div>
                <div className="mt-1 break-all text-sm font-medium text-slate-800">{v}</div>
              </div>
            ))}
          </div>

          {document.notes && (
            <div className="mt-6 border-t border-slate-100 pt-5">
              <div className="text-xs font-medium text-slate-400">Notas</div>
              <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">{document.notes}</p>
            </div>
          )}

          {document.operationId && (
            <Link to={`/operaciones/${document.operationId}`} className="mt-6 inline-flex font-semibold text-blue-700">
              Ver operación relacionada
            </Link>
          )}
        </Card>

        <Card className="overflow-hidden">
          <div className="border-b border-slate-100 px-5 py-4">
            <h2 className="font-semibold text-slate-900">Observaciones</h2>
          </div>

          {observations.length === 0 ? (
            <div className="p-8 text-center text-sm text-slate-500">Sin observaciones.</div>
          ) : (
            <div className="divide-y divide-slate-100">
              {observations.map((obs) => (
                <div key={obs.documentObservationId} className="px-5 py-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <Badge tone={obs.status === "OPEN" ? "red" : "green"}>
                          {observationStatusName(obs.status)}
                        </Badge>
                        <span className="text-xs text-slate-400">{formatDate(obs.observedAt)}</span>
                      </div>
                      <p className="mt-2 text-sm leading-6 text-slate-700">{obs.description}</p>
                      {obs.resolutionNote && (
                        <div className="mt-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800">
                          {obs.resolutionNote}
                        </div>
                      )}
                    </div>
                    {!clientMode && canManage && obs.status === "OPEN" && (
                      <Button variant="secondary" onClick={() => void resolve(obs)}>
                        <RefreshCw className="size-4" />
                        Resolver
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      <DocumentReplaceDialog
        open={replaceOpen}
        document={document}
        onClose={() => setReplaceOpen(false)}
        onReplaced={(doc) => {
          setReplaceOpen(false);
          navigate(`/documentos/${doc.documentId}`);
        }}
      />

      <DocumentActionDialog
        open={action !== null}
        title={
          action === "VALIDATE"
            ? "Validar documento"
            : action === "OBSERVE"
              ? "Observar documento"
              : "Anular documento"
        }
        description={
          action === "VALIDATE"
            ? "La versión quedará marcada como válida y puede satisfacer un requisito de etapa."
            : undefined
        }
        required={action === "OBSERVE" || action === "VOID"}
        confirmLabel={
          action === "VALIDATE"
            ? "Validar"
            : action === "OBSERVE"
              ? "Registrar observación"
              : "Anular documento"
        }
        danger={action === "VOID"}
        onClose={() => setAction(null)}
        onConfirm={performAction}
      />
    </>
  );
}
