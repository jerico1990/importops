import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { HttpError } from "@/lib/api/http";
import type { WorkflowStage } from "@/features/catalogs/types";
import { workflowConfigurationApi } from "./workflow.api";
import type { WorkflowTaskTemplate } from "./types";

export function WorkflowTaskTemplateDialog({
  open,
  onClose,
  item,
  stages,
  initialStageId,
  onSaved
}: {
  open: boolean;
  onClose: () => void;
  item: WorkflowTaskTemplate | null;
  stages: WorkflowStage[];
  initialStageId?: number;
  onSaved: () => void;
}) {
  const [stageId, setStageId] = useState<string>();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [mandatory, setMandatory] = useState(true);
  const [dueDays, setDueDays] = useState("");
  const [order, setOrder] = useState("10");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setStageId(String(item?.workflowStageId ?? initialStageId ?? stages[0]?.workflowStageId ?? ""));
    setTitle(item?.title ?? "");
    setDescription(item?.description ?? "");
    setMandatory(item?.isMandatory ?? true);
    setDueDays(item?.dueDaysOffset == null ? "" : String(item.dueDaysOffset));
    setOrder(String(item?.displayOrder ?? 10));
    setError("");
  }, [open, item, initialStageId, stages]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const parsedStage = Number(stageId);
    const parsedOrder = Number(order);
    const parsedDue = dueDays.trim() === "" ? null : Number(dueDays);
    if (!parsedStage) return setError("Selecciona una etapa.");
    if (!title.trim()) return setError("El título es obligatorio.");
    if (!Number.isInteger(parsedOrder) || parsedOrder < 0) return setError("El orden debe ser un número entero mayor o igual a 0.");
    if (parsedDue !== null && (!Number.isInteger(parsedDue) || parsedDue < 0)) return setError("El plazo en días debe ser un número entero mayor o igual a 0.");

    setSaving(true);
    setError("");
    try {
      const request = {
        workflowStageId: parsedStage,
        title: title.trim(),
        description: description.trim() || null,
        isMandatory: mandatory,
        dueDaysOffset: parsedDue,
        displayOrder: parsedOrder
      };
      if (item) await workflowConfigurationApi.updateTaskTemplate(item.stageTaskTemplateId, request);
      else await workflowConfigurationApi.createTaskTemplate(request);
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo guardar la plantilla de tarea.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={item ? "Editar tarea por etapa" : "Nueva tarea por etapa"}
      description="La tarea se generará automáticamente al ingresar una operación a la etapa y se asignará al responsable de la operación."
      className="sm:max-w-2xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <div>
          <label className="mb-1.5 block text-sm font-semibold text-slate-700">Etapa</label>
          <SearchableSelect
            value={stageId}
            onChange={setStageId}
            options={stages.map((stage) => ({
              value: String(stage.workflowStageId),
              label: `${stage.displayOrder}. ${stage.name}`,
              description: stage.code
            }))}
            placeholder="Selecciona una etapa"
            searchPlaceholder="Buscar etapa..."
          />
        </div>
        <div>
          <label className="mb-1.5 block text-sm font-semibold text-slate-700">Título</label>
          <Input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={180} placeholder="Ej. Confirmar booking o reserva" />
        </div>
        <div>
          <label className="mb-1.5 block text-sm font-semibold text-slate-700">Descripción</label>
          <textarea
            className="focus-ring min-h-24 w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={500}
            placeholder="Describe qué debe realizar el responsable de la operación."
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm font-semibold text-slate-700">Plazo en días</label>
            <Input type="number" min="0" max="3650" step="1" value={dueDays} onChange={(e) => setDueDays(e.target.value)} placeholder="Sin plazo" />
            <p className="mt-1 text-xs text-slate-500">Se calcula desde el momento en que se genera la tarea.</p>
          </div>
          <div>
            <label className="mb-1.5 block text-sm font-semibold text-slate-700">Orden</label>
            <Input type="number" min="0" max="100000" step="1" value={order} onChange={(e) => setOrder(e.target.value)} />
          </div>
        </div>
        <label className="flex items-center gap-3 rounded-xl border border-slate-200 p-4 text-sm font-semibold text-slate-700">
          <input type="checkbox" checked={mandatory} onChange={(e) => setMandatory(e.target.checked)} />
          Obligatoria
        </label>
        <div className="flex justify-end gap-2 border-t pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>{item ? "Guardar cambios" : "Crear tarea"}</Button>
        </div>
      </form>
    </Dialog>
  );
}
