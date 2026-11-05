import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { AppUserCatalog } from "@/features/catalogs/types";
import { InlineAlert } from "@/components/feedback/InlineAlert";

function displayName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function AssignRequestDialog({
  open,
  currentUserId,
  onClose,
  onConfirm
}: {
  open: boolean;
  currentUserId?: number | null;
  onClose: () => void;
  onConfirm: (userId?: number, comment?: string) => Promise<void>;
}) {
  const [users, setUsers] = useState<AppUserCatalog[]>([]);
  const [userId, setUserId] = useState<string>();
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setUserId(currentUserId ? String(currentUserId) : undefined);
    setComment("");
    setError("");
    catalogsApi.requestAssignees()
      .then(setUsers)
      .catch(() => setUsers([]));
  }, [open, currentUserId]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      await onConfirm(userId ? Number(userId) : undefined, comment.trim() || undefined);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo asignar el responsable.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Asignar responsable"
      description="Solo se muestran usuarios internos habilitados para gestionar solicitudes."
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <div>
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Responsable
          </span>
          <SearchableSelect
            value={userId}
            onChange={setUserId}
            options={users.map((u) => ({
              value: String(u.userId),
              label: displayName(u),
              description: u.email ?? u.username
            }))}
            placeholder="Sin asignar"
            searchPlaceholder="Buscar responsable..."
          />
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            Comentario interno
          </span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Motivo de asignación o instrucción operativa."
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar asignación</Button>
        </div>
      </form>
    </Dialog>
  );
}
