import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import { HttpError } from "@/lib/api/http";
import { suppliersApi } from "./suppliers.api";
import type {
  SupplierDetail,
  CreateSupplierRequest,
  UpdateSupplierRequest
} from "./types";
import type { Country } from "@/features/catalogs/types";

export function SupplierFormDialog({
  open,
  supplier,
  onClose,
  onSaved
}: {
  open: boolean;
  supplier?: SupplierDetail | null;
  onClose: () => void;
  onSaved: (supplier: SupplierDetail) => void;
}) {
  const editing = Boolean(supplier);
  const [countries, setCountries] = useState<Country[]>([]);
  const [legalName, setLegalName] = useState("");
  const [taxId, setTaxId] = useState("");
  const [countryCode, setCountryCode] = useState<string>();
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [addressLine, setAddressLine] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;

    catalogsApi.countries()
      .then(setCountries)
      .catch(() => setCountries([]));
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setLegalName(supplier?.legalName ?? "");
    setTaxId(supplier?.taxId ?? "");
    setCountryCode(supplier?.countryCode ?? undefined);
    setEmail(supplier?.email ?? "");
    setPhone(supplier?.phone ?? "");
    setAddressLine(supplier?.addressLine ?? "");
    setNotes(supplier?.notes ?? "");
    setError("");
  }, [open, supplier]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (!legalName.trim()) {
      setError("La razón social o nombre del proveedor es obligatorio.");
      return;
    }

    const base: CreateSupplierRequest = {
      legalName: legalName.trim(),
      taxId: taxId.trim() || null,
      countryCode: countryCode || null,
      email: email.trim() || null,
      phone: phone.trim() || null,
      addressLine: addressLine.trim() || null,
      notes: notes.trim() || null
    };

    setSaving(true);

    try {
      const result = editing && supplier
        ? await suppliersApi.update(supplier.supplierId, {
            ...base,
            isActive: supplier.isActive,
            rowVersion: supplier.rowVersion
          } satisfies UpdateSupplierRequest)
        : await suppliersApi.create(base);

      onSaved(result);
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo guardar el proveedor."
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={editing ? "Editar proveedor" : "Nuevo proveedor"}
      description="Datos maestros utilizados por solicitudes, operaciones y gestión comercial."
      onClose={onClose}
      className="sm:max-w-4xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Razón social / Nombre *
            </span>
            <Input
              value={legalName}
              onChange={(e) => setLegalName(e.target.value)}
              placeholder="Ej. Shanghai Global Trading Co., Ltd."
              maxLength={200}
              required
            />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Tax ID / Identificación fiscal
            </span>
            <Input
              value={taxId}
              onChange={(e) => setTaxId(e.target.value)}
              placeholder="Identificación fiscal"
              maxLength={50}
            />
          </label>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              País
            </span>
            <SearchableSelect
              value={countryCode}
              onChange={setCountryCode}
              options={countries.map((c) => ({
                value: c.countryCode,
                label: `${c.countryCode} — ${c.countryName}`
              }))}
              placeholder="Seleccionar país"
              searchPlaceholder="Buscar país..."
            />
          </div>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Email
            </span>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ventas@proveedor.com"
              maxLength={200}
            />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Teléfono
            </span>
            <Input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="+86 ..."
              maxLength={50}
            />
          </label>

          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Dirección
            </span>
            <Input
              value={addressLine}
              onChange={(e) => setAddressLine(e.target.value)}
              placeholder="Dirección fiscal/comercial"
              maxLength={300}
            />
          </label>

          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Notas
            </span>
            <textarea
              className="focus-ring min-h-24 w-full resize-y rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm placeholder:text-slate-400"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Información comercial u operativa relevante del proveedor."
              maxLength={1000}
            />
          </label>
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={saving}>
            {editing ? "Guardar cambios" : "Crear proveedor"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
