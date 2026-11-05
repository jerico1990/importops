import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { AppUserCatalog } from "@/features/catalogs/types";
import { operationsApi } from "./operations.api";
import type { OperationDetail } from "./types";

function displayName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function CreateOperationDialog({
  open, quotationId, quotationNo, onClose, onCreated
}: {
  open: boolean;
  quotationId: number;
  quotationNo: string;
  onClose: () => void;
  onCreated: (operation: OperationDetail) => void;
}) {
  const [users, setUsers] = useState<AppUserCatalog[]>([]);
  const [responsibleUserId, setResponsibleUserId] = useState<string>();
  const [estimatedCloseDate, setEstimatedCloseDate] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setResponsibleUserId(undefined);
    setEstimatedCloseDate("");
    setNotes("");
    setError("");
    catalogsApi.operationResponsibles().then(setUsers).catch(() => setUsers([]));
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!responsibleUserId) {
      setError("Selecciona el responsable operativo.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      const result = await operationsApi.createFromQuotation(quotationId, {
        responsibleUserId: Number(responsibleUserId),
        estimatedCloseDate: estimatedCloseDate || null,
        notes: notes.trim() || null
      });
      onCreated(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo crear la operación.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Crear operación"
      description={`Apertura operativa desde ${quotationNo}.`}
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <div>
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Responsable operativo *
          </span>
          <SearchableSelect
            value={responsibleUserId}
            onChange={setResponsibleUserId}
            options={users.map((u) => ({
              value: String(u.userId),
              label: displayName(u),
              description: u.email ?? u.username
            }))}
            placeholder="Seleccionar responsable"
            searchPlaceholder="Buscar responsable..."
          />
        </div>
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Fecha estimada de cierre
          </span>
          <Input
            type="date"
            value={estimatedCloseDate}
            onChange={(e) => setEstimatedCloseDate(e.target.value)}
          />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Notas internas de apertura
          </span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Crear operación</Button>
        </div>
      </form>
    </Dialog>
  );
}
