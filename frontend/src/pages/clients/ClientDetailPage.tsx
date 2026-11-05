import { ArrowLeft, Building2, CalendarClock, Edit3, Mail, MapPin, Phone, Plus, RefreshCw, ShieldCheck, UserRound, UsersRound } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Card, CardHeader } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Tabs } from "@/components/ui/Tabs";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { PermissionGate } from "@/features/auth/PermissionGate";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { clientsApi } from "@/features/clients/clients.api";
import { ClientFormDialog } from "@/features/clients/ClientFormDialog";
import { ContactFormDialog } from "@/features/clients/ContactFormDialog";
import { AddressFormDialog } from "@/features/clients/AddressFormDialog";
import type { ClientAddress, ClientContact, ClientDetail, ImportRequestSummary, OperationSummary } from "@/features/clients/types";
import type { PagedResult } from "@/types/common";
import { HttpError } from "@/lib/api/http";
import { formatDate } from "@/lib/format";

const addressLabels: Record<string,string> = { LEGAL: "Legal / Fiscal", BILLING: "Facturación", DELIVERY: "Entrega", OTHER: "Otra" };
const requestTones: Record<string, "neutral"|"blue"|"green"|"amber"|"red"|"violet"> = { DRAFT:"neutral", SUBMITTED:"blue", IN_REVIEW:"amber", APPROVED:"green", REJECTED:"red", CANCELLED:"neutral" };

export function ClientDetailPage() {
  const { id } = useParams();
  const clientId = Number(id);
  const navigate = useNavigate();
  const { user, hasPermission } = useAuth();
  const [client, setClient] = useState<ClientDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [tab, setTab] = useState("general");
  const [editOpen, setEditOpen] = useState(false);
  const [contactOpen, setContactOpen] = useState(false);
  const [editingContact, setEditingContact] = useState<ClientContact | null>(null);
  const [addressOpen, setAddressOpen] = useState(false);
  const [editingAddress, setEditingAddress] = useState<ClientAddress | null>(null);
  const [mutating, setMutating] = useState(false);
  const [requests, setRequests] = useState<PagedResult<ImportRequestSummary> | null>(null);
  const [operations, setOperations] = useState<PagedResult<OperationSummary> | null>(null);
  const [relatedLoading, setRelatedLoading] = useState(false);

  const load = useCallback(() => {
    if (!Number.isFinite(clientId) || clientId <= 0) return;
    setLoading(true); setError("");
    clientsApi.get(clientId).then(setClient).catch((err) => setError(err instanceof HttpError ? err.message : "No se pudo cargar el cliente.")).finally(() => setLoading(false));
  }, [clientId]);
  useEffect(load, [load]);

  useEffect(() => {
    if (tab === "solicitudes" && !requests) {
      setRelatedLoading(true); clientsApi.requests(clientId).then(setRequests).catch((err) => setError(err instanceof HttpError ? err.message : "No se pudieron cargar las solicitudes.")).finally(() => setRelatedLoading(false));
    }
    if (tab === "operaciones" && !operations) {
      setRelatedLoading(true); clientsApi.operations(clientId).then(setOperations).catch((err) => setError(err instanceof HttpError ? err.message : "No se pudieron cargar las operaciones.")).finally(() => setRelatedLoading(false));
    }
  }, [tab, requests, operations, clientId]);

  const clientMode = user?.userType === "CLIENT";
  const primaryContact = client?.contacts.find((x) => x.isPrimary && x.isActive);
  const primaryAddress = client?.addresses.find((x) => x.isPrimary && x.isActive);

  const toggleActive = async () => {
    if (!client) return;
    const next = !client.isActive;
    if (!window.confirm(next ? "¿Reactivar este cliente?" : "¿Desactivar este cliente? No podrá utilizarse en nuevos procesos mientras esté inactivo.")) return;
    setMutating(true); setError("");
    try {
      const updated = await clientsApi.update(client.clientId, {
        clientType: client.clientType, legalName: client.legalName, tradeName: client.tradeName, taxId: client.taxId,
        countryCode: client.countryCode, email: client.email, phone: client.phone, creditDays: client.creditDays,
        preferredCurrencyCode: client.preferredCurrencyCode, notes: client.notes, accountManagerUserId: client.accountManagerUserId,
        isActive: next, rowVersion: client.rowVersion
      });
      setClient(updated);
    } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo cambiar el estado del cliente."); }
    finally { setMutating(false); }
  };

  const deactivateContact = async (contact: ClientContact) => {
    if (!window.confirm(`¿Desactivar a ${contact.fullName}?`)) return;
    setMutating(true); try { await clientsApi.deactivateContact(clientId, contact.clientContactId); load(); } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo desactivar el contacto."); } finally { setMutating(false); }
  };
  const reactivateContact = async (contact: ClientContact) => {
    setMutating(true); try { await clientsApi.updateContact(clientId, contact.clientContactId, { fullName: contact.fullName, positionName: contact.positionName, email: contact.email, phone: contact.phone, mobile: contact.mobile, isPrimary: false, isActive: true }); load(); } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo reactivar el contacto."); } finally { setMutating(false); }
  };
  const deactivateAddress = async (address: ClientAddress) => {
    if (!window.confirm("¿Desactivar esta dirección?")) return;
    setMutating(true); try { await clientsApi.deactivateAddress(clientId, address.clientAddressId); load(); } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo desactivar la dirección."); } finally { setMutating(false); }
  };
  const reactivateAddress = async (address: ClientAddress) => {
    setMutating(true); try { await clientsApi.updateAddress(clientId, address.clientAddressId, { addressType: address.addressType, addressLine: address.addressLine, district: address.district, province: address.province, department: address.department, countryCode: address.countryCode, isPrimary: false, isActive: true }); load(); } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo reactivar la dirección."); } finally { setMutating(false); }
  };

  const tabs = useMemo(() => [
    { value: "general", label: "General" },
    { value: "contactos", label: "Contactos", count: client?.contacts.filter((x) => x.isActive).length ?? 0 },
    { value: "direcciones", label: "Direcciones", count: client?.addresses.filter((x) => x.isActive).length ?? 0 },
    { value: "solicitudes", label: "Solicitudes" },
    { value: "operaciones", label: "Operaciones" }
  ], [client]);

  if (loading) return <div className="space-y-4"><Skeleton className="h-24"/><Skeleton className="h-16"/><Skeleton className="h-96"/></div>;
  if (!client) return <Card>{error ? <div className="p-5"><InlineAlert message={error}/></div> : <EmptyState title="Cliente no encontrado"/>}</Card>;

  return (
    <>
      <div className="mb-5">
        {!clientMode && <button type="button" onClick={() => navigate("/maestros/clientes")} className="mb-3 inline-flex items-center gap-1.5 text-sm font-semibold text-slate-500 hover:text-blue-700"><ArrowLeft className="size-4"/>Clientes</button>}
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex min-w-0 items-start gap-3">
            <div className="grid size-12 shrink-0 place-items-center rounded-2xl bg-blue-50 text-blue-700">{client.clientType === "COMPANY" ? <Building2 className="size-6"/> : <UserRound className="size-6"/>}</div>
            <div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><h1 className="truncate text-2xl font-bold tracking-tight text-slate-950">{clientMode ? "Mi empresa" : client.legalName}</h1><Badge tone={client.isActive ? "green" : "neutral"}>{client.isActive ? "Activo" : "Inactivo"}</Badge></div>{clientMode && <div className="mt-1 text-base font-semibold text-slate-700">{client.legalName}</div>}<div className="mt-1 text-sm text-slate-500">{client.taxId ? `RUC / ID: ${client.taxId}` : "Sin identificación tributaria"}{client.tradeName ? ` · ${client.tradeName}` : ""}</div></div>
          </div>
          <PermissionGate permission={Permissions.ClientsManage}><div className="flex gap-2"><Button variant="secondary" onClick={toggleActive} loading={mutating}>{client.isActive ? "Desactivar" : "Reactivar"}</Button><Button onClick={() => setEditOpen(true)}><Edit3 className="size-4"/>Editar</Button></div></PermissionGate>
        </div>
      </div>

      {error && <div className="mb-4"><InlineAlert message={error}/></div>}

      <Card className="overflow-hidden"><Tabs value={tab} onChange={setTab} items={tabs}/>
        {tab === "general" && <div className="grid gap-5 p-5 lg:grid-cols-[1.3fr_.7fr]">
          <div className="grid gap-4 sm:grid-cols-2">
            <Info icon={<Building2 className="size-4"/>} label="Tipo" value={client.clientType === "COMPANY" ? "Empresa" : "Persona"}/>
            <Info icon={<ShieldCheck className="size-4"/>} label="País" value={client.countryCode}/>
            <Info icon={<Mail className="size-4"/>} label="Correo principal" value={client.email || "No registrado"}/>
            <Info icon={<Phone className="size-4"/>} label="Teléfono" value={client.phone || "No registrado"}/>
            <Info icon={<CalendarClock className="size-4"/>} label="Crédito" value={client.creditDays !== null && client.creditDays !== undefined ? `${client.creditDays} días` : "No definido"}/>
            <Info icon={<ShieldCheck className="size-4"/>} label="Moneda preferida" value={client.preferredCurrencyCode || "No definida"}/>
            <div className="sm:col-span-2 rounded-xl border border-slate-200 bg-slate-50/60 p-4"><div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Notas</div><div className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">{client.notes || "Sin notas registradas."}</div></div>
          </div>
          <div className="space-y-3">
            <Card className="p-4"><div className="flex items-center gap-2 text-sm font-semibold text-slate-800"><UsersRound className="size-4 text-blue-700"/>Contacto principal</div>{primaryContact ? <div className="mt-3"><div className="font-semibold text-slate-900">{primaryContact.fullName}</div><div className="mt-1 text-sm text-slate-500">{primaryContact.positionName || "Sin cargo"}</div><div className="mt-2 text-xs text-slate-500">{primaryContact.email || primaryContact.mobile || primaryContact.phone || "Sin datos de contacto"}</div></div> : <div className="mt-3 text-sm text-slate-500">No existe contacto principal activo.</div>}</Card>
            <Card className="p-4"><div className="flex items-center gap-2 text-sm font-semibold text-slate-800"><MapPin className="size-4 text-blue-700"/>Dirección principal</div>{primaryAddress ? <div className="mt-3"><div className="text-sm font-medium leading-6 text-slate-800">{primaryAddress.addressLine}</div><div className="mt-1 text-xs text-slate-500">{[primaryAddress.district, primaryAddress.province, primaryAddress.department, primaryAddress.countryCode].filter(Boolean).join(" · ")}</div></div> : <div className="mt-3 text-sm text-slate-500">No existe dirección principal activa.</div>}</Card>
          </div>
        </div>}

        {tab === "contactos" && <section><CardHeader title="Contactos" description="Personas de contacto comerciales y operativas del cliente." action={<PermissionGate permission={Permissions.ClientsManage}><Button onClick={() => { setEditingContact(null); setContactOpen(true); }}><Plus className="size-4"/>Nuevo contacto</Button></PermissionGate>}/>{client.contacts.length === 0 ? <EmptyState title="Sin contactos registrados"/> : <div className="divide-y divide-slate-100">{client.contacts.map((c) => <div key={c.clientContactId} className="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center sm:justify-between"><div><div className="flex flex-wrap items-center gap-2"><span className="font-semibold text-slate-900">{c.fullName}</span>{c.isPrimary && <Badge tone="blue">Principal</Badge>}{!c.isActive && <Badge>Inactivo</Badge>}</div><div className="mt-1 text-sm text-slate-500">{c.positionName || "Sin cargo"}</div><div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500"><span>{c.email || "Sin correo"}</span><span>{c.mobile || c.phone || "Sin teléfono"}</span></div></div><PermissionGate permission={Permissions.ClientsManage}><div className="flex gap-2"><Button variant="secondary" onClick={() => { setEditingContact(c); setContactOpen(true); }}>Editar</Button><Button variant="ghost" disabled={mutating} onClick={() => c.isActive ? deactivateContact(c) : reactivateContact(c)}>{c.isActive ? "Desactivar" : "Reactivar"}</Button></div></PermissionGate></div>)}</div>}</section>}

        {tab === "direcciones" && <section><CardHeader title="Direcciones" description="Direcciones legales, de facturación y entrega." action={<PermissionGate permission={Permissions.ClientsManage}><Button onClick={() => { setEditingAddress(null); setAddressOpen(true); }}><Plus className="size-4"/>Nueva dirección</Button></PermissionGate>}/>{client.addresses.length === 0 ? <EmptyState title="Sin direcciones registradas"/> : <div className="grid gap-3 p-5 md:grid-cols-2">{client.addresses.map((a) => <Card key={a.clientAddressId} className="p-4"><div className="flex items-start justify-between gap-3"><div><div className="flex flex-wrap items-center gap-2"><span className="text-xs font-bold uppercase tracking-wider text-blue-700">{addressLabels[a.addressType]}</span>{a.isPrimary && <Badge tone="blue">Principal</Badge>}{!a.isActive && <Badge>Inactiva</Badge>}</div><div className="mt-3 text-sm font-semibold leading-6 text-slate-900">{a.addressLine}</div><div className="mt-1 text-xs leading-5 text-slate-500">{[a.district,a.province,a.department,a.countryCode].filter(Boolean).join(" · ")}</div></div><MapPin className="size-5 shrink-0 text-slate-300"/></div><PermissionGate permission={Permissions.ClientsManage}><div className="mt-4 flex gap-2 border-t border-slate-100 pt-3"><Button variant="secondary" onClick={() => { setEditingAddress(a); setAddressOpen(true); }}>Editar</Button><Button variant="ghost" disabled={mutating} onClick={() => a.isActive ? deactivateAddress(a) : reactivateAddress(a)}>{a.isActive ? "Desactivar" : "Reactivar"}</Button></div></PermissionGate></Card>)}</div>}</section>}

        {tab === "solicitudes" && <RelatedRequests loading={relatedLoading} data={requests} onOpen={(requestId) => navigate(`/solicitudes/${requestId}`)}/>} 
        {tab === "operaciones" && <RelatedOperations loading={relatedLoading} data={operations} onOpen={(operationId) => navigate(`/operaciones/${operationId}`)}/>} 
      </Card>

      <ClientFormDialog open={editOpen} client={client} onClose={() => setEditOpen(false)} onSaved={(value) => setClient(value)} />
      <ContactFormDialog open={contactOpen} clientId={clientId} contact={editingContact} onClose={() => setContactOpen(false)} onSaved={load} />
      <AddressFormDialog open={addressOpen} clientId={clientId} address={editingAddress} onClose={() => setAddressOpen(false)} onSaved={load} />
    </>
  );
}

function Info({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) { return <div className="rounded-xl border border-slate-200 bg-white p-4"><div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-slate-500">{icon}{label}</div><div className="mt-2 text-sm font-semibold text-slate-900">{value}</div></div>; }

function RelatedRequests({ loading, data, onOpen }: { loading: boolean; data: PagedResult<ImportRequestSummary> | null; onOpen: (id:number)=>void }) {
  if (loading) return <div className="space-y-3 p-5">{Array.from({length:4}).map((_,i)=><Skeleton key={i} className="h-16"/>)}</div>;
  if (!data || data.items.length===0) return <EmptyState title="Este cliente aún no tiene solicitudes" description="Las solicitudes creadas para este cliente aparecerán aquí."/>;
  return <div className="divide-y divide-slate-100">{data.items.map((r)=><button key={r.requestId} type="button" onClick={()=>onOpen(r.requestId)} className="flex w-full flex-col gap-2 px-5 py-4 text-left hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"><div><div className="font-semibold text-slate-900">{r.requestNo}</div><div className="mt-1 line-clamp-1 text-sm text-slate-500">{r.merchandiseDescription}</div></div><div className="flex items-center gap-3"><Badge tone={requestTones[r.status] ?? "neutral"}>{r.status}</Badge><span className="text-xs text-slate-500">{formatDate(r.requestDate)}</span></div></button>)}</div>;
}

function RelatedOperations({ loading, data, onOpen }: { loading: boolean; data: PagedResult<OperationSummary> | null; onOpen: (id:number)=>void }) {
  if (loading) return <div className="space-y-3 p-5">{Array.from({length:4}).map((_,i)=><Skeleton key={i} className="h-16"/>)}</div>;
  if (!data || data.items.length===0) return <EmptyState title="Este cliente aún no tiene operaciones" description="Cuando una cotización aprobada genere una operación, aparecerá aquí."/>;
  return <div className="divide-y divide-slate-100">{data.items.map((o)=><button key={o.operationId} type="button" onClick={()=>onOpen(o.operationId)} className="flex w-full flex-col gap-2 px-5 py-4 text-left hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between"><div><div className="font-semibold text-slate-900">{o.operationNo}</div><div className="mt-1 text-sm text-slate-500">{o.stageName} · Responsable: {o.responsibleName}</div></div><div className="flex items-center gap-2"><Badge tone="blue">{o.operationalStatus}</Badge><span className="text-xs text-slate-500">{formatDate(o.openedAt)}</span></div></button>)}</div>;
}
