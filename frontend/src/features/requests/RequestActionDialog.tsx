import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { InlineAlert } from "@/components/feedback/InlineAlert";

export function RequestActionDialog({
  open,
  title,
  description,
  label = "Comentario",
  placeholder,
  required = false,
  confirmLabel,
  danger = false,
  onClose,
  onConfirm
}: {
  open: boolean;
  title: string;
  description?: string;
  label?: string;
  placeholder?: string;
  required?: boolean;
  confirmLabel: string;
  danger?: boolean;
  onClose: () => void;
  onConfirm: (comment: string) => Promise<void>;
}) {
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setComment("");
      setError("");
    }
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (required && !comment.trim()) {
      setError("Debes ingresar un comentario.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      await onConfirm(comment.trim());
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo completar la acción.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={title}
      description={description}
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form onSubmit={submit} className="space-y-4">
        {error && <InlineAlert message={error} />}
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            {label}{required ? " *" : ""}
          </span>
          <textarea
            className="focus-ring min-h-28 w-full resize-y rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm shadow-sm"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder={placeholder}
          />
        </label>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" variant={danger ? "danger" : "primary"} loading={saving}>
            {confirmLabel}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
