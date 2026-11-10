import { useEffect, useState, type FormEvent } from "react";
import { KeyRound, UserRound } from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardHeader } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useAuth } from "@/features/auth/AuthContext";
import { authApi } from "@/lib/api/auth.api";
import { HttpError } from "@/lib/api/http";

const fieldClass = "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100";
const labelClass = "mb-1 block text-xs font-semibold text-slate-600";

export function ProfilePage() {
  const { user, refreshSession } = useAuth();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [saving, setSaving] = useState(false);
  const [changing, setChanging] = useState(false);
  const [profileMessage, setProfileMessage] = useState("");
  const [passwordMessage, setPasswordMessage] = useState("");
  const [profileError, setProfileError] = useState(false);
  const [passwordError, setPasswordError] = useState(false);

  useEffect(() => {
    setFirstName(user?.firstName ?? "");
    setLastName(user?.lastName ?? "");
    setEmail(user?.email ?? "");
  }, [user?.firstName, user?.lastName, user?.email]);

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true); setProfileMessage(""); setProfileError(false);
    try {
      await authApi.updateProfile({ firstName: firstName.trim(), lastName: lastName.trim(), email: email.trim() });
      await refreshSession();
      setProfileMessage("Datos actualizados correctamente.");
    } catch (error) {
      setProfileError(true);
      setProfileMessage(error instanceof HttpError ? error.message : "No se pudo actualizar el perfil.");
    } finally { setSaving(false); }
  };

  const changePassword = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPasswordMessage(""); setPasswordError(false);
    if (newPassword !== confirmPassword) { setPasswordError(true); setPasswordMessage("Las contraseñas nuevas no coinciden."); return; }
    if (newPassword.length < 8 || !/[A-Z]/.test(newPassword) || !/[a-z]/.test(newPassword) || !/[0-9]/.test(newPassword)) {
      setPasswordError(true); setPasswordMessage("La nueva contraseña requiere al menos 8 caracteres, mayúscula, minúscula y número."); return;
    }
    setChanging(true);
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      await refreshSession();
      setCurrentPassword(""); setNewPassword(""); setConfirmPassword("");
      setPasswordMessage("Contraseña actualizada correctamente.");
    } catch (error) {
      setPasswordError(true);
      setPasswordMessage(error instanceof HttpError ? error.message : "No se pudo cambiar la contraseña.");
    } finally { setChanging(false); }
  };

  return (
    <div className="space-y-5">
      <PageHeader eyebrow="R&B Freight Cargo S.A.C. · ImportOps" title="Mi perfil" description="Actualiza tus datos personales y la seguridad de tu cuenta." />
      <div className="grid items-start gap-5 lg:grid-cols-2">
        <Card>
          <CardHeader title="Datos personales" description="Información asociada a tu sesión" action={<UserRound className="size-5 text-blue-700" />} />
          <form onSubmit={saveProfile} className="space-y-4 p-5">
            <div className="grid grid-cols-2 gap-3">
              <div><label className={labelClass} htmlFor="profile-first">Nombres</label><input id="profile-first" className={fieldClass} value={firstName} onChange={e => setFirstName(e.target.value)} maxLength={100} required /></div>
              <div><label className={labelClass} htmlFor="profile-last">Apellidos</label><input id="profile-last" className={fieldClass} value={lastName} onChange={e => setLastName(e.target.value)} maxLength={100} required /></div>
            </div>
            <div><label className={labelClass} htmlFor="profile-email">Correo electrónico</label><input id="profile-email" type="email" className={fieldClass} value={email} onChange={e => setEmail(e.target.value)} maxLength={200} required /></div>
            <div className="grid grid-cols-2 gap-3">
              <div><span className={labelClass}>Usuario</span><div className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">{user?.username ?? "—"}</div></div>
              <div><span className={labelClass}>Empresa (ID)</span><div className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">{user?.companyId ?? "—"}</div></div>
            </div>
            <div><span className={labelClass}>Roles</span><div className="text-sm text-slate-700">{user?.roles.join(", ") || "Sin roles asignados"}</div></div>
            {profileMessage && <p role="status" className={`text-sm ${profileError ? "text-red-600" : "text-green-700"}`}>{profileMessage}</p>}
            <div className="flex justify-end"><Button type="submit" loading={saving}>Guardar cambios</Button></div>
          </form>
        </Card>
        <Card>
          <CardHeader title="Cambiar contraseña" description="Verifica tu contraseña actual para establecer una nueva" action={<KeyRound className="size-5 text-blue-700" />} />
          <form onSubmit={changePassword} className="space-y-4 p-5">
            {user?.mustChangePassword && <p className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">Debes cambiar tu contraseña temporal.</p>}
            <div><label className={labelClass} htmlFor="profile-current">Contraseña actual</label><input id="profile-current" type="password" autoComplete="current-password" className={fieldClass} value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} required /></div>
            <div><label className={labelClass} htmlFor="profile-new">Nueva contraseña</label><input id="profile-new" type="password" autoComplete="new-password" className={fieldClass} value={newPassword} onChange={e => setNewPassword(e.target.value)} required minLength={8} /></div>
            <div><label className={labelClass} htmlFor="profile-confirm">Confirmar nueva contraseña</label><input id="profile-confirm" type="password" autoComplete="new-password" className={fieldClass} value={confirmPassword} onChange={e => setConfirmPassword(e.target.value)} required minLength={8} /></div>
            <p className="text-xs text-slate-500">Mínimo 8 caracteres, incluyendo mayúscula, minúscula y número.</p>
            {passwordMessage && <p role="status" className={`text-sm ${passwordError ? "text-red-600" : "text-green-700"}`}>{passwordMessage}</p>}
            <div className="flex justify-end"><Button type="submit" loading={changing}>Actualizar contraseña</Button></div>
          </form>
        </Card>
      </div>
    </div>
  );
}
