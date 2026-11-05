import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { DocumentType } from "@/features/catalogs/types";
import { requestsApi } from "./requests.api";

export function RequestDocumentUploadDialog({
  open,
  requestId,
  clientMode,
  onClose,
  onUploaded
}: {
  open: boolean;
  requestId: number;
  clientMode: boolean;
  onClose: () => void;
  onUploaded: () => void;
}) {
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [typeId, setTypeId] = useState<string>();
  const [file, setFile] = useState<File | null>(null);
  const [documentDate, setDocumentDate] = useState("");
  const [expirationDate, setExpirationDate] = useState("");
  const [notes, setNotes] = useState("");
  const [visibility, setVisibility] = useState<"CLIENT" | "BOTH">("BOTH");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setTypeId(undefined);
    setFile(null);
    setDocumentDate("");
    setExpirationDate("");
    setNotes("");
    setVisibility("BOTH");
    setError("");
    catalogsApi.documentTypes().then(setTypes).catch(() => setTypes([]));
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!file || !typeId) {
      setError("Selecciona el tipo documental y el archivo.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      await requestsApi.uploadDocument(requestId, {
        file,
        documentTypeId: Number(typeId),
        documentDate: documentDate || undefined,
        expirationDate: expirationDate || undefined,
        notes: notes.trim() || undefined,
        visibility: clientMode ? "BOTH" : visibility
      });
      onUploaded();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar el documento.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Cargar documento"
      description="El archivo quedará relacionado directamente con la solicitud."
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div>
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Tipo documental *
          </span>
          <SearchableSelect
            value={typeId}
            onChange={setTypeId}
            options={types.map((x) => ({
              value: String(x.documentTypeId),
              label: x.name,
              description: `${x.category} · ${x.code}`
            }))}
            placeholder="Seleccionar tipo"
            searchPlaceholder="Buscar documento..."
          />
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Archivo *
          </span>
          <Input
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,.xlsx,.docx,.xls,.doc,.txt"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha documento</span>
            <Input type="date" value={documentDate} onChange={(e) => setDocumentDate(e.target.value)} />
          </label>
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Vencimiento</span>
            <Input type="date" value={expirationDate} onChange={(e) => setExpirationDate(e.target.value)} />
          </label>
        </div>

        {!clientMode && (
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Visibilidad</span>
            <SearchableSelect
              value={visibility}
              onChange={(value) => setVisibility((value as "CLIENT" | "BOTH") ?? "BOTH")}
              options={[
                { value: "BOTH", label: "Interno + Cliente" },
                { value: "CLIENT", label: "Cliente" }
              ]}
            />
          </div>
        )}

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas</span>
          <textarea
            className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Cargar documento</Button>
        </div>
      </form>
    </Dialog>
  );
}
