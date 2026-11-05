import {
  Building2,
  ChevronRight,
  Plus,
  Search,
  SlidersHorizontal
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/feedback/EmptyState";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { PermissionGate } from "@/features/auth/PermissionGate";
import { Permissions } from "@/types/auth";
import { HttpError } from "@/lib/api/http";
import { suppliersApi } from "@/features/suppliers/suppliers.api";
import { SupplierFormDialog } from "@/features/suppliers/SupplierFormDialog";
import type {
  SupplierDetail,
  SupplierListItem
} from "@/features/suppliers/types";

export function SuppliersPage() {
  const navigate = useNavigate();
  const [items, setItems] = useState<SupplierListItem[]>([]);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState<string>();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(timer);
  }, [search]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const result = await suppliersApi.list({
        search: debouncedSearch || undefined,
        isActive:
          status === "ACTIVE"
            ? true
            : status === "INACTIVE"
              ? false
              : undefined,
        page,
        pageSize
      });

      setItems(result.items);
      setTotal(result.total);
      setTotalPages(Math.max(result.totalPages, 1));
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo consultar proveedores."
      );
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, status, page, pageSize]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <>
      <PageHeader
        eyebrow="Maestros"
        title="Proveedores"
        description="Maestro de proveedores utilizados en solicitudes y operaciones de importación."
        actions={
          <PermissionGate permission={Permissions.SuppliersManage}>
            <Button onClick={() => setFormOpen(true)}>
              <Plus className="size-4" />
              Nuevo proveedor
            </Button>
          </PermissionGate>
        }
      />

      <Card className="overflow-hidden">
        <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-[1.4fr_240px_auto]">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
            <Input
              className="pl-9"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Razón social, Tax ID o email..."
            />
          </div>

          <SearchableSelect
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
            options={[
              { value: "ACTIVE", label: "Activos" },
              { value: "INACTIVE", label: "Inactivos" }
            ]}
            placeholder="Todos los estados"
          />

          <Button
            variant="secondary"
            onClick={() => {
              setSearch("");
              setStatus(undefined);
              setPage(1);
            }}
          >
            <SlidersHorizontal className="size-4" />
            Limpiar
          </Button>
        </div>

        {error && (
          <div className="p-4">
            <InlineAlert message={error} />
          </div>
        )}

        {loading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            title="No hay proveedores"
            description={
              debouncedSearch || status
                ? "No encontramos proveedores para los filtros seleccionados."
                : "Aún no se han registrado proveedores."
            }
          />
        ) : (
          <>
            <div className="app-scrollbar hidden overflow-x-auto md:block">
              <table className="w-full min-w-[850px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70">
                    {[
                      "Proveedor",
                      "Tax ID",
                      "País",
                      "Contacto",
                      "Estado",
                      ""
                    ].map((h) => (
                      <th
                        key={h}
                        className="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-[0.08em] text-slate-500"
                      >
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>

                <tbody>
                  {items.map((item) => (
                    <tr
                      key={item.supplierId}
                      className="cursor-pointer border-b border-slate-100 transition last:border-0 hover:bg-slate-50/70"
                      onClick={() =>
                        navigate(`/maestros/proveedores/${item.supplierId}`)
                      }
                    >
                      <td className="px-4 py-4">
                        <div className="font-semibold text-slate-900">
                          {item.legalName}
                        </div>
                        <div className="mt-1 text-xs text-slate-500">
                          ID {item.supplierId}
                        </div>
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700">
                        {item.taxId || "—"}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700">
                        {item.countryCode || "—"}
                      </td>
                      <td className="px-4 py-4">
                        <div className="text-sm text-slate-700">
                          {item.email || "—"}
                        </div>
                        <div className="mt-1 text-xs text-slate-500">
                          {item.phone || ""}
                        </div>
                      </td>
                      <td className="px-4 py-4">
                        <Badge tone={item.isActive ? "green" : "neutral"}>
                          {item.isActive ? "Activo" : "Inactivo"}
                        </Badge>
                      </td>
                      <td className="px-4 py-4 text-right">
                        <ChevronRight className="ml-auto size-4 text-slate-400" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {items.map((item) => (
                <button
                  key={item.supplierId}
                  type="button"
                  className="w-full p-4 text-left hover:bg-slate-50"
                  onClick={() =>
                    navigate(`/maestros/proveedores/${item.supplierId}`)
                  }
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-semibold text-slate-900">
                        {item.legalName}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        {item.taxId || "Sin Tax ID"}
                      </div>
                    </div>
                    <Badge tone={item.isActive ? "green" : "neutral"}>
                      {item.isActive ? "Activo" : "Inactivo"}
                    </Badge>
                  </div>

                  <div className="mt-3 text-sm text-slate-600">
                    {item.countryCode || "País no informado"}
                    {item.email ? ` · ${item.email}` : ""}
                  </div>
                </button>
              ))}
            </div>

            <div className="flex flex-col gap-3 border-t border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="text-xs text-slate-500">
                {total} proveedor{total === 1 ? "" : "es"}
              </div>

              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Anterior
                </Button>
                <span className="px-3 text-sm font-semibold text-slate-700">
                  {page} / {totalPages}
                </span>
                <Button
                  variant="ghost"
                  disabled={page >= totalPages}
                  onClick={() =>
                    setPage((p) => Math.min(totalPages, p + 1))
                  }
                >
                  Siguiente
                </Button>
              </div>
            </div>
          </>
        )}
      </Card>

      <SupplierFormDialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSaved={(supplier: SupplierDetail) => {
          setFormOpen(false);
          navigate(`/maestros/proveedores/${supplier.supplierId}`);
        }}
      />
    </>
  );
}
