import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { operationsApi } from "./operations.api";

export function OperationEventDialog({
  open, operationId, onClose, onSaved
}: {
  open: boolean;
  operationId: number;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}) {
  const [types, setTypes] = useState<CodeNameCatalog[]>([]);
  const [visibilities, setVisibilities] = useState<CodeNameCatalog[]>([]);
  const [eventType, setEventType] = useState<string>();
  const [visibility, setVisibility] = useState<string>();
  const [eventDate, setEventDate] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    Promise.all([
      catalogsApi.operationEventTypes(),
      catalogsApi.recordVisibilities()
    ]).then(([a, b]) => {
      setTypes(a);
      setVisibilities(b);
      setEventType(a[0]?.code);
      setVisibility(b.find((x) => x.code === "BOTH")?.code ?? b[0]?.code);
    });
    setEventDate("");
    setTitle("");
    setDescription("");
    setError("");
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!eventType || !visibility || !title.trim()) {
      setError("Tipo, visibilidad y título son obligatorios.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await operationsApi.addEvent(operationId, {
        eventType,
        visibility,
        eventDate: eventDate || null,
        title: title.trim(),
        description: description.trim() || null
      });
      await onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo registrar el evento.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} title="Registrar evento" onClose={onClose} className="sm:max-w-2xl">
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo *</span>
            <SearchableSelect value={eventType} onChange={setEventType} options={types.map((x) => ({ value: x.code, label: x.name }))} />
          </div>
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Visibilidad *</span>
            <SearchableSelect value={visibility} onChange={setVisibility} options={visibilities.map((x) => ({ value: x.code, label: x.name }))} />
          </div>
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha / hora</span>
            <Input type="datetime-local" value={eventDate} onChange={(e) => setEventDate(e.target.value)} />
          </label>
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Título *</span>
            <Input value={title} onChange={(e) => setTitle(e.target.value)} />
          </label>
        </div>
        <textarea
          className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Descripción..."
        />
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Registrar evento</Button>
        </div>
      </form>
    </Dialog>
  );
}
