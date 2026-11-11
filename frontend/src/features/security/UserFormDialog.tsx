import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { clientsApi } from "@/features/clients/clients.api";
import { HttpError } from "@/lib/api/http";
import { securityApi } from "./security.api";
import type { ClientContact } from "@/features/clients/types";
import type { SecurityRole, SecurityUser } from "./types";

const clientRoleCodes = new Set(["CLIENT_ADMIN", "CLIENT_ADMI", "CLIENT_USER"]);

export function UserFormDialog({
  open,
  user,
  roles,
  onClose,
  onSaved
}: {
  open: boolean;
  user?: SecurityUser | null;
  roles: SecurityRole[];
  onClose: () => void;
  onSaved: (user: SecurityUser) => void;
}) {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [userType, setUserType] = useState<"INTERNAL" | "CLIENT">("INTERNAL");
  const [clientId, setClientId] = useState<number | null>(null);
  const [contactId, setContactId] = useState<number | null>(null);
  const [contacts, setContacts] = useState<ClientContact[]>([]);
  const [password, setPassword] = useState("");
  const [mustChangePassword, setMustChangePassword] = useState(true);
  const [roleIds, setRoleIds] = useState<number[]>([]);
  const [clients, setClients] = useState<Array<{ clientId: number; legalName: string }>>([]);
  const [saving, setSaving] = useState(false);
  const [loadingClients, setLoadingClients] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;

    setError("");
    setUsername(user?.username ?? "");
    setEmail(user?.email ?? "");
    setFirstName(user?.firstName ?? "");
    setLastName(user?.lastName ?? "");
    setUserType(user?.userType ?? "INTERNAL");
    setClientId(user?.clientId ?? null);
    setContactId(null);
    setPassword("");
    setMustChangePassword(user?.mustChangePassword ?? true);
    setRoleIds(user?.roles.map((role) => role.roleId) ?? []);

    setLoadingClients(true);
    clientsApi
      .list({ page: 1, pageSize: 100 })
      .then((result) =>
        setClients(result.items.map((x) => ({ clientId: x.clientId, legalName: x.legalName })))
      )
      .catch(() => setError("No se pudo cargar el catálogo de clientes."))
      .finally(() => setLoadingClients(false));
  }, [open, user]);

  useEffect(() => {
    setContacts([]);
    setContactId(null);
    if (!open || userType !== "CLIENT" || !clientId) return;
    let active = true;
    clientsApi.get(clientId).then(result => {
      if (active) setContacts(result.contacts.filter(c => c.isActive));
    }).catch(() => { if (active) setError("No se pudieron cargar los contactos del cliente."); });
    return () => { active = false; };
  }, [open, clientId, userType]);

  const selectContact = (value?: string) => {
    const id = value ? Number(value) : null;
    setContactId(id);
    const contact = contacts.find(c => c.clientContactId === id);
    if (!contact || user) return;
    const parts = contact.fullName.trim().split(/\s+/);
    setFirstName(parts.shift() ?? "");
    setLastName(parts.join(" "));
    setEmail(contact.email ?? "");
    if (contact.email) setUsername(contact.email.split("@")[0].toLowerCase());
  };

  const availableRoles = useMemo(
    () =>
      roles.filter((role) =>
        userType === "CLIENT"
          ? clientRoleCodes.has(role.code)
          : !clientRoleCodes.has(role.code)
      ),
    [roles, userType]
  );

  useEffect(() => {
    const allowed = new Set(availableRoles.map((role) => role.roleId));
    setRoleIds((current) => current.filter((id) => allowed.has(id)));
  }, [availableRoles]);

  const clientOptions = clients.map((client) => ({
    value: String(client.clientId),
    label: client.legalName
  }));

  const toggleRole = (roleId: number) => {
    setRoleIds((current) =>
      current.includes(roleId)
        ? current.filter((id) => id !== roleId)
        : [...current, roleId]
    );
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (!username.trim()) return setError("El username es obligatorio.");
    if (!firstName.trim()) return setError("El nombre es obligatorio.");
    if (userType === "CLIENT" && !clientId) return setError("Selecciona el cliente al que pertenece el usuario.");
    if (!user && userType === "CLIENT" && !contactId) return setError("Selecciona un contacto del cliente.");
    if (roleIds.length === 0) return setError("Selecciona al menos un perfil.");
    if (!user && password.length < 8) return setError("La contraseña inicial debe tener al menos 8 caracteres.");

    setSaving(true);
    try {
      const saved = user
        ? await securityApi.updateUser(user.userId, {
            email: email.trim() || null,
            firstName: firstName.trim() || null,
            lastName: lastName.trim() || null,
            userType,
            clientId: userType === "CLIENT" ? clientId : null,
            roleIds
          })
        : await securityApi.createUser({
            username: username.trim(),
            email: email.trim() || null,
            firstName: firstName.trim() || null,
            lastName: lastName.trim() || null,
            userType,
            clientId: userType === "CLIENT" ? clientId : null,
            password,
            mustChangePassword,
            roleIds
          });

      onSaved(saved);
      onClose();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo guardar el usuario.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={user ? "Editar usuario" : "Nuevo usuario"}
      description={user ? `Actualiza el acceso y perfil de ${user.username}.` : "Crea un usuario interno o un acceso asociado a un cliente."}
      className="sm:max-w-3xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" form="security-user-form" loading={saving}>{user ? "Guardar cambios" : "Crear usuario"}</Button>
        </div>
      }
    >
      <form id="security-user-form" onSubmit={submit} className="space-y-5">
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo de usuario *</span>
            <SearchableSelect
              value={userType}
              onChange={(value) => {
                const next = (value ?? "INTERNAL") as "INTERNAL" | "CLIENT";
                setUserType(next);
                if (next === "INTERNAL") setClientId(null);
              }}
              options={[
                { value: "INTERNAL", label: "Usuario interno", description: "Personal de la empresa operadora" },
                { value: "CLIENT", label: "Usuario cliente", description: "Acceso limitado a una empresa cliente" }
              ]}
            />
          </label>

          {userType === "CLIENT" ? (
            <label className="block">
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Cliente *</span>
              <SearchableSelect
                value={clientId ? String(clientId) : undefined}
                onChange={(value) => setClientId(value ? Number(value) : null)}
                options={clientOptions}
                placeholder={loadingClients ? "Cargando clientes..." : "Seleccionar cliente"}
                searchPlaceholder="Buscar cliente..."
              />
            </label>
          ) : (
            <div className="hidden sm:block" />
          )}

          {userType === "CLIENT" && clientId && !user && (
            <label className="block sm:col-span-2">
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Contacto del cliente *</span>
              <SearchableSelect value={contactId ? String(contactId) : undefined} onChange={selectContact}
                options={contacts.map(c => ({value: String(c.clientContactId), label: c.fullName, description: c.email ?? undefined}))}
                placeholder="Seleccionar contacto registrado" searchPlaceholder="Buscar contacto..." />
            </label>
          )}
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Username *</span>
            <Input value={username} onChange={(event) => setUsername(event.target.value)} disabled={Boolean(user)} placeholder="jperez" />
            {user && <span className="mt-1 block text-xs text-slate-400">El username no se modifica luego de creado.</span>}
          </label>

          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Correo</span>
            <Input type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="usuario@empresa.com" />
          </label>

          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Nombres *</span>
            <Input value={firstName} onChange={(event) => setFirstName(event.target.value)} />
          </label>

          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Apellidos</span>
            <Input value={lastName} onChange={(event) => setLastName(event.target.value)} />
          </label>

          {!user && (
            <>
              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Contraseña inicial *</span>
                <Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Mínimo 8 caracteres" />
              </label>
              <label className="flex items-center gap-2 self-end pb-2 text-sm text-slate-600">
                <input type="checkbox" checked={mustChangePassword} onChange={(event) => setMustChangePassword(event.target.checked)} className="size-4 accent-blue-700" />
                Solicitar cambio de contraseña al usuario
              </label>
            </>
          )}
        </div>

        <div>
          <div className="mb-2 text-sm font-semibold text-slate-700">Perfil de acceso *</div>
          <div className="grid gap-2 sm:grid-cols-2">
            {availableRoles.map((role) => {
              const selected = roleIds.includes(role.roleId);
              return (
                <button
                  key={role.roleId}
                  type="button"
                  onClick={() => toggleRole(role.roleId)}
                  className={`rounded-xl border px-4 py-3 text-left transition ${selected ? "border-blue-300 bg-blue-50 ring-1 ring-blue-200" : "border-slate-200 bg-white hover:bg-slate-50"}`}
                >
                  <div className="flex items-center justify-between gap-2">
                    <div className="text-sm font-semibold text-slate-800">{role.name}</div>
                    <div className={`size-4 rounded-full border ${selected ? "border-blue-700 bg-blue-700 ring-2 ring-white" : "border-slate-300"}`} />
                  </div>
                  <div className="mt-1 text-xs text-slate-500">{role.code}</div>
                </button>
              );
            })}
          </div>
        </div>
      </form>
    </Dialog>
  );
}
