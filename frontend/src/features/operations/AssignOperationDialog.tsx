import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { AppUserCatalog } from "@/features/catalogs/types";

function displayName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function AssignOperationDialog({
  open, currentUserId, onClose, onConfirm
}: {
  open: boolean;
  currentUserId: number;
  onClose: () => void;
  onConfirm: (userId: number, comment?: string) => Promise<void>;
}) {
  const [users, setUsers] = useState<AppUserCatalog[]>([]);
  const [userId, setUserId] = useState(String(currentUserId));
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setUserId(String(currentUserId));
    setComment("");
    setError("");
    catalogsApi.operationResponsibles().then(setUsers).catch(() => setUsers([]));
  }, [open, currentUserId]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!userId) {
      setError("Selecciona un responsable.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await onConfirm(Number(userId), comment.trim() || undefined);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo reasignar.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} title="Reasignar operación" onClose={onClose} className="sm:max-w-xl">
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <SearchableSelect
          value={userId}
          onChange={(v) => setUserId(v ?? "")}
          options={users.map((u) => ({
            value: String(u.userId),
            label: displayName(u),
            description: u.email ?? u.username
          }))}
          placeholder="Seleccionar responsable"
        />
        <textarea
          className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder="Comentario de reasignación..."
        />
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar</Button>
        </div>
      </form>
    </Dialog>
  );
}
