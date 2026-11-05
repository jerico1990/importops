import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { AppUserCatalog, CodeNameCatalog } from "@/features/catalogs/types";
import { operationsApi } from "./operations.api";
import type { OperationTask } from "./types";

function displayName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function OperationTaskDialog({
  open, operationId, currentStageId, task, onClose, onSaved
}: {
  open: boolean;
  operationId: number;
  currentStageId: number;
  task?: OperationTask | null;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}) {
  const template = task?.isFromTemplate ?? false;
  const [users, setUsers] = useState<AppUserCatalog[]>([]);
  const [priorities, setPriorities] = useState<CodeNameCatalog[]>([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<string>();
  const [assignedUserId, setAssignedUserId] = useState<string>();
  const [dueAt, setDueAt] = useState("");
  const [isMandatory, setIsMandatory] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    Promise.all([
      catalogsApi.operationResponsibles(),
      catalogsApi.taskPriorities()
    ]).then(([u, p]) => {
      setUsers(u);
      setPriorities(p);
    }).catch(() => {});
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setTitle(task?.title ?? "");
    setDescription(task?.description ?? "");
    setPriority(task?.priority ?? undefined);
    setAssignedUserId(task?.assignedUserId ? String(task.assignedUserId) : undefined);
    setDueAt(task?.dueAt ? task.dueAt.slice(0, 16) : "");
    setIsMandatory(task?.isMandatory ?? false);
    setError("");
  }, [open, task]);

  useEffect(() => {
    if (!priority && priorities.length) setPriority(priorities[0].code);
  }, [priorities, priority]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!title.trim() || !priority) {
      setError("Título y prioridad son obligatorios.");
      return;
    }

    const payload = {
      title: title.trim(),
      description: description.trim() || null,
      priority,
      assignedUserId: assignedUserId ? Number(assignedUserId) : null,
      dueAt: dueAt || null,
      isMandatory
    };

    setSaving(true);
    setError("");
    try {
      if (task) {
        await operationsApi.updateTask(operationId, task.operationTaskId, payload);
      } else {
        await operationsApi.createTask(operationId, {
          workflowStageId: currentStageId,
          ...payload
        });
      }
      await onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo guardar la tarea.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={task ? "Editar tarea" : "Nueva tarea"}
      description={
        template
          ? "Tarea generada por el workflow. Su definición y obligatoriedad no se modifican."
          : "Tarea manual de la operación."
      }
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Título *</span>
          <Input value={title} onChange={(e) => setTitle(e.target.value)} disabled={template} />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Descripción</span>
          <textarea
            className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm disabled:bg-slate-50"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={template}
          />
        </label>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Prioridad *</span>
            <SearchableSelect
              value={priority}
              onChange={setPriority}
              options={priorities.map((x) => ({ value: x.code, label: x.name }))}
            />
          </div>
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Responsable</span>
            <SearchableSelect
              value={assignedUserId}
              onChange={setAssignedUserId}
              options={users.map((u) => ({
                value: String(u.userId),
                label: displayName(u),
                description: u.email ?? u.username
              }))}
              placeholder="Responsable de operación"
            />
          </div>
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Vencimiento</span>
            <Input type="datetime-local" value={dueAt} onChange={(e) => setDueAt(e.target.value)} />
          </label>
          {!template && (
            <label className="flex items-center gap-2 pt-7 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={isMandatory}
                onChange={(e) => setIsMandatory(e.target.checked)}
                className="size-4 accent-blue-700"
              />
              Obligatoria
            </label>
          )}
        </div>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar tarea</Button>
        </div>
      </form>
    </Dialog>
  );
}
