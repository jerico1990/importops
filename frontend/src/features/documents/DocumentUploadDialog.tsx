import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { DocumentType, CodeNameCatalog } from "@/features/catalogs/types";
import { documentsApi } from "./documents.api";
import type { DocumentRecord } from "./types";

export function DocumentUploadDialog({
  open,
  operationId,
  preferredDocumentTypeId,
  clientMode,
  onClose,
  onUploaded
}: {
  open: boolean;
  operationId: number;
  preferredDocumentTypeId?: number | null;
  clientMode: boolean;
  onClose: () => void;
  onUploaded: (document: DocumentRecord) => void;
}) {
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [visibilities, setVisibilities] = useState<CodeNameCatalog[]>([]);
  const [documentTypeId, setDocumentTypeId] = useState<string>();
  const [visibility, setVisibility] = useState<string>("BOTH");
  const [file, setFile] = useState<File | null>(null);
  const [documentDate, setDocumentDate] = useState("");
  const [expirationDate, setExpirationDate] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    Promise.all([
      catalogsApi.documentTypes(),
      catalogsApi.recordVisibilities()
    ]).then(([typeData, visibilityData]) => {
      setTypes(typeData);
      setVisibilities(visibilityData);
    }).catch(() => {});
    setDocumentTypeId(preferredDocumentTypeId ? String(preferredDocumentTypeId) : undefined);
    setVisibility("BOTH");
    setFile(null);
    setDocumentDate("");
    setExpirationDate("");
    setNotes("");
    setError("");
  }, [open, preferredDocumentTypeId]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!documentTypeId || !file) {
      setError("Selecciona el tipo documental y el archivo.");
      return;
    }
    if (documentDate && expirationDate && expirationDate < documentDate) {
      setError("La fecha de vencimiento no puede ser anterior a la fecha del documento.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      const uploaded = await documentsApi.upload(operationId, {
        file,
        documentTypeId: Number(documentTypeId),
        documentDate: documentDate || undefined,
        expirationDate: expirationDate || undefined,
        notes: notes.trim() || undefined,
        visibility: clientMode ? "BOTH" : visibility
      });
      onUploaded(uploaded);
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
      description="Cada carga genera una versión documental controlada por el backend."
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div>
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo documental *</span>
          <SearchableSelect
            value={documentTypeId}
            onChange={setDocumentTypeId}
            options={types.map((x) => ({
              value: String(x.documentTypeId),
              label: x.name,
              description: `${x.category} · ${x.code}`
            }))}
            placeholder="Seleccionar tipo"
            searchPlaceholder="Buscar tipo documental..."
          />
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Archivo *</span>
          <Input
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,.xlsx,.xls,.docx,.doc,.txt"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
          <span className="mt-1 block text-xs text-slate-400">Máximo 25 MB. El backend valida extensión, MIME y firma binaria.</span>
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
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Visibilidad *</span>
            <SearchableSelect
              value={visibility}
              onChange={(v) => setVisibility(v ?? "BOTH")}
              options={visibilities.map((x) => ({ value: x.code, label: x.name }))}
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
