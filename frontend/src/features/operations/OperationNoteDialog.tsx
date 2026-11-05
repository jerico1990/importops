import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { operationsApi } from "./operations.api";

export function OperationNoteDialog({
  open, operationId, onClose, onSaved
}: {
  open: boolean;
  operationId: number;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}) {
  const [types, setTypes] = useState<CodeNameCatalog[]>([]);
  const [noteType, setNoteType] = useState<string>();
  const [noteText, setNoteText] = useState("");
  const [isPinned, setIsPinned] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    catalogsApi.operationNoteTypes().then((data) => {
      setTypes(data);
      setNoteType(data[0]?.code);
    });
    setNoteText("");
    setIsPinned(false);
    setError("");
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!noteType || !noteText.trim()) {
      setError("Tipo y contenido son obligatorios.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await operationsApi.addNote(operationId, {
        noteType,
        noteText: noteText.trim(),
        isPinned
      });
      await onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo registrar la nota.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} title="Agregar nota" onClose={onClose} className="sm:max-w-xl">
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <SearchableSelect
          value={noteType}
          onChange={setNoteType}
          options={types.map((x) => ({ value: x.code, label: x.name }))}
        />
        <textarea
          className="focus-ring min-h-28 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
          value={noteText}
          onChange={(e) => setNoteText(e.target.value)}
          placeholder="Contenido..."
        />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isPinned} onChange={(e) => setIsPinned(e.target.checked)} className="size-4 accent-blue-700" />
          Fijar nota
        </label>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar nota</Button>
        </div>
      </form>
    </Dialog>
  );
}
