import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { documentsApi } from "./documents.api";
import type { DocumentRecord } from "./types";

export function DocumentReplaceDialog({
  open,
  document,
  onClose,
  onReplaced
}: {
  open: boolean;
  document: DocumentRecord;
  onClose: () => void;
  onReplaced: (document: DocumentRecord) => void;
}) {
  const [file, setFile] = useState<File | null>(null);
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setFile(null);
      setNotes("");
      setError("");
    }
  }, [open, document.documentId]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!file) {
      setError("Selecciona el archivo de reemplazo.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      onReplaced(await documentsApi.replace(document.documentId, file, notes.trim() || undefined));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo reemplazar el documento.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Reemplazar documento"
      description={`${document.documentTypeName} · versión actual V${document.versionNo}.`}
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <Input
          type="file"
          accept=".pdf,.jpg,.jpeg,.png,.xlsx,.xls,.docx,.doc,.txt"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
        <textarea
          className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Notas de la nueva versión..."
        />
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Crear nueva versión</Button>
        </div>
      </form>
    </Dialog>
  );
}
