import { KeyRound, Pencil, Plus, Search, ShieldCheck, UserCheck, UserRoundCog, UserX } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card, CardHeader } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { Skeleton } from "@/components/ui/Skeleton";
import { HttpError } from "@/lib/api/http";
import { securityApi } from "@/features/security/security.api";
import { UserFormDialog } from "@/features/security/UserFormDialog";
import { ResetPasswordDialog } from "@/features/security/ResetPasswordDialog";
import type { SecurityRole, SecurityUser } from "@/features/security/types";

const typeOptions = [
  { value: "INTERNAL", label: "Internos" },
  { value: "CLIENT", label: "Clientes" }
];

const statusOptions = [
  { value: "true", label: "Activos" },
  { value: "false", label: "Inactivos" }
];

export function UsersPage() {
  const [users, setUsers] = useState<SecurityUser[]>([]);
  const [roles, setRoles] = useState<SecurityRole[]>([]);
  const [search, setSearch] = useState("");
  const [userType, setUserType] = useState<string>();
  const [isActive, setIsActive] = useState<string | undefined>("true");
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<SecurityUser | null>(null);
  const [resetUser, setResetUser] = useState<SecurityUser | null>(null);
  const [changingUserId, setChangingUserId] = useState<number | null>(null);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const [userResult, roleResult] = await Promise.all([
        securityApi.users({
          search: search.trim() || undefined,
          userType,
          isActive: isActive === undefined ? undefined : isActive === "true",
          page,
          pageSize: 20
        }),
        securityApi.roles()
      ]);
      setUsers(userResult.items);
      setTotal(userResult.total);
      setTotalPages(Math.max(userResult.totalPages, 1));
      setRoles(roleResult);
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudieron cargar los usuarios.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 250);
    return () => window.clearTimeout(timer);
  }, [search, userType, isActive, page]);

  useEffect(() => setPage(1), [search, userType, isActive]);

  const counts = useMemo(() => ({
    internal: users.filter((x) => x.userType === "INTERNAL").length,
    client: users.filter((x) => x.userType === "CLIENT").length,
    active: users.filter((x) => x.isActive).length
  }), [users]);

  const setActive = async (user: SecurityUser) => {
    setChangingUserId(user.userId);
    setError("");
    try {
      await securityApi.setActive(user.userId, !user.isActive);
      await load();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo cambiar el estado del usuario.");
    } finally {
      setChangingUserId(null);
    }
  };

  return (
    <>
      <PageHeader
        eyebrow="Administración"
        title="Usuarios y accesos"
        description="Administra usuarios internos y accesos de clientes. Los perfiles determinan qué módulos pueden utilizar; los usuarios CLIENT además quedan restringidos a su client_id."
        actions={
          <Button onClick={() => { setEditing(null); setFormOpen(true); }}>
            <Plus className="size-4" />
            Nuevo usuario
          </Button>
        }
      />

      <div className="mb-4 grid gap-3 sm:grid-cols-3">
        <Card className="p-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-2xl font-bold text-slate-950">{total}</div>
              <div className="mt-1 text-sm font-semibold text-slate-700">Usuarios encontrados</div>
            </div>
            <div className="grid size-10 place-items-center rounded-xl bg-blue-50 text-blue-700"><UserRoundCog className="size-5" /></div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-2xl font-bold text-slate-950">{counts.internal}</div>
              <div className="mt-1 text-sm font-semibold text-slate-700">Internos en esta página</div>
            </div>
            <div className="grid size-10 place-items-center rounded-xl bg-violet-50 text-violet-700"><ShieldCheck className="size-5" /></div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-2xl font-bold text-slate-950">{counts.client}</div>
              <div className="mt-1 text-sm font-semibold text-slate-700">Clientes en esta página</div>
            </div>
            <div className="grid size-10 place-items-center rounded-xl bg-emerald-50 text-emerald-700"><UserCheck className="size-5" /></div>
          </div>
        </Card>
      </div>

      <Card className="overflow-visible">
        <CardHeader
          title="Directorio de usuarios"
          description="Un usuario CLIENT debe pertenecer a un cliente y solo puede recibir perfiles CLIENT_ADMIN o CLIENT_USER."
        />

        <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-[1.5fr_1fr_1fr]">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input value={search} onChange={(event) => setSearch(event.target.value)} className="pl-9" placeholder="Usuario, correo, nombre..." />
          </div>
          <SearchableSelect value={userType} onChange={setUserType} options={typeOptions} placeholder="Todos los tipos" />
          <SearchableSelect value={isActive} onChange={setIsActive} options={statusOptions} placeholder="Todos los estados" />
        </div>

        {error && <div className="p-4"><InlineAlert message={error} /></div>}

        {loading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, index) => <Skeleton key={index} className="h-16" />)}
          </div>
        ) : users.length === 0 ? (
          <div className="px-5 py-14 text-center text-sm text-slate-500">No hay usuarios para los filtros seleccionados.</div>
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="min-w-[980px] w-full border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {['Usuario', 'Tipo', 'Cliente', 'Perfil', 'Estado', 'Acciones'].map((header) => (
                      <th key={header} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500">{header}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.userId} className="border-b border-slate-100 last:border-0 hover:bg-slate-50/60">
                      <td className="px-4 py-4">
                        <div className="font-semibold text-slate-900">{[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}</div>
                        <div className="mt-1 text-xs text-slate-500">{user.username}{user.email ? ` · ${user.email}` : ''}</div>
                      </td>
                      <td className="px-4 py-4"><Badge tone={user.userType === 'CLIENT' ? 'green' : 'blue'}>{user.userType === 'CLIENT' ? 'Cliente' : 'Interno'}</Badge></td>
                      <td className="px-4 py-4 text-sm text-slate-700">{user.clientName ?? '—'}</td>
                      <td className="px-4 py-4"><div className="flex flex-wrap gap-1.5">{user.roles.map((role) => <Badge key={role.roleId} tone="neutral">{role.name}</Badge>)}</div></td>
                      <td className="px-4 py-4"><Badge tone={user.isActive ? 'green' : 'neutral'}>{user.isActive ? 'Activo' : 'Inactivo'}</Badge></td>
                      <td className="px-4 py-4">
                        <div className="flex items-center gap-1">
                          <button type="button" className="focus-ring rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-blue-700" title="Editar" onClick={() => { setEditing(user); setFormOpen(true); }}><Pencil className="size-4" /></button>
                          <button type="button" className="focus-ring rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-blue-700" title="Restablecer contraseña" onClick={() => setResetUser(user)}><KeyRound className="size-4" /></button>
                          <button type="button" disabled={changingUserId === user.userId} className="focus-ring rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-red-600 disabled:opacity-40" title={user.isActive ? 'Desactivar' : 'Activar'} onClick={() => void setActive(user)}>{user.isActive ? <UserX className="size-4" /> : <UserCheck className="size-4" />}</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {users.map((user) => (
                <div key={user.userId} className="p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">{[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}</div>
                      <div className="mt-1 text-xs text-slate-500">{user.username}</div>
                    </div>
                    <Badge tone={user.isActive ? 'green' : 'neutral'}>{user.isActive ? 'Activo' : 'Inactivo'}</Badge>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">{user.userType === 'CLIENT' ? user.clientName : 'Usuario interno'}</div>
                  <div className="mt-2 flex flex-wrap gap-1.5">{user.roles.map((role) => <Badge key={role.roleId}>{role.name}</Badge>)}</div>
                  <div className="mt-4 flex gap-2">
                    <Button variant="secondary" className="flex-1" onClick={() => { setEditing(user); setFormOpen(true); }}><Pencil className="size-4" />Editar</Button>
                    <Button variant="ghost" onClick={() => setResetUser(user)}><KeyRound className="size-4" /></Button>
                  </div>
                </div>
              ))}
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 px-4 py-3">
              <div className="text-xs text-slate-500">Página {page} de {totalPages}</div>
              <div className="flex gap-2">
                <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((x) => x - 1)}>Anterior</Button>
                <Button variant="secondary" disabled={page >= totalPages} onClick={() => setPage((x) => x + 1)}>Siguiente</Button>
              </div>
            </div>
          </>
        )}
      </Card>

      <UserFormDialog
        open={formOpen}
        user={editing}
        roles={roles}
        onClose={() => { setFormOpen(false); setEditing(null); }}
        onSaved={() => void load()}
      />

      <ResetPasswordDialog
        open={Boolean(resetUser)}
        user={resetUser}
        onClose={() => setResetUser(null)}
        onDone={() => void load()}
      />
    </>
  );
}
