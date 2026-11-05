import { Building2, ChevronLeft, ChevronRight, Plus, Search, UserRound, ArrowUpRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { PermissionGate } from "@/features/auth/PermissionGate";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { clientsApi } from "@/features/clients/clients.api";
import { ClientFormDialog } from "@/features/clients/ClientFormDialog";
import type { ClientListItem } from "@/features/clients/types";
import type { PagedResult } from "@/types/common";
import { HttpError } from "@/lib/api/http";

export function ClientsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [data, setData] = useState<PagedResult<ClientListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const id = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 350);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const load = () => {
    setLoading(true); setError("");
    clientsApi.list({ search: search || undefined, page, pageSize })
      .then(setData)
      .catch((err) => setError(err instanceof HttpError ? err.message : "No se pudieron cargar los clientes."))
      .finally(() => setLoading(false));
  };

  useEffect(load, [search, page, pageSize]);

  if (user?.userType === "CLIENT" && user.clientId) {
    return <Navigate to={`/maestros/clientes/${user.clientId}`} replace />;
  }

  const active = data?.items.filter((x) => x.isActive).length ?? 0;
  const inactive = (data?.items.length ?? 0) - active;
  const range = useMemo(() => {
    if (!data || data.total === 0) return "0";
    const from = (data.page - 1) * data.pageSize + 1;
    const to = Math.min(data.page * data.pageSize, data.total);
    return `${from}-${to}`;
  }, [data]);

  return (
    <>
      <PageHeader
        eyebrow="Maestros"
        title="Clientes"
        description="Empresas y personas que originan solicitudes, cotizaciones y operaciones de importación."
        actions={<PermissionGate permission={Permissions.ClientsManage}><Button onClick={() => setCreateOpen(true)}><Plus className="size-4" />Nuevo cliente</Button></PermissionGate>}
      />

      <div className="mb-4 grid gap-3 sm:grid-cols-3">
        <Card className="p-4"><div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Total registrados</div><div className="mt-2 text-2xl font-bold text-slate-950">{data?.total ?? "—"}</div></Card>
        <Card className="p-4"><div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Activos en esta página</div><div className="mt-2 text-2xl font-bold text-emerald-700">{loading ? "—" : active}</div></Card>
        <Card className="p-4"><div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Inactivos en esta página</div><div className="mt-2 text-2xl font-bold text-slate-700">{loading ? "—" : inactive}</div></Card>
      </div>

      <Card className="overflow-hidden">
        <div className="flex flex-col gap-3 border-b border-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="relative w-full max-w-xl"><Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" /><Input value={searchInput} onChange={(e) => setSearchInput(e.target.value)} className="pl-9" placeholder="Buscar por razón social, nombre comercial o RUC..." /></div>
          <select className="focus-ring h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-700 shadow-sm" value={pageSize} onChange={(e) => { setPage(1); setPageSize(Number(e.target.value)); }}><option value={10}>10 por página</option><option value={20}>20 por página</option><option value={50}>50 por página</option></select>
        </div>

        {error && <div className="p-4"><InlineAlert message={error} /></div>}

        {loading ? (
          <div className="space-y-3 p-4">{Array.from({ length: 6 }).map((_, i) => <div key={i} className="grid grid-cols-[1.6fr_1fr_1fr_100px] gap-4"><Skeleton className="h-12"/><Skeleton className="h-12"/><Skeleton className="h-12"/><Skeleton className="h-12"/></div>)}</div>
        ) : data && data.items.length > 0 ? (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[860px] border-collapse">
                <thead><tr className="border-b border-slate-100 bg-slate-50/70">{["Cliente","Identificación","Contacto","Estado",""] .map((h) => <th key={h} className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[.08em] text-slate-500">{h}</th>)}</tr></thead>
                <tbody>{data.items.map((client) => (
                  <tr key={client.clientId} className="cursor-pointer border-b border-slate-100 transition last:border-0 hover:bg-slate-50/70" onClick={() => navigate(`/maestros/clientes/${client.clientId}`)}>
                    <td className="px-4 py-4"><div className="flex items-center gap-3"><div className="grid size-10 place-items-center rounded-xl bg-blue-50 text-blue-700">{client.clientType === "COMPANY" ? <Building2 className="size-5"/> : <UserRound className="size-5"/>}</div><div><div className="font-semibold text-slate-900">{client.legalName}</div><div className="mt-0.5 text-xs text-slate-500">{client.tradeName || (client.clientType === "COMPANY" ? "Empresa" : "Persona")}</div></div></div></td>
                    <td className="px-4 py-4"><div className="text-sm font-medium text-slate-700">{client.taxId || "—"}</div><div className="mt-0.5 text-xs text-slate-400">ID #{client.clientId}</div></td>
                    <td className="px-4 py-4"><div className="text-sm text-slate-700">{client.email || "Sin correo"}</div><div className="mt-0.5 text-xs text-slate-500">{client.phone || "Sin teléfono"}</div></td>
                    <td className="px-4 py-4"><Badge tone={client.isActive ? "green" : "neutral"}>{client.isActive ? "Activo" : "Inactivo"}</Badge></td>
                    <td className="px-4 py-4"><button type="button" className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-blue-700" onClick={(e) => { e.stopPropagation(); navigate(`/maestros/clientes/${client.clientId}`); }}><ArrowUpRight className="size-4"/></button></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
            <div className="divide-y divide-slate-100 md:hidden">{data.items.map((client) => <button key={client.clientId} type="button" onClick={() => navigate(`/maestros/clientes/${client.clientId}`)} className="block w-full p-4 text-left hover:bg-slate-50"><div className="flex items-start justify-between gap-3"><div><div className="font-semibold text-slate-900">{client.legalName}</div><div className="mt-1 text-xs text-slate-500">{client.taxId || "Sin identificación tributaria"}</div></div><Badge tone={client.isActive ? "green" : "neutral"}>{client.isActive ? "Activo" : "Inactivo"}</Badge></div><div className="mt-3 text-sm text-slate-600">{client.email || client.phone || "Sin contacto principal registrado"}</div></button>)}</div>
            <div className="flex flex-col gap-3 border-t border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"><div className="text-xs text-slate-500">Mostrando {range} de {data.total}</div><div className="flex items-center gap-2"><Button variant="secondary" className="px-3" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}><ChevronLeft className="size-4"/>Anterior</Button><span className="min-w-20 text-center text-xs font-medium text-slate-500">Página {data.page} de {Math.max(data.totalPages,1)}</span><Button variant="secondary" className="px-3" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Siguiente<ChevronRight className="size-4"/></Button></div></div>
          </>
        ) : (
          <EmptyState title="No hay clientes para mostrar" description={search ? "No encontramos coincidencias con la búsqueda actual." : "Registra el primer cliente para comenzar el flujo comercial."} action={!search ? <PermissionGate permission={Permissions.ClientsManage}><Button onClick={() => setCreateOpen(true)}><Plus className="size-4"/>Nuevo cliente</Button></PermissionGate> : undefined} />
        )}
      </Card>

      <ClientFormDialog open={createOpen} onClose={() => setCreateOpen(false)} onSaved={(client) => { setCreateOpen(false); navigate(`/maestros/clientes/${client.clientId}`); }} />
    </>
  );
}
