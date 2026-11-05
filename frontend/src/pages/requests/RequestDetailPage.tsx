import {
  AlertTriangle,
  ArrowLeft,
  CheckCircle2,
  ClipboardList,
  Download,
  FileText,
  Package,
  Pencil,
  Plus,
  ReceiptText,
  Send,
  UserRound,
  XCircle
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";
import {
  Link,
  useNavigate,
  useParams
} from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs } from "@/components/ui/Tabs";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { PermissionGate } from "@/features/auth/PermissionGate";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { formatDate, formatMoney } from "@/lib/format";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { ImportRequestStatusCatalog } from "@/features/catalogs/types";
import { requestsApi } from "@/features/requests/requests.api";
import type {
  ImportRequestDetail,
  QuotationSummary,
  RequestDocument
} from "@/features/requests/types";
import { RequestFormDialog } from "@/features/requests/RequestFormDialog";
import { RequestActionDialog } from "@/features/requests/RequestActionDialog";
import { AssignRequestDialog } from "@/features/requests/AssignRequestDialog";
import { RequestDocumentUploadDialog } from "@/features/requests/RequestDocumentUploadDialog";
import { QuotationHeaderDialog } from "@/features/quotations/QuotationHeaderDialog";
import { HttpError } from "@/lib/api/http";

type TabKey = "SUMMARY" | "DOCUMENTS" | "TIMELINE" | "QUOTATIONS";
type ActionKey = "SUBMIT" | "OBSERVE" | "REJECT" | "CANCEL" | null;

function tone(status: string) {
  if (status === "APPROVED") return "green" as const;
  if (status === "REJECTED" || status === "CANCELLED") return "red" as const;
  if (status === "OBSERVED") return "amber" as const;
  if (status === "QUOTED") return "violet" as const;
  if (status === "UNDER_REVIEW") return "blue" as const;
  return "neutral" as const;
}

function docTone(status: string) {
  if (status === "VALID") return "green" as const;
  if (status === "OBSERVED" || status === "EXPIRED") return "red" as const;
  if (status === "PENDING_REVIEW") return "amber" as const;
  return "neutral" as const;
}

export function RequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const requestId = Number(id);
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [request, setRequest] = useState<ImportRequestDetail | null>(null);
  const [statuses, setStatuses] = useState<ImportRequestStatusCatalog[]>([]);
  const [documents, setDocuments] = useState<RequestDocument[]>([]);
  const [quotations, setQuotations] = useState<QuotationSummary[]>([]);
  const [tab, setTab] = useState<TabKey>("SUMMARY");
  const [loading, setLoading] = useState(true);
  const [tabLoading, setTabLoading] = useState(false);
  const [error, setError] = useState("");
  const [editOpen, setEditOpen] = useState(false);
  const [action, setAction] = useState<ActionKey>(null);
  const [assignOpen, setAssignOpen] = useState(false);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [quoteCreateOpen, setQuoteCreateOpen] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [detail, statusData] = await Promise.all([
        requestsApi.get(requestId),
        catalogsApi.importRequestStatuses()
      ]);
      setRequest(detail);
      setStatuses(statusData);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo consultar la solicitud.");
    } finally {
      setLoading(false);
    }
  }, [requestId]);

  useEffect(() => {
    if (Number.isFinite(requestId) && requestId > 0) load();
  }, [load, requestId]);

  useEffect(() => {
    if (!request) return;

    if (tab === "DOCUMENTS") {
      setTabLoading(true);
      requestsApi.documents(request.requestId)
        .then((x) => setDocuments(x.items))
        .catch(() => setDocuments([]))
        .finally(() => setTabLoading(false));
    }

    if (tab === "QUOTATIONS") {
      setTabLoading(true);
      requestsApi.quotations(request.requestId)
        .then(setQuotations)
        .catch(() => setQuotations([]))
        .finally(() => setTabLoading(false));
    }
  }, [tab, request?.requestId]);

  if (loading) {
    return <div className="space-y-4"><Skeleton className="h-20" /><Skeleton className="h-96" /></div>;
  }

  if (error && !request) return <InlineAlert message={error} />;
  if (!request) return null;

  const statusLabel = statuses.find((x) => x.code === request.status)?.name ?? request.status;
  const editable = request.status === "NEW" || request.status === "OBSERVED";
  const canCreate = hasPermission(Permissions.RequestsCreate);
  const canManage = hasPermission(Permissions.RequestsManage);

  const refreshDocs = async () => {
    const result = await requestsApi.documents(request.requestId);
    setDocuments(result.items);
  };

  const performAction = async (comment: string) => {
    if (!action) return;

    const updated =
      action === "SUBMIT"
        ? await requestsApi.submitReview(request.requestId, comment)
        : action === "OBSERVE"
          ? await requestsApi.observe(request.requestId, comment)
          : action === "REJECT"
            ? await requestsApi.reject(request.requestId, comment)
            : await requestsApi.cancel(request.requestId, comment);

    setRequest(updated);
    setAction(null);
  };

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/solicitudes")}
      >
        <ArrowLeft className="size-4" />
        Volver a solicitudes
      </button>

      <PageHeader
        eyebrow={clientMode ? "Solicitud de mi empresa" : "Solicitud de importación"}
        title={request.requestNo}
        description={`${request.clientName} · ${formatDate(request.requestDate)}`}
        actions={
          <>
            {editable && canCreate && (
              <Button variant="secondary" onClick={() => setEditOpen(true)}>
                <Pencil className="size-4" />
                Editar
              </Button>
            )}

            {(request.status === "NEW" || request.status === "OBSERVED") && canCreate && (
              <Button onClick={() => setAction("SUBMIT")}>
                <Send className="size-4" />
                Enviar a revisión
              </Button>
            )}

            {!clientMode && canManage && request.status === "UNDER_REVIEW" && (
              <>
                <Button onClick={() => setQuoteCreateOpen(true)}>
                  <ReceiptText className="size-4" />
                  Crear cotización
                </Button>
                <Button variant="secondary" onClick={() => setAssignOpen(true)}>
                  <UserRound className="size-4" />
                  Asignar
                </Button>
                <Button variant="secondary" onClick={() => setAction("OBSERVE")}>
                  <AlertTriangle className="size-4" />
                  Observar
                </Button>
                <Button variant="danger" onClick={() => setAction("REJECT")}>
                  <XCircle className="size-4" />
                  Rechazar
                </Button>
              </>
            )}

            {!clientMode &&
              canManage &&
              ["NEW", "UNDER_REVIEW", "OBSERVED"].includes(request.status) && (
                <Button variant="ghost" onClick={() => setAction("CANCEL")}>
                  Cancelar solicitud
                </Button>
              )}
          </>
        }
      />

      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      <div className="mb-5 flex flex-wrap gap-2">
        <Badge tone={tone(request.status)}>{statusLabel}</Badge>
        {request.transportMode && <Badge tone="blue">{request.transportMode}</Badge>}
        {request.assignedTo && <Badge tone="neutral">Responsable: {request.assignedTo}</Badge>}
      </div>

      {request.status === "OBSERVED" && (
        <div className="mb-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-4">
          <div className="flex gap-3">
            <AlertTriangle className="mt-0.5 size-5 shrink-0 text-amber-700" />
            <div>
              <div className="font-semibold text-amber-900">Solicitud observada</div>
              <div className="mt-1 text-sm leading-6 text-amber-800">
                Revisa la última observación en la pestaña Timeline, corrige la información y vuelve a enviar la solicitud a revisión.
              </div>
            </div>
          </div>
        </div>
      )}

      <Tabs
        value={tab}
        onChange={(value) => setTab(value as TabKey)}
        items={[
          { value: "SUMMARY", label: "Resumen" },
          { value: "DOCUMENTS", label: "Documentos", count: documents.length || undefined },
          { value: "TIMELINE", label: "Timeline", count: request.history.length },
          { value: "QUOTATIONS", label: "Cotizaciones", count: quotations.length || undefined }
        ]}
      />

      <div className="mt-4">
        {tab === "SUMMARY" && (
          <div className="grid gap-4 xl:grid-cols-[1.45fr_0.75fr]">
            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">Datos de la solicitud</h2>
              <div className="mt-5 grid gap-x-6 gap-y-5 sm:grid-cols-2 xl:grid-cols-3">
                {[
                  ["Cliente", request.clientName],
                  ["Contacto", request.clientContactName || "—"],
                  ["Proveedor", request.supplierName || "Sin asignar"],
                  ["Solicitado por", request.requestedBy],
                  ["Responsable", request.assignedTo || "Sin asignar"],
                  ["Incoterm", request.incotermCode || "—"],
                  ["Origen", [request.originCountry, request.originPort].filter(Boolean).join(" · ") || "—"],
                  ["Destino", [request.destinationCountry, request.destinationPort].filter(Boolean).join(" · ") || "—"],
                  ["Fecha deseada", formatDate(request.desiredServiceDate)]
                ].map(([label, value]) => (
                  <div key={label}>
                    <div className="text-xs font-medium text-slate-400">{label}</div>
                    <div className="mt-1 text-sm font-medium text-slate-800">{value}</div>
                  </div>
                ))}
              </div>

              <div className="mt-6 border-t border-slate-100 pt-5">
                <div className="text-xs font-medium text-slate-400">Mercancía</div>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">
                  {request.merchandiseDescription}
                </p>
              </div>

              <div className="mt-5 grid gap-4 rounded-xl bg-slate-50 p-4 sm:grid-cols-4">
                <div><div className="text-xs text-slate-400">Bultos</div><div className="mt-1 font-semibold text-slate-800">{request.packageCount ?? "—"}</div></div>
                <div><div className="text-xs text-slate-400">Peso bruto</div><div className="mt-1 font-semibold text-slate-800">{request.grossWeightKg != null ? `${request.grossWeightKg} kg` : "—"}</div></div>
                <div><div className="text-xs text-slate-400">Volumen</div><div className="mt-1 font-semibold text-slate-800">{request.volumeM3 != null ? `${request.volumeM3} m³` : "—"}</div></div>
                <div><div className="text-xs text-slate-400">Valor declarado</div><div className="mt-1 font-semibold text-slate-800">{request.declaredValue != null && request.declaredCurrencyCode ? formatMoney(request.declaredValue, request.declaredCurrencyCode) : "—"}</div></div>
              </div>
            </Card>

            <div className="space-y-4">
              <Card className="p-5">
                <h2 className="text-base font-semibold text-slate-900">Notas del cliente</h2>
                <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                  {request.customerNotes || "Sin notas registradas."}
                </p>
              </Card>

              {!clientMode && (
                <Card className="p-5">
                  <h2 className="text-base font-semibold text-slate-900">Notas internas</h2>
                  <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                    {request.internalNotes || "Sin notas internas."}
                  </p>
                </Card>
              )}

              {request.rejectionReason && (
                <Card className="border-red-200 bg-red-50 p-5">
                  <h2 className="text-base font-semibold text-red-900">Motivo</h2>
                  <p className="mt-2 text-sm leading-6 text-red-800">{request.rejectionReason}</p>
                </Card>
              )}
            </div>
          </div>
        )}

        {tab === "DOCUMENTS" && (
          <Card className="overflow-hidden">
            <div className="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="font-semibold text-slate-900">Documentación de la solicitud</h2>
                <p className="mt-1 text-sm text-slate-500">Archivos asociados a este requerimiento.</p>
              </div>
              <PermissionGate permission={Permissions.DocumentsUpload}>
                <Button onClick={() => setUploadOpen(true)}>
                  <Plus className="size-4" />
                  Cargar documento
                </Button>
              </PermissionGate>
            </div>

            {tabLoading ? (
              <div className="space-y-3 p-5"><Skeleton className="h-12" /><Skeleton className="h-12" /></div>
            ) : documents.length === 0 ? (
              <EmptyState
                title="Sin documentos"
                description="Todavía no existen archivos relacionados con esta solicitud."
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {documents.map((doc) => (
                  <div key={doc.documentId} className="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex min-w-0 gap-3">
                      <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-blue-50 text-blue-700">
                        <FileText className="size-5" />
                      </div>
                      <div className="min-w-0">
                        <div className="truncate font-semibold text-slate-900">{doc.documentTypeName}</div>
                        <div className="mt-1 truncate text-xs text-slate-500">
                          {doc.originalFileName} · V{doc.versionNo} · {formatDate(doc.uploadedAt)}
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge tone={docTone(doc.documentStatus)}>{doc.documentStatus}</Badge>
                      <Button
                        variant="ghost"
                        onClick={() => requestsApi.downloadDocument(doc.documentId, doc.originalFileName)}
                      >
                        <Download className="size-4" />
                        Descargar
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        )}

        {tab === "TIMELINE" && (
          <Card className="p-5">
            <h2 className="font-semibold text-slate-900">Historial de la solicitud</h2>
            <div className="mt-5 space-y-0">
              {request.history.map((event, index) => (
                <div key={event.requestStatusHistoryId} className="relative flex gap-4 pb-6 last:pb-0">
                  {index < request.history.length - 1 && (
                    <div className="absolute left-[17px] top-8 h-[calc(100%-16px)] w-px bg-slate-200" />
                  )}
                  <div className="relative z-10 grid size-9 shrink-0 place-items-center rounded-full bg-blue-50 text-blue-700">
                    {event.newStatus === "APPROVED" ? <CheckCircle2 className="size-4" /> : <ClipboardList className="size-4" />}
                  </div>
                  <div className="min-w-0 pt-0.5">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone={tone(event.newStatus)}>
                        {statuses.find((x) => x.code === event.newStatus)?.name ?? event.newStatus}
                      </Badge>
                      <span className="text-xs text-slate-400">{formatDate(event.changedAt)}</span>
                    </div>
                    {event.comment && <p className="mt-2 text-sm leading-6 text-slate-600">{event.comment}</p>}
                    <div className="mt-1 text-xs text-slate-400">Por {event.changedByName}</div>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {tab === "QUOTATIONS" && (
          <Card className="overflow-hidden">
            {tabLoading ? (
              <div className="space-y-3 p-5"><Skeleton className="h-12" /><Skeleton className="h-12" /></div>
            ) : quotations.length === 0 ? (
              <EmptyState
                title="Sin cotizaciones"
                description={
                  request.status === "UNDER_REVIEW"
                    ? "La solicitud todavía está siendo revisada. La cotización se generará desde el módulo de Cotizaciones."
                    : "No existen cotizaciones relacionadas con esta solicitud."
                }
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {quotations.map((quote) => (
                  <Link
                    key={quote.quotationId}
                    to={`/cotizaciones/${quote.quotationId}`}
                    className="flex flex-col gap-2 px-5 py-4 transition hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div>
                      <div className="font-semibold text-slate-900">
                        {quote.quotationNo} · V{quote.versionNo}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        Emisión {formatDate(quote.issueDate)}
                        {quote.validUntil ? ` · Válida hasta ${formatDate(quote.validUntil)}` : ""}
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge tone="blue">{quote.status}</Badge>
                      <span className="font-semibold text-slate-800">
                        {formatMoney(quote.totalAmount, quote.currencyCode)}
                      </span>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        )}
      </div>

      <RequestFormDialog
        open={editOpen}
        request={request}
        onClose={() => setEditOpen(false)}
        onSaved={(updated) => {
          setRequest(updated);
          setEditOpen(false);
        }}
      />

      <RequestActionDialog
        open={action !== null}
        title={
          action === "SUBMIT"
            ? "Enviar a revisión"
            : action === "OBSERVE"
              ? "Observar solicitud"
              : action === "REJECT"
                ? "Rechazar solicitud"
                : "Cancelar solicitud"
        }
        description={
          action === "SUBMIT"
            ? "La solicitud dejará de ser editable hasta que sea observada o continúe el proceso."
            : undefined
        }
        label={action === "SUBMIT" ? "Comentario" : "Motivo"}
        placeholder={
          action === "OBSERVE"
            ? "Indica exactamente qué información debe corregirse."
            : action === "REJECT"
              ? "Indica el motivo del rechazo."
              : undefined
        }
        required={action === "OBSERVE" || action === "REJECT" || action === "CANCEL"}
        confirmLabel={
          action === "SUBMIT"
            ? "Enviar"
            : action === "OBSERVE"
              ? "Registrar observación"
              : action === "REJECT"
                ? "Rechazar"
                : "Cancelar solicitud"
        }
        danger={action === "REJECT" || action === "CANCEL"}
        onClose={() => setAction(null)}
        onConfirm={performAction}
      />

      <AssignRequestDialog
        open={assignOpen}
        currentUserId={request.assignedUserId}
        onClose={() => setAssignOpen(false)}
        onConfirm={async (userId, comment) => {
          const updated = await requestsApi.assign(request.requestId, userId, comment);
          setRequest(updated);
          setAssignOpen(false);
        }}
      />

      <RequestDocumentUploadDialog
        open={uploadOpen}
        requestId={request.requestId}
        clientMode={clientMode}
        onClose={() => setUploadOpen(false)}
        onUploaded={async () => {
          setUploadOpen(false);
          await refreshDocs();
        }}
      />

      <QuotationHeaderDialog
        open={quoteCreateOpen}
        requestId={request.requestId}
        onClose={() => setQuoteCreateOpen(false)}
        onSaved={(quotation) => {
          setQuoteCreateOpen(false);
          navigate(`/cotizaciones/${quotation.quotationId}`);
        }}
      />
    </>
  );
}
