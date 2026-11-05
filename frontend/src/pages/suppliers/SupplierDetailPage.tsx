import {
  ArrowLeft,
  Building2,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Plus,
  Power,
  UserRound
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";
import {
  Link,
  useNavigate,
  useParams
} from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs } from "@/components/ui/Tabs";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { PermissionGate } from "@/features/auth/PermissionGate";
import { Permissions } from "@/types/auth";
import { formatDate } from "@/lib/format";
import { HttpError } from "@/lib/api/http";
import { suppliersApi } from "@/features/suppliers/suppliers.api";
import { SupplierFormDialog } from "@/features/suppliers/SupplierFormDialog";
import { SupplierContactFormDialog } from "@/features/suppliers/SupplierContactFormDialog";
import type {
  SupplierContact,
  SupplierDetail,
  SupplierOperationSummary,
  SupplierRequestSummary
} from "@/features/suppliers/types";

type TabKey = "GENERAL" | "CONTACTS" | "REQUESTS" | "OPERATIONS";

export function SupplierDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const supplierId = Number(id);

  const [supplier, setSupplier] = useState<SupplierDetail | null>(null);
  const [tab, setTab] = useState<TabKey>("GENERAL");
  const [loading, setLoading] = useState(true);
  const [relatedLoading, setRelatedLoading] = useState(false);
  const [error, setError] = useState("");
  const [editOpen, setEditOpen] = useState(false);
  const [contactOpen, setContactOpen] = useState(false);
  const [editingContact, setEditingContact] = useState<SupplierContact | null>(null);
  const [requests, setRequests] = useState<SupplierRequestSummary[]>([]);
  const [operations, setOperations] = useState<SupplierOperationSummary[]>([]);

  const load = useCallback(async () => {
    if (!Number.isFinite(supplierId) || supplierId <= 0) return;

    setLoading(true);
    setError("");

    try {
      setSupplier(await suppliersApi.get(supplierId));
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo consultar el proveedor."
      );
    } finally {
      setLoading(false);
    }
  }, [supplierId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!supplier) return;

    if (tab === "REQUESTS" && requests.length === 0) {
      setRelatedLoading(true);
      suppliersApi.requests(supplier.supplierId)
        .then((r) => setRequests(r.items))
        .catch(() => setRequests([]))
        .finally(() => setRelatedLoading(false));
    }

    if (tab === "OPERATIONS" && operations.length === 0) {
      setRelatedLoading(true);
      suppliersApi.operations(supplier.supplierId)
        .then((r) => setOperations(r.items))
        .catch(() => setOperations([]))
        .finally(() => setRelatedLoading(false));
    }
  }, [tab, supplier, requests.length, operations.length]);

  const toggleActive = async () => {
    if (!supplier) return;

    try {
      const updated = await suppliersApi.update(supplier.supplierId, {
        legalName: supplier.legalName,
        taxId: supplier.taxId,
        countryCode: supplier.countryCode,
        email: supplier.email,
        phone: supplier.phone,
        addressLine: supplier.addressLine,
        notes: supplier.notes,
        isActive: !supplier.isActive,
        rowVersion: supplier.rowVersion
      });
      setSupplier(updated);
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo actualizar el estado del proveedor."
      );
    }
  };

  const deactivateContact = async (contact: SupplierContact) => {
    if (!supplier) return;

    if (contact.isActive) {
      await suppliersApi.deactivateContact(
        supplier.supplierId,
        contact.supplierContactId
      );
    } else {
      await suppliersApi.updateContact(
        supplier.supplierId,
        contact.supplierContactId,
        {
          fullName: contact.fullName,
          email: contact.email,
          phone: contact.phone,
          positionName: contact.positionName,
          isPrimary: contact.isPrimary,
          isActive: true
        }
      );
    }

    await load();
  };

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-16" />
        <Skeleton className="h-80" />
      </div>
    );
  }

  if (error && !supplier) {
    return <InlineAlert message={error} />;
  }

  if (!supplier) return null;

  const primary = supplier.contacts.find((x) => x.isPrimary && x.isActive);

  return (
    <>
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-slate-900"
        onClick={() => navigate("/maestros/proveedores")}
      >
        <ArrowLeft className="size-4" />
        Volver a proveedores
      </button>

      <PageHeader
        eyebrow="Proveedor"
        title={supplier.legalName}
        description={[
          supplier.taxId ? `Tax ID ${supplier.taxId}` : null,
          supplier.countryCode
        ].filter(Boolean).join(" · ")}
        actions={
          <PermissionGate permission={Permissions.SuppliersManage}>
            <>
              <Button
                variant="secondary"
                onClick={() => setEditOpen(true)}
              >
                <Pencil className="size-4" />
                Editar
              </Button>
              <Button
                variant={supplier.isActive ? "secondary" : "primary"}
                onClick={toggleActive}
              >
                <Power className="size-4" />
                {supplier.isActive ? "Desactivar" : "Reactivar"}
              </Button>
            </>
          </PermissionGate>
        }
      />

      {error && (
        <div className="mb-4">
          <InlineAlert message={error} />
        </div>
      )}

      <div className="mb-5 flex flex-wrap items-center gap-2">
        <Badge tone={supplier.isActive ? "green" : "neutral"}>
          {supplier.isActive ? "Activo" : "Inactivo"}
        </Badge>
        {primary && (
          <Badge tone="blue">
            Contacto principal: {primary.fullName}
          </Badge>
        )}
      </div>

      <Tabs
        value={tab}
        onChange={(value) => setTab(value as TabKey)}
        items={[
          { value: "GENERAL", label: "General" },
          { value: "CONTACTS", label: `Contactos (${supplier.contacts.length})` },
          { value: "REQUESTS", label: "Solicitudes" },
          { value: "OPERATIONS", label: "Operaciones" }
        ]}
      />

      <div className="mt-4">
        {tab === "GENERAL" && (
          <div className="grid gap-4 xl:grid-cols-[1.4fr_0.8fr]">
            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">
                Información general
              </h2>

              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                {[
                  ["Razón social / Nombre", supplier.legalName, Building2],
                  ["Tax ID", supplier.taxId || "—", Building2],
                  ["País", supplier.countryCode || "—", MapPin],
                  ["Email", supplier.email || "—", Mail],
                  ["Teléfono", supplier.phone || "—", Phone],
                  ["Dirección", supplier.addressLine || "—", MapPin]
                ].map(([label, value, Icon]: any) => (
                  <div key={label} className="flex gap-3">
                    <div className="grid size-9 shrink-0 place-items-center rounded-xl bg-slate-100 text-slate-500">
                      <Icon className="size-4" />
                    </div>
                    <div>
                      <div className="text-xs font-medium text-slate-400">
                        {label}
                      </div>
                      <div className="mt-1 text-sm font-medium text-slate-800">
                        {value}
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              <div className="mt-6 border-t border-slate-100 pt-5">
                <div className="text-xs font-medium text-slate-400">
                  Notas
                </div>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                  {supplier.notes || "Sin notas registradas."}
                </p>
              </div>
            </Card>

            <Card className="p-5">
              <h2 className="text-base font-semibold text-slate-900">
                Contacto principal
              </h2>

              {primary ? (
                <div className="mt-5">
                  <div className="grid size-12 place-items-center rounded-2xl bg-blue-50 text-blue-700">
                    <UserRound className="size-5" />
                  </div>
                  <div className="mt-4 font-semibold text-slate-900">
                    {primary.fullName}
                  </div>
                  <div className="mt-1 text-sm text-slate-500">
                    {primary.positionName || "Cargo no registrado"}
                  </div>

                  <div className="mt-4 space-y-2 text-sm text-slate-600">
                    <div>{primary.email || "Sin email"}</div>
                    <div>{primary.phone || "Sin teléfono"}</div>
                  </div>
                </div>
              ) : (
                <p className="mt-4 text-sm leading-6 text-slate-500">
                  No hay un contacto principal activo.
                </p>
              )}
            </Card>
          </div>
        )}

        {tab === "CONTACTS" && (
          <Card className="overflow-hidden">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <div>
                <h2 className="font-semibold text-slate-900">
                  Contactos del proveedor
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  Personas de contacto utilizadas durante solicitudes y operaciones.
                </p>
              </div>

              <PermissionGate permission={Permissions.SuppliersManage}>
                <Button
                  onClick={() => {
                    setEditingContact(null);
                    setContactOpen(true);
                  }}
                >
                  <Plus className="size-4" />
                  Nuevo contacto
                </Button>
              </PermissionGate>
            </div>

            {supplier.contacts.length === 0 ? (
              <EmptyState
                title="Sin contactos"
                description="Registra al menos un contacto comercial u operativo del proveedor."
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {supplier.contacts.map((contact) => (
                  <div
                    key={contact.supplierContactId}
                    className="flex flex-col gap-4 px-5 py-4 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-semibold text-slate-900">
                          {contact.fullName}
                        </span>
                        {contact.isPrimary && <Badge tone="blue">Principal</Badge>}
                        <Badge tone={contact.isActive ? "green" : "neutral"}>
                          {contact.isActive ? "Activo" : "Inactivo"}
                        </Badge>
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {contact.positionName || "Sin cargo"}
                      </div>
                      <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                        <span>{contact.email || "Sin email"}</span>
                        <span>{contact.phone || "Sin teléfono"}</span>
                      </div>
                    </div>

                    <PermissionGate permission={Permissions.SuppliersManage}>
                      <div className="flex gap-2">
                        <Button
                          variant="secondary"
                          onClick={() => {
                            setEditingContact(contact);
                            setContactOpen(true);
                          }}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="ghost"
                          onClick={() => deactivateContact(contact)}
                        >
                          {contact.isActive ? "Desactivar" : "Reactivar"}
                        </Button>
                      </div>
                    </PermissionGate>
                  </div>
                ))}
              </div>
            )}
          </Card>
        )}

        {tab === "REQUESTS" && (
          <Card className="overflow-hidden">
            {relatedLoading ? (
              <div className="space-y-3 p-5">
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
              </div>
            ) : requests.length === 0 ? (
              <EmptyState
                title="Sin solicitudes relacionadas"
                description="Este proveedor todavía no está asociado a solicitudes de importación."
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {requests.map((request) => (
                  <Link
                    key={request.requestId}
                    to={`/solicitudes/${request.requestId}`}
                    className="block px-5 py-4 transition hover:bg-slate-50"
                  >
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <div className="font-semibold text-slate-900">
                          {request.requestNo}
                        </div>
                        <div className="mt-1 text-sm text-slate-500">
                          {request.clientName} · {request.merchandiseDescription}
                        </div>
                      </div>
                      <div className="flex items-center gap-3">
                        <Badge tone="blue">{request.status}</Badge>
                        <span className="text-xs text-slate-500">
                          {formatDate(request.requestDate)}
                        </span>
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        )}

        {tab === "OPERATIONS" && (
          <Card className="overflow-hidden">
            {relatedLoading ? (
              <div className="space-y-3 p-5">
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
              </div>
            ) : operations.length === 0 ? (
              <EmptyState
                title="Sin operaciones relacionadas"
                description="Este proveedor todavía no participa en operaciones creadas."
              />
            ) : (
              <div className="divide-y divide-slate-100">
                {operations.map((operation) => (
                  <Link
                    key={operation.operationId}
                    to={`/operaciones/${operation.operationId}`}
                    className="block px-5 py-4 transition hover:bg-slate-50"
                  >
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <div className="font-semibold text-slate-900">
                          {operation.operationNo}
                        </div>
                        <div className="mt-1 text-sm text-slate-500">
                          {operation.clientName} · {operation.stageName}
                        </div>
                      </div>
                      <div className="flex items-center gap-3">
                        <Badge tone="blue">{operation.operationalStatus}</Badge>
                        <span className="text-xs text-slate-500">
                          {operation.responsibleName}
                        </span>
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        )}
      </div>

      <SupplierFormDialog
        open={editOpen}
        supplier={supplier}
        onClose={() => setEditOpen(false)}
        onSaved={(updated) => {
          setSupplier(updated);
          setEditOpen(false);
        }}
      />

      <SupplierContactFormDialog
        open={contactOpen}
        supplierId={supplier.supplierId}
        contact={editingContact}
        onClose={() => {
          setContactOpen(false);
          setEditingContact(null);
        }}
        onSaved={async () => {
          setContactOpen(false);
          setEditingContact(null);
          await load();
        }}
      />
    </>
  );
}
