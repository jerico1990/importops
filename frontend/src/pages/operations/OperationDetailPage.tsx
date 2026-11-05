import {
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  CirclePause,
  CirclePlay,
  Flag,
  Plus,
  Route,
  UserRound,
  XCircle
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs } from "@/components/ui/Tabs";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { operationsApi } from "@/features/operations/operations.api";
import type {
  OperationDetail,
  OperationEvent,
  OperationNote,
  OperationStageHistory,
  OperationStatusHistory,
  OperationTask,
  OperationAvailableStage
} from "@/features/operations/types";
import { AssignOperationDialog } from "@/features/operations/AssignOperationDialog";
import { ChangeStageDialog } from "@/features/operations/ChangeStageDialog";
import { OperationActionDialog } from "@/features/operations/OperationActionDialog";
import { OperationTaskDialog } from "@/features/operations/OperationTaskDialog";
import { OperationEventDialog } from "@/features/operations/OperationEventDialog";
import { OperationNoteDialog } from "@/features/operations/OperationNoteDialog";
import { OperationShipmentsPanel } from "@/features/shipments/OperationShipmentsPanel";
import { OperationDocumentsPanel } from "@/features/documents/OperationDocumentsPanel";
import { OperationCustomsPanel } from "@/features/customs/OperationCustomsPanel";
import { OperationFinancePanel } from "@/features/finance/OperationFinancePanel";
import { HttpError } from "@/lib/api/http";
import { formatDate, formatMoney } from "@/lib/format";

type TabKey = "SUMMARY" | "WORKFLOW" | "TASKS" | "SHIPMENTS" | "DOCUMENTS" | "CUSTOMS" | "FINANCE" | "TIMELINE" | "NOTES";
type ActionKey = "HOLD" | "RESUME" | "CANCEL" | "CLOSE" | null;

function tone(code: string) {
  if (["CLOSED", "DELIVERED", "COMPLETE", "COMPLETED"].includes(code)) return "green" as const;
  if (["CANCELLED", "OBSERVED"].includes(code)) return "red" as const;
  if (["ON_HOLD", "PENDING", "BLOCKED"].includes(code)) return "amber" as const;
  if (["IN_PROGRESS", "OPEN"].includes(code)) return "blue" as const;
  return "neutral" as const;
}

export function OperationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const operationId = Number(id);
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";

  const [operation, setOperation] = useState<OperationDetail | null>(null);
  const [opStatuses, setOpStatuses] = useState<CodeNameCatalog[]>([]);
  const [docStatuses, setDocStatuses] = useState<CodeNameCatalog[]>([]);
  const [finStatuses, setFinStatuses] = useState<CodeNameCatalog[]>([]);
  const [taskStatuses, setTaskStatuses] = useState<CodeNameCatalog[]>([]);
  const [stageHistory, setStageHistory] = useState<OperationStageHistory[]>([]);
  const [statusHistory, setStatusHistory] = useState<OperationStatusHistory[]>([]);
  const [tasks, setTasks] = useState<OperationTask[]>([]);
  // Orden estable: etapa, atención pendiente, prioridad y vencimiento.
  // No se altera el estado ni la secuencia original en el backend.
  const orderedTasks = [...tasks].sort((a, b) => {
    const stageA = a.workflowStageId ?? Number.MAX_SAFE_INTEGER;
    const stageB = b.workflowStageId ?? Number.MAX_SAFE_INTEGER;
    if (stageA !== stageB) return stageA - stageB;
    const rank = (priority: string) => ({ CRITICAL: 0, URGENT: 0, HIGH: 1, MEDIUM: 2, NORMAL: 2, LOW: 3 }[priority.toUpperCase()] ?? 4);
    if (rank(a.priority) !== rank(b.priority)) return rank(a.priority) - rank(b.priority);
    const dueA = a.dueAt ? Date.parse(a.dueAt) : Number.MAX_SAFE_INTEGER;
    const dueB = b.dueAt ? Date.parse(b.dueAt) : Number.MAX_SAFE_INTEGER;
    if (dueA !== dueB) return dueA - dueB;
    return a.operationTaskId - b.operationTaskId;
  });
  const [events, setEvents] = useState<OperationEvent[]>([]);
  const [notes, setNotes] = useState<OperationNote[]>([]);
  const [availableStages, setAvailableStages] = useState<OperationAvailableStage[]>([]);
  const [tab, setTab] = useState<TabKey>("SUMMARY");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [assignOpen, setAssignOpen] = useState(false);
  const [stageOpen, setStageOpen] = useState(false);
  const [taskOpen, setTaskOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<OperationTask | null>(null);
  const [eventOpen, setEventOpen] = useState(false);
  const [noteOpen, setNoteOpen] = useState(false);
  const [action, setAction] = useState<ActionKey>(null);

  const canManage = hasPermission(Permissions.OperationsManage);
  const canClose = hasPermission(Permissions.OperationsClose);
  const canManageShipments = hasPermission(Permissions.ShipmentsManage);
  const canUploadDocuments = hasPermission(Permissions.DocumentsUpload);
  const canManageDocuments = hasPermission(Permissions.DocumentsManage);
  const canViewCustoms = hasPermission(Permissions.CustomsView);
  const canManageCustoms = hasPermission(Permissions.CustomsManage);
  const canViewFinance = !clientMode && hasPermission(Permissions.FinanceView);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const detail = await operationsApi.get(operationId);
      setOperation(detail);

      const promises: Promise<unknown>[] = [
        catalogsApi.operationStatuses().then(setOpStatuses),
        catalogsApi.documentaryStatuses().then(setDocStatuses),
        catalogsApi.operationTaskStatuses().then(setTaskStatuses)
      ];
      if (!clientMode) promises.push(catalogsApi.financialStatuses().then(setFinStatuses));
      await Promise.all(promises);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo consultar la operación.");
    } finally {
      setLoading(false);
    }
  }, [operationId, clientMode]);

  useEffect(() => {
    if (Number.isFinite(operationId) && operationId > 0) void load();
  }, [load, operationId]);

  const loadWorkflow = async () => {
    const [a, b] = await Promise.all([
      operationsApi.stageHistory(operationId),
      operationsApi.statusHistory(operationId)
    ]);
    setStageHistory(a);
    setStatusHistory(b);
  };

  const loadTasks = async () => {
    if (clientMode) return;
    setTasks(await operationsApi.tasks(operationId));
  };

  const loadEvents = async () => {
    setEvents(await operationsApi.events(operationId));
  };

  const loadNotes = async () => {
    setNotes(await operationsApi.notes(operationId));
  };

  useEffect(() => {
    if (!operation) return;
    if (tab === "WORKFLOW") void loadWorkflow();
    if (tab === "TASKS") void loadTasks();
    if (tab === "TIMELINE") void loadEvents();
    if (tab === "NOTES") void loadNotes();
  }, [tab, operation?.operationId]);

  useEffect(() => {
    if (
      !operation ||
      clientMode ||
      !canManage ||
      ["CANCELLED", "CLOSED"].includes(operation.operationalStatus)
    ) {
      setAvailableStages([]);
      return;
    }

    operationsApi.availableStages(operation.operationId)
      .then(setAvailableStages)
      .catch(() => setAvailableStages([]));
  }, [
    operation?.operationId,
    operation?.currentStageId,
    operation?.operationalStatus,
    clientMode,
    canManage
  ]);

  const label = (items: CodeNameCatalog[], code?: string | null) =>
    code ? items.find((x) => x.code === code)?.name ?? code : "—";

  const refresh = async () => {
    setOperation(await operationsApi.get(operationId));
  };

  const performAction = async (comment: string) => {
    if (!action) return;
    const updated =
      action === "HOLD"
        ? await operationsApi.hold(operationId, comment)
        : action === "RESUME"
          ? await operationsApi.resume(operationId, comment)
          : action === "CANCEL"
            ? await operationsApi.cancel(operationId, comment)
            : await operationsApi.close(operationId, comment);

    setOperation(updated);
    setAction(null);
  };

  const changeTaskStatus = async (task: OperationTask, status: string) => {
    let comment: string | undefined;
    if (status === "BLOCKED") {
      comment = window.prompt("Motivo del bloqueo:")?.trim() || undefined;
      if (!comment) return;
    }
    try {
      await operationsApi.changeTaskStatus(operationId, task.operationTaskId, status, comment);
      await loadTasks();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cambiar la tarea.");
    }
  };

  if (loading) return <div className="space-y-4"><Skeleton className="h-20" /><Skeleton className="h-96" /></div>;
  if (error && !operation) return <InlineAlert message={error} />;
  if (!operation) return null;

  const terminal = ["CANCELLED", "CLOSED"].includes(operation.operationalStatus);
  const canAdvanceWorkflow = !clientMode && canManage && !terminal && availableStages.length > 0;
  const canCloseOperation = !clientMode && canClose && operation.operationalStatus === "DELIVERED" && operation.currentStageCode === "SETTLEMENT";

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/operaciones")}
      >
        <ArrowLeft className="size-4" />
        Volver a operaciones
      </button>

      <PageHeader
        eyebrow={clientMode ? "Operación de mi empresa" : "Expediente operativo"}
        title={operation.operationNo}
        description={`${operation.clientName} · ${operation.currentStageName}`}
        actions={
          <>
            {!clientMode && canManage && !terminal && (
              <Button variant="secondary" onClick={() => setAssignOpen(true)}>
                <UserRound className="size-4" />
                Reasignar
              </Button>
            )}

            {canAdvanceWorkflow && (
              <Button onClick={() => setStageOpen(true)}>
                <Route className="size-4" />
                Avanzar etapa
              </Button>
            )}

            {!clientMode && canManage && ["OPEN", "IN_PROGRESS"].includes(operation.operationalStatus) && (
              <Button variant="secondary" onClick={() => setAction("HOLD")}>
                <CirclePause className="size-4" />
                Pausar
              </Button>
            )}

            {!clientMode && canManage && operation.operationalStatus === "ON_HOLD" && (
              <Button onClick={() => setAction("RESUME")}>
                <CirclePlay className="size-4" />
                Reanudar
              </Button>
            )}

            {canCloseOperation && (
              <Button onClick={() => setAction("CLOSE")}>
                <CheckCircle2 className="size-4" />
                Cerrar
              </Button>
            )}

            {!clientMode && canManage && !terminal && operation.operationalStatus !== "DELIVERED" && (
              <Button variant="ghost" onClick={() => setAction("CANCEL")}>
                <XCircle className="size-4" />
                Cancelar
              </Button>
            )}
          </>
        }
      />

      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      <div className="mb-5 flex flex-wrap gap-2">
        <Badge tone={tone(operation.operationalStatus)}>
          {label(opStatuses, operation.operationalStatus)}
        </Badge>
        <Badge tone={tone(operation.documentaryStatus)}>
          Docs: {label(docStatuses, operation.documentaryStatus)}
        </Badge>
        {!clientMode && operation.financialStatus && (
          <Badge tone="neutral">Finanzas: {label(finStatuses, operation.financialStatus)}</Badge>
        )}
        <Badge tone="blue">{operation.currentStageName}</Badge>
      </div>

      <Tabs
        value={tab}
        onChange={(value) => setTab(value as TabKey)}
        items={[
          { value: "SUMMARY", label: "Resumen" },
          { value: "WORKFLOW", label: "Workflow" },
          ...(!clientMode ? [{ value: "TASKS", label: "Tareas" }] : []),
          { value: "SHIPMENTS", label: "Embarques" },
          { value: "DOCUMENTS", label: "Documentos" },
          ...(canViewCustoms ? [{ value: "CUSTOMS", label: "Aduanas" }] : []),
          ...((clientMode || canViewFinance) ? [{ value: "FINANCE", label: "Finanzas" }] : []),
          { value: "TIMELINE", label: "Timeline" },
          { value: "NOTES", label: clientMode ? "Comunicaciones" : "Notas" }
        ]}
      />

      <div className="mt-4">
        {tab === "SUMMARY" && (
          <div className="grid gap-4 xl:grid-cols-[1.45fr_0.75fr]">
            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">Información operativa</h2>
              <div className="mt-5 grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
                {[
                  ["Cliente", operation.clientName],
                  ["Contacto", operation.clientContactName || "—"],
                  ["Proveedor", operation.supplierName || "—"],
                  ["Responsable", operation.responsibleName],
                  ["Modalidad", operation.transportMode || "—"],
                  ["Incoterm", operation.incotermCode || "—"],
                  ["Origen", [operation.originCountry, operation.originPort].filter(Boolean).join(" · ") || "—"],
                  ["Destino", [operation.destinationCountry, operation.destinationPort].filter(Boolean).join(" · ") || "—"],
                  ["Apertura", formatDate(operation.openedAt)],
                  ["Cierre estimado", formatDate(operation.estimatedCloseDate)],
                  ["Cierre real", formatDate(operation.actualCloseDate)],
                  ["Etapa actual", operation.currentStageName]
                ].map(([key, value]) => (
                  <div key={key}>
                    <div className="text-xs font-medium text-slate-400">{key}</div>
                    <div className="mt-1 text-sm font-medium text-slate-800">{value}</div>
                  </div>
                ))}
              </div>

              <div className="mt-6 border-t border-slate-100 pt-5">
                <div className="text-xs font-medium text-slate-400">Mercancía</div>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">
                  {operation.merchandiseDescription}
                </p>
              </div>

              {!clientMode && operation.notes && (
                <div className="mt-6 border-t border-slate-100 pt-5">
                  <div className="text-xs font-medium text-slate-400">Notas internas de apertura</div>
                  <p className="mt-2 whitespace-pre-wrap text-sm text-slate-600">{operation.notes}</p>
                </div>
              )}
            </Card>

            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">Origen comercial</h2>
              <div className="mt-4 space-y-4">
                {operation.requestId && (
                  <div>
                    <div className="text-xs text-slate-400">Solicitud</div>
                    <Link to={`/solicitudes/${operation.requestId}`} className="mt-1 inline-flex font-semibold text-blue-700">
                      {operation.requestNo || `#${operation.requestId}`}
                    </Link>
                  </div>
                )}
                {operation.quotationId && (
                  <div>
                    <div className="text-xs text-slate-400">Cotización aprobada</div>
                    <Link to={`/cotizaciones/${operation.quotationId}`} className="mt-1 inline-flex font-semibold text-blue-700">
                      {operation.quotationNo} · V{operation.quotationVersion}
                    </Link>
                  </div>
                )}
                {!clientMode && operation.quotationTotalSnapshot != null && operation.quotationCurrencySnapshot && (
                  <div>
                    <div className="text-xs text-slate-400">Importe cotizado</div>
                    <div className="mt-1 text-lg font-bold text-slate-900">
                      {formatMoney(operation.quotationTotalSnapshot, operation.quotationCurrencySnapshot)}
                    </div>
                  </div>
                )}
              </div>
            </Card>
          </div>
        )}

        {tab === "WORKFLOW" && (
          <div className="grid gap-4 xl:grid-cols-2">
            <Card className="p-5">
              <div className="flex items-center justify-between">
                <h2 className="font-semibold text-slate-900">Historial de etapas</h2>
                {canAdvanceWorkflow && (
                  <Button onClick={() => setStageOpen(true)}>
                    <Route className="size-4" />
                    Avanzar
                  </Button>
                )}
              </div>
              <div className="mt-5 space-y-4">
                {stageHistory.map((h) => (
                  <div key={h.operationStageHistoryId} className="flex gap-3">
                    <div className="grid size-9 shrink-0 place-items-center rounded-full bg-blue-50 text-blue-700">
                      <Flag className="size-4" />
                    </div>
                    <div>
                      <div className="font-semibold text-slate-900">{h.toStageName}</div>
                      <div className="mt-1 text-xs text-slate-400">{formatDate(h.changedAt)} · {h.changedByName}</div>
                      {h.comment && <p className="mt-2 text-sm text-slate-600">{h.comment}</p>}
                    </div>
                  </div>
                ))}
              </div>
            </Card>

            <Card className="p-5">
              <h2 className="font-semibold text-slate-900">Cambios de estado</h2>
              <div className="mt-5 space-y-3">
                {statusHistory.map((h) => (
                  <div key={h.operationStatusHistoryId} className="rounded-xl border border-slate-100 p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone={tone(h.newStatus)}>{h.statusDimension}</Badge>
                      <span className="font-semibold text-slate-800">{h.newStatus}</span>
                    </div>
                    <div className="mt-2 text-xs text-slate-400">{formatDate(h.changedAt)} · {h.changedByName}</div>
                    {h.comment && <p className="mt-2 text-sm text-slate-600">{h.comment}</p>}
                  </div>
                ))}
              </div>
            </Card>
          </div>
        )}

        {tab === "TASKS" && !clientMode && (
          <Card className="overflow-hidden">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <div>
                <h2 className="font-semibold text-slate-900">Tareas operativas</h2>
                <p className="mt-1 text-sm text-slate-500">Tareas automáticas del workflow y manuales.</p>
              </div>
              {canManage && !terminal && (
                <Button onClick={() => { setEditingTask(null); setTaskOpen(true); }}>
                  <Plus className="size-4" />
                  Nueva tarea
                </Button>
              )}
            </div>
            {tasks.length === 0 ? (
              <EmptyState title="Sin tareas" description="No existen tareas para esta operación." />
            ) : (
              <div className="divide-y divide-slate-100">
                {orderedTasks.map((task) => (
                  <div key={task.operationTaskId} className="px-5 py-4">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-semibold text-slate-900">{task.title}</span>
                          {task.isMandatory && <Badge tone="red">Obligatoria</Badge>}
                          {task.isFromTemplate && <Badge tone="blue">Workflow</Badge>}
                          <Badge tone={tone(task.taskStatus)}>{label(taskStatuses, task.taskStatus)}</Badge>
                        </div>
                        <div className="mt-1 text-sm text-slate-500">
                          {task.stageName || "Sin etapa"} · {task.assignedUserName || "Sin asignar"}
                          {task.dueAt ? ` · Vence ${formatDate(task.dueAt)}` : ""}
                        </div>
                      </div>
                      {canManage && !terminal && (
                        <div className="flex flex-wrap gap-2">
                          {task.allowedNextStatuses.map((next) => (
                            <Button key={next} variant="secondary" onClick={() => void changeTaskStatus(task, next)}>
                              {label(taskStatuses, next)}
                            </Button>
                          ))}
                          {!["COMPLETED", "CANCELLED"].includes(task.taskStatus) && (
                            <Button variant="ghost" onClick={() => { setEditingTask(task); setTaskOpen(true); }}>
                              Editar
                            </Button>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        )}

        {tab === "SHIPMENTS" && (
          <OperationShipmentsPanel
            operationId={operation.operationId}
            canManage={canManageShipments}
            clientMode={clientMode}
          />
        )}

        {tab === "DOCUMENTS" && (
          <OperationDocumentsPanel
            operationId={operation.operationId}
            clientMode={clientMode}
            canUpload={canUploadDocuments}
            canManage={canManageDocuments}
          />
        )}

        {tab === "CUSTOMS" && canViewCustoms && (
          <OperationCustomsPanel
            operationId={operation.operationId}
            canManage={canManageCustoms}
            clientMode={clientMode}
          />
        )}

        {tab === "FINANCE" && clientMode && (
          <Card className="p-5">
            <h3 className="text-base font-semibold text-slate-900">Liquidación final</h3>
            <p className="mt-2 text-sm text-slate-600">Consulta la liquidación emitida de tu operación y descarga el PDF con los comprobantes autorizados.</p>
            <div className="mt-4"><Button onClick={() => navigate(`/mi-liquidacion/${operation.operationId}`)}>Ver liquidación final</Button></div>
          </Card>
        )}

        {tab === "FINANCE" && !clientMode && canViewFinance && (
          <OperationFinancePanel operationId={operation.operationId} />
        )}

        {tab === "TIMELINE" && (
          <Card className="overflow-hidden">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <div>
                <h2 className="font-semibold text-slate-900">Timeline</h2>
                <p className="mt-1 text-sm text-slate-500">Eventos visibles según el perfil actual.</p>
              </div>
              {!clientMode && canManage && !terminal && (
                <Button onClick={() => setEventOpen(true)}>
                  <Plus className="size-4" />
                  Registrar evento
                </Button>
              )}
            </div>
            {events.length === 0 ? (
              <EmptyState title="Sin eventos" description="No existen eventos visibles." />
            ) : (
              <div className="divide-y divide-slate-100">
                {events.map((event) => (
                  <div key={event.operationEventId} className="flex gap-4 px-5 py-4">
                    <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-slate-100 text-slate-600">
                      <CalendarDays className="size-4" />
                    </div>
                    <div>
                      <div className="font-semibold text-slate-900">{event.title}</div>
                      <div className="mt-1 text-xs text-slate-400">{formatDate(event.eventDate)} · {event.createdByName}</div>
                      {event.description && <p className="mt-2 text-sm leading-6 text-slate-600">{event.description}</p>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        )}

        {tab === "NOTES" && (
          <Card className="overflow-hidden">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <div>
                <h2 className="font-semibold text-slate-900">{clientMode ? "Comunicaciones" : "Notas"}</h2>
                <p className="mt-1 text-sm text-slate-500">
                  {clientMode ? "Solo información destinada al cliente." : "Notas internas, cliente e incidencias."}
                </p>
              </div>
              {!clientMode && canManage && !terminal && (
                <Button onClick={() => setNoteOpen(true)}>
                  <Plus className="size-4" />
                  Agregar nota
                </Button>
              )}
            </div>
            {notes.length === 0 ? (
              <EmptyState title="Sin notas" description="No existen notas visibles." />
            ) : (
              <div className="divide-y divide-slate-100">
                {notes.map((note) => (
                  <div key={note.operationNoteId} className="px-5 py-4">
                    <div className="flex items-center gap-2">
                      <Badge tone={note.noteType === "INCIDENT" ? "red" : note.noteType === "CUSTOMER" ? "blue" : "neutral"}>
                        {note.noteType}
                      </Badge>
                      {note.isPinned && <Badge tone="amber">Fijada</Badge>}
                    </div>
                    <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">{note.noteText}</p>
                    <div className="mt-2 text-xs text-slate-400">{formatDate(note.createdAt)} · {note.createdByName}</div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        )}
      </div>

      <AssignOperationDialog
        open={assignOpen}
        currentUserId={operation.responsibleUserId}
        onClose={() => setAssignOpen(false)}
        onConfirm={async (userId, comment) => {
          setOperation(await operationsApi.assign(operationId, userId, comment));
          setAssignOpen(false);
        }}
      />

      <ChangeStageDialog
        open={stageOpen}
        operationId={operationId}
        currentStageName={operation.currentStageName}
        onClose={() => setStageOpen(false)}
        onChanged={async () => {
          setStageOpen(false);
          await refresh();
          if (tab === "WORKFLOW") await loadWorkflow();
          if (tab === "TASKS") await loadTasks();
        }}
      />

      <OperationActionDialog
        open={action !== null}
        title={
          action === "HOLD" ? "Pausar operación" :
          action === "RESUME" ? "Reanudar operación" :
          action === "CANCEL" ? "Cancelar operación" :
          "Cerrar operación"
        }
        description={
          action === "CLOSE"
            ? "El backend validará workflow, tareas, documentos y finanzas antes de cerrar."
            : undefined
        }
        confirmLabel={
          action === "HOLD" ? "Pausar" :
          action === "RESUME" ? "Reanudar" :
          action === "CANCEL" ? "Cancelar operación" :
          "Cerrar operación"
        }
        danger={action === "CANCEL"}
        onClose={() => setAction(null)}
        onConfirm={performAction}
      />

      <OperationTaskDialog
        open={taskOpen}
        operationId={operationId}
        currentStageId={operation.currentStageId}
        task={editingTask}
        onClose={() => { setTaskOpen(false); setEditingTask(null); }}
        onSaved={async () => {
          setTaskOpen(false);
          setEditingTask(null);
          await loadTasks();
        }}
      />

      <OperationEventDialog
        open={eventOpen}
        operationId={operationId}
        onClose={() => setEventOpen(false)}
        onSaved={async () => {
          setEventOpen(false);
          await loadEvents();
        }}
      />

      <OperationNoteDialog
        open={noteOpen}
        operationId={operationId}
        onClose={() => setNoteOpen(false)}
        onSaved={async () => {
          setNoteOpen(false);
          await loadNotes();
        }}
      />
    </>
  );
}
