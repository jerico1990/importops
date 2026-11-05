import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";

export function ApproveQuotationDialog({
  open,
  clientMode,
  onClose,
  onConfirm
}: {
  open: boolean;
  clientMode: boolean;
  onClose: () => void;
  onConfirm: (approvedByName: string, comment?: string) => Promise<void>;
}) {
  const [approvedByName, setApprovedByName] = useState("");
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setApprovedByName("");
      setComment("");
      setError("");
    }
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!clientMode && !approvedByName.trim()) {
      setError("Indica el nombre de la persona que aprobó la cotización.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      await onConfirm(
        clientMode ? "" : approvedByName.trim(),
        comment.trim() || undefined
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo aprobar la cotización.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Aprobar cotización"
      description={
        clientMode
          ? "Tu usuario quedará registrado como responsable de la aprobación."
          : "Registra quién confirmó la aprobación comercial."
      }
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        {!clientMode && (
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Aprobada por *</span>
            <Input
              value={approvedByName}
              onChange={(e) => setApprovedByName(e.target.value)}
              placeholder="Nombre del contacto o responsable que aprobó"
            />
          </label>
        )}

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Comentario</span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Referencia de aprobación, OC, correo u observación."
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Confirmar aprobación</Button>
        </div>
      </form>
    </Dialog>
  );
}
