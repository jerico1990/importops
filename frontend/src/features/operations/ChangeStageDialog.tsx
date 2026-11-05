import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { operationsApi } from "./operations.api";
import type { OperationAvailableStage } from "./types";

export function ChangeStageDialog({
  open, operationId, currentStageName, onClose, onChanged
}: {
  open: boolean;
  operationId: number;
  currentStageName: string;
  onClose: () => void;
  onChanged: () => Promise<void> | void;
}) {
  const [stages, setStages] = useState<OperationAvailableStage[]>([]);
  const [stageId, setStageId] = useState<string>();
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setStageId(undefined);
    setComment("");
    setError("");
    operationsApi.availableStages(operationId)
      .then(setStages)
      .catch((e) => setError(e instanceof Error ? e.message : "No se pudieron cargar las transiciones."));
  }, [open, operationId]);

  const selected = stages.find((x) => String(x.workflowStageId) === stageId);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!stageId) {
      setError("Selecciona una etapa destino.");
      return;
    }
    if (selected?.requiresComment && !comment.trim()) {
      setError("Esta transición requiere comentario.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await operationsApi.changeStage(operationId, Number(stageId), comment.trim() || undefined);
      await onChanged();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo avanzar la etapa.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Avanzar workflow"
      description={`Etapa actual: ${currentStageName}. Solo se muestran transiciones habilitadas.`}
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <SearchableSelect
          value={stageId}
          onChange={setStageId}
          options={stages.map((x) => ({
            value: String(x.workflowStageId),
            label: x.name,
            description: x.code
          }))}
          placeholder="Seleccionar etapa destino"
        />
        <textarea
          className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder={selected?.requiresComment ? "Comentario obligatorio" : "Comentario opcional"}
        />
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Cambiar etapa</Button>
        </div>
      </form>
    </Dialog>
  );
}
