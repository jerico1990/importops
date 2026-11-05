import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { InlineAlert } from "@/components/feedback/InlineAlert";

export function QuotationActionDialog({
  open,
  title,
  description,
  label,
  placeholder,
  required,
  confirmLabel,
  danger = false,
  onClose,
  onConfirm
}: {
  open: boolean;
  title: string;
  description?: string;
  label: string;
  placeholder?: string;
  required: boolean;
  confirmLabel: string;
  danger?: boolean;
  onClose: () => void;
  onConfirm: (value: string) => Promise<void>;
}) {
  const [value, setValue] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setValue("");
      setError("");
    }
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (required && !value.trim()) {
      setError("Este campo es obligatorio.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      await onConfirm(value.trim());
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo completar la acción.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} title={title} description={description} onClose={onClose} className="sm:max-w-xl">
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            {label}{required ? " *" : ""}
          </span>
          <textarea
            className="focus-ring min-h-28 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder={placeholder}
          />
        </label>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" variant={danger ? "danger" : "primary"} loading={saving}>{confirmLabel}</Button>
        </div>
      </form>
    </Dialog>
  );
}
