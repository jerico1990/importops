import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { HttpError } from "@/lib/api/http";
import { securityApi } from "./security.api";
import type { SecurityUser } from "./types";

export function ResetPasswordDialog({ open, user, onClose, onDone }: {
  open: boolean;
  user?: SecurityUser | null;
  onClose: () => void;
  onDone: () => void;
}) {
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [mustChangePassword, setMustChangePassword] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setPassword("");
    setConfirm("");
    setMustChangePassword(true);
    setError("");
  }, [open]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!user) return;
    setError("");
    if (password.length < 8) return setError("La nueva contraseña debe tener al menos 8 caracteres.");
    if (password !== confirm) return setError("Las contraseñas no coinciden.");

    setSaving(true);
    try {
      await securityApi.resetPassword(user.userId, { newPassword: password, mustChangePassword });
      onDone();
      onClose();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo restablecer la contraseña.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Restablecer contraseña"
      description={user ? `Define una contraseña temporal para ${user.username}.` : undefined}
      className="sm:max-w-lg"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" form="reset-password-form" loading={saving}>Restablecer</Button>
        </div>
      }
    >
      <form id="reset-password-form" onSubmit={submit} className="space-y-4">
        {error && <InlineAlert message={error} />}
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Nueva contraseña</span>
          <Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Confirmar contraseña</span>
          <Input type="password" value={confirm} onChange={(event) => setConfirm(event.target.value)} />
        </label>
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input type="checkbox" className="size-4 accent-blue-700" checked={mustChangePassword} onChange={(event) => setMustChangePassword(event.target.checked)} />
          Solicitar cambio de contraseña al próximo ingreso
        </label>
      </form>
    </Dialog>
  );
}
