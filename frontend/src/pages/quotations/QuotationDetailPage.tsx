import {
  ArrowLeft,
  CheckCircle2,
  Clock3,
  CopyPlus,
  Download,
  Pencil,
  Plus,
  Send,
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
import type { QuotationStatusCatalog } from "@/features/catalogs/types";
import { quotationsApi } from "@/features/quotations/quotations.api";
import type { QuotationDetail, QuotationListItem } from "@/features/quotations/types";
import { QuotationHeaderDialog } from "@/features/quotations/QuotationHeaderDialog";
import { QuotationActionDialog } from "@/features/quotations/QuotationActionDialog";
import { ApproveQuotationDialog } from "@/features/quotations/ApproveQuotationDialog";
import { NewQuotationVersionDialog } from "@/features/quotations/NewQuotationVersionDialog";
import { QuotationProformaPanel } from "@/features/quotations/QuotationProformaPanel";
import { CreateOperationDialog } from "@/features/operations/CreateOperationDialog";
import { operationsApi } from "@/features/operations/operations.api";
import type { OperationListItem } from "@/features/operations/types";
import { HttpError } from "@/lib/api/http";

type TabKey = "SUMMARY" | "PROFORMA" | "HISTORY" | "VERSIONS";
type ActionKey = "SEND" | "REJECT" | "CANCEL" | "EXPIRE" | null;

function tone(status: string) {
  if (status === "APPROVED") return "green" as const;
  if (status === "REJECTED" || status === "CANCELLED") return "red" as const;
  if (status === "EXPIRED") return "amber" as const;
  if (status === "SENT") return "blue" as const;
  return "neutral" as const;
}

export function QuotationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const quotationId = Number(id);
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [quote, setQuote] = useState<QuotationDetail | null>(null);
  const [statuses, setStatuses] = useState<QuotationStatusCatalog[]>([]);
  const [versions, setVersions] = useState<QuotationListItem[]>([]);
  const [tab, setTab] = useState<TabKey>("SUMMARY");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [headerOpen, setHeaderOpen] = useState(false);
  const [action, setAction] = useState<ActionKey>(null);
  const [approveOpen, setApproveOpen] = useState(false);
  const [versionOpen, setVersionOpen] = useState(false);
  const [operationCreateOpen, setOperationCreateOpen] = useState(false);
  const [linkedOperation, setLinkedOperation] = useState<OperationListItem | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [detail, statusData] = await Promise.all([
        quotationsApi.get(quotationId),
        catalogsApi.quotationStatuses()
      ]);
      setQuote(detail);
      setStatuses(statusData);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo consultar la cotización.");
    } finally {
      setLoading(false);
    }
  }, [quotationId]);

  useEffect(() => {
    if (Number.isFinite(quotationId) && quotationId > 0) load();
  }, [load, quotationId]);

  useEffect(() => {
    if (tab === "VERSIONS" && quote) {
      quotationsApi.byRequest(quote.requestId)
        .then(setVersions)
        .catch(() => setVersions([]));
    }
  }, [tab, quote?.requestId]);

  useEffect(() => {
    if (!quote || quote.status !== "APPROVED" || clientMode) {
      setLinkedOperation(null);
      return;
    }

    operationsApi.list({
      quotationId: quote.quotationId,
      page: 1,
      pageSize: 1
    })
      .then((result) => setLinkedOperation(result.items[0] ?? null))
      .catch(() => setLinkedOperation(null));
  }, [quote?.quotationId, quote?.status, clientMode]);

  if (loading) {
    return <div className="space-y-4"><Skeleton className="h-20" /><Skeleton className="h-96" /></div>;
  }
  if (error && !quote) return <InlineAlert message={error} />;
  if (!quote) return null;

  const statusLabel = statuses.find((x) => x.code === quote.status)?.name ?? quote.status;
  const draft = quote.status === "DRAFT";
  const sent = quote.status === "SENT";
  const canManage = hasPermission(Permissions.QuotationsManage);
  const canDecide = hasPermission(Permissions.QuotationsApprove);
  const canVersion = ["REJECTED", "EXPIRED", "CANCELLED"].includes(quote.status);

  const reload = async () => {
    const updated = await quotationsApi.get(quote.quotationId);
    setQuote(updated);
  };


  const performAction = async (value: string) => {
    let updated: QuotationDetail;
    if (action === "SEND") updated = await quotationsApi.send(quote.quotationId, value || undefined);
    else if (action === "REJECT") updated = await quotationsApi.reject(quote.quotationId, value);
    else if (action === "CANCEL") updated = await quotationsApi.cancel(quote.quotationId, value);
    else updated = await quotationsApi.expire(quote.quotationId, value || undefined);

    setQuote(updated);
    setAction(null);
  };

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/cotizaciones")}
      >
        <ArrowLeft className="size-4" />
        Volver a cotizaciones
      </button>

      <PageHeader
        eyebrow={clientMode ? "Cotización recibida" : "Cotización"}
        title={`${quote.quotationNo} · V${quote.versionNo}`}
        description={`${quote.clientName} · Solicitud ${quote.requestNo}`}
        actions={
          <>
            {(!clientMode || quote.sentAt) && (
              <Button variant="secondary" onClick={async () => {
                try {
                  const { blob, fileName } = await quotationsApi.downloadPdf(quote.quotationId);
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement("a");
                  a.href = url; a.download = fileName; document.body.appendChild(a); a.click(); a.remove();
                  URL.revokeObjectURL(url);
                } catch (e) {
                  setError(e instanceof HttpError ? e.message : "No se pudo descargar la cotización PDF.");
                }
              }}>
                <Download className="size-4" />
                Descargar cotización PDF
              </Button>
            )}

            {!clientMode && quote.status === "APPROVED" && canManage && (
              linkedOperation ? (
                <Button onClick={() => navigate(`/operaciones/${linkedOperation.operationId}`)}>
                  Ver operación {linkedOperation.operationNo}
                </Button>
              ) : (
                <Button onClick={() => setOperationCreateOpen(true)}>
                  Crear operación
                </Button>
              )
            )}

            {!clientMode && draft && canManage && (
              <>
                <Button variant="secondary" onClick={() => setHeaderOpen(true)}>
                  <Pencil className="size-4" />
                  Editar cabecera
                </Button>
                <Button onClick={() => setAction("SEND")}>
                  <Send className="size-4" />
                  Enviar al cliente
                </Button>
              </>
            )}

            {sent && canDecide && (
              <>
                <Button onClick={() => setApproveOpen(true)}>
                  <CheckCircle2 className="size-4" />
                  Aprobar
                </Button>
                <Button variant="danger" onClick={() => setAction("REJECT")}>
                  <XCircle className="size-4" />
                  Rechazar
                </Button>
              </>
            )}

            {!clientMode && sent && canManage && quote.validUntil && new Date(quote.validUntil) < new Date() && (
              <Button variant="secondary" onClick={() => setAction("EXPIRE")}>
                <Clock3 className="size-4" />
                Marcar vencida
              </Button>
            )}

            {!clientMode && canManage && quote.status !== "APPROVED" && quote.status !== "CANCELLED" && (
              <Button variant="ghost" onClick={() => setAction("CANCEL")}>
                Cancelar
              </Button>
            )}

            {!clientMode && canManage && canVersion && (
              <Button variant="secondary" onClick={() => setVersionOpen(true)}>
                <CopyPlus className="size-4" />
                Nueva versión
              </Button>
            )}
          </>
        }
      />

      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      <div className="mb-5 flex flex-wrap gap-2">
        <Badge tone={tone(quote.status)}>{statusLabel}</Badge>
        <Badge tone="neutral">{quote.currencyCode}</Badge>
        {quote.validUntil && <Badge tone="neutral">Vigencia: {formatDate(quote.validUntil)}</Badge>}
      </div>

      {sent && clientMode && canDecide && (
        <div className="mb-5 rounded-2xl border border-blue-200 bg-blue-50 px-4 py-4">
          <div className="font-semibold text-blue-900">Pendiente de tu decisión</div>
          <p className="mt-1 text-sm leading-6 text-blue-800">
            Revisa conceptos, importes y términos. Como administrador del cliente puedes aprobar o rechazar esta versión.
          </p>
        </div>
      )}

      <Tabs
        value={tab}
        onChange={(value) => setTab(value as TabKey)}
        items={[
          { value: "SUMMARY", label: "Resumen" },
          { value: "PROFORMA", label: "Proforma" },
          { value: "HISTORY", label: "Historial", count: quote.history.length },
          { value: "VERSIONS", label: "Versiones" }
        ]}
      />

      <div className="mt-4">
        {tab === "SUMMARY" && (
          <div className="grid gap-4 xl:grid-cols-[1.35fr_0.65fr]">
            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">Información comercial</h2>
              <div className="mt-5 grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
                {[
                  ["Solicitud", quote.requestNo],
                  ["Cliente", quote.clientName],
                  ["Fecha emisión", formatDate(quote.issueDate)],
                  ["Vigencia", formatDate(quote.validUntil)],
                  ["Moneda", quote.currencyCode],
                  ["Estado", statusLabel],
                  ["Enviada", formatDate(quote.sentAt)],
                  ["Aprobada", formatDate(quote.approvedAt)],
                  ["Aprobada por", quote.approvedByName || "—"]
                ].map(([label, value]) => (
                  <div key={label}>
                    <div className="text-xs font-medium text-slate-400">{label}</div>
                    <div className="mt-1 text-sm font-medium text-slate-800">{value}</div>
                  </div>
                ))}
              </div>

              <div className="mt-6 border-t border-slate-100 pt-5">
                <div className="text-xs font-medium text-slate-400">Términos comerciales</div>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                  {quote.terms || "Sin términos adicionales registrados."}
                </p>
              </div>

              {!clientMode && (
                <div className="mt-6 border-t border-slate-100 pt-5">
                  <div className="text-xs font-medium text-slate-400">Notas internas</div>
                  <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                    {quote.internalNotes || "Sin notas internas."}
                  </p>
                </div>
              )}

              {quote.rejectionReason && (
                <div className="mt-6 rounded-xl border border-red-200 bg-red-50 p-4">
                  <div className="text-sm font-semibold text-red-900">Motivo de rechazo</div>
                  <p className="mt-1 text-sm leading-6 text-red-800">{quote.rejectionReason}</p>
                </div>
              )}
            </Card>

            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">Totales</h2>
              <div className="mt-5 space-y-3">
                <div className="flex justify-between text-sm text-slate-600">
                  <span>Subtotal bruto</span>
                  <span className="font-medium tabular-nums">{formatMoney(quote.subtotal, quote.currencyCode)}</span>
                </div>
                <div className="flex justify-between text-sm text-slate-600">
                  <span>Descuentos</span>
                  <span className="font-medium tabular-nums">-{formatMoney(quote.discountAmount, quote.currencyCode)}</span>
                </div>
                <div className="flex justify-between text-sm text-slate-600">
                  <span>Impuestos</span>
                  <span className="font-medium tabular-nums">{formatMoney(quote.taxAmount, quote.currencyCode)}</span>
                </div>
                <div className="border-t border-slate-200 pt-4">
                  <div className="flex items-end justify-between gap-3">
                    <span className="font-semibold text-slate-900">Total</span>
                    <span className="text-2xl font-bold tracking-tight text-slate-950">
                      {formatMoney(quote.totalAmount, quote.currencyCode)}
                    </span>
                  </div>
                </div>
              </div>

              <Link
                to={`/solicitudes/${quote.requestId}`}
                className="mt-6 inline-flex text-sm font-semibold text-blue-700 hover:text-blue-800"
              >
                Ver solicitud relacionada
              </Link>
            </Card>
          </div>
        )}

        {tab === "PROFORMA" && (
          <QuotationProformaPanel quote={quote} editable={!clientMode && draft && canManage} onChanged={load} />
        )}


        {tab === "HISTORY" && (
          <Card className="p-5">
            <h2 className="font-semibold text-slate-900">Historial de estados</h2>
            <div className="mt-5 space-y-0">
              {quote.history.map((event, index) => (
                <div key={event.quotationStatusHistoryId} className="relative flex gap-4 pb-6 last:pb-0">
                  {index < quote.history.length - 1 && (
                    <div className="absolute left-[17px] top-8 h-[calc(100%-16px)] w-px bg-slate-200" />
                  )}
                  <div className="relative z-10 grid size-9 shrink-0 place-items-center rounded-full bg-blue-50 text-blue-700">
                    <CheckCircle2 className="size-4" />
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

        {tab === "VERSIONS" && (
          <Card className="overflow-hidden">
            {versions.length === 0 ? (
              <EmptyState title="Sin versiones" description="No se encontraron versiones relacionadas." />
            ) : (
              <div className="divide-y divide-slate-100">
                {versions.map((version) => (
                  <button
                    key={version.quotationId}
                    type="button"
                    className="flex w-full flex-col gap-2 px-5 py-4 text-left transition hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"
                    onClick={() => navigate(`/cotizaciones/${version.quotationId}`)}
                  >
                    <div>
                      <div className="font-semibold text-slate-900">{version.quotationNo} · V{version.versionNo}</div>
                      <div className="mt-1 text-xs text-slate-500">Emisión {formatDate(version.issueDate)}</div>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge tone={tone(version.status)}>
                        {statuses.find((x) => x.code === version.status)?.name ?? version.status}
                      </Badge>
                      <span className="font-semibold text-slate-800">{formatMoney(version.totalAmount, version.currencyCode)}</span>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </Card>
        )}
      </div>

      <QuotationHeaderDialog
        open={headerOpen}
        quotation={quote}
        onClose={() => setHeaderOpen(false)}
        onSaved={(updated) => {
          setQuote(updated);
          setHeaderOpen(false);
        }}
      />


      <QuotationActionDialog
        open={action !== null}
        title={
          action === "SEND"
            ? "Enviar cotización"
            : action === "REJECT"
              ? "Rechazar cotización"
              : action === "CANCEL"
                ? "Cancelar cotización"
                : "Marcar cotización vencida"
        }
        description={
          action === "SEND"
            ? "Al enviar, la solicitud relacionada pasará a estado COTIZADA y el cliente podrá ver esta versión."
            : undefined
        }
        label={action === "SEND" || action === "EXPIRE" ? "Comentario" : "Motivo"}
        placeholder={
          action === "REJECT"
            ? "Indica por qué no aceptas esta cotización."
            : action === "CANCEL"
              ? "Indica el motivo de cancelación."
              : undefined
        }
        required={action === "REJECT" || action === "CANCEL"}
        confirmLabel={
          action === "SEND"
            ? "Enviar al cliente"
            : action === "REJECT"
              ? "Confirmar rechazo"
              : action === "CANCEL"
                ? "Cancelar cotización"
                : "Marcar vencida"
        }
        danger={action === "REJECT" || action === "CANCEL"}
        onClose={() => setAction(null)}
        onConfirm={performAction}
      />

      <ApproveQuotationDialog
        open={approveOpen}
        clientMode={clientMode}
        onClose={() => setApproveOpen(false)}
        onConfirm={async (approvedByName, comment) => {
          const updated = await quotationsApi.approve(
            quote.quotationId,
            approvedByName,
            comment
          );
          setQuote(updated);
          setApproveOpen(false);
        }}
      />

      <NewQuotationVersionDialog
        open={versionOpen}
        quotation={quote}
        onClose={() => setVersionOpen(false)}
        onCreated={(created) => {
          setVersionOpen(false);
          navigate(`/cotizaciones/${created.quotationId}`);
        }}
      />

      <CreateOperationDialog
        open={operationCreateOpen}
        quotationId={quote.quotationId}
        quotationNo={`${quote.quotationNo} · V${quote.versionNo}`}
        onClose={() => setOperationCreateOpen(false)}
        onCreated={(operation) => {
          setOperationCreateOpen(false);
          navigate(`/operaciones/${operation.operationId}`);
        }}
      />
    </>
  );
}
