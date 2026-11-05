import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import { clientsApi } from "./clients.api";
import type { Country } from "@/features/catalogs/types";
import type { AddressType, ClientAddress, CreateClientAddressRequest } from "./types";
import { HttpError } from "@/lib/api/http";

export function AddressFormDialog({ open, clientId, address, onClose, onSaved }: { open: boolean; clientId: number; address?: ClientAddress | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState<CreateClientAddressRequest>({ addressType: "LEGAL", addressLine: "", district: "", province: "", department: "", countryCode: "PE", isPrimary: false });
  const [countries, setCountries] = useState<Country[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setError("");
    setForm(address ? { addressType: address.addressType, addressLine: address.addressLine, district: address.district ?? "", province: address.province ?? "", department: address.department ?? "", countryCode: address.countryCode, isPrimary: address.isPrimary } : { addressType: "LEGAL", addressLine: "", district: "", province: "", department: "", countryCode: "PE", isPrimary: false });
    catalogsApi.countries().then(setCountries).catch(() => setError("No se pudo cargar el catálogo de países."));
  }, [open, address]);

  const countryOptions = useMemo(() => countries.map((x) => ({ value: x.countryCode, label: `${x.countryCode} — ${x.countryName}` })), [countries]);
  const typeOptions = [
    { value: "LEGAL", label: "Legal / Fiscal" },
    { value: "BILLING", label: "Facturación" },
    { value: "DELIVERY", label: "Entrega" },
    { value: "OTHER", label: "Otra" }
  ];

  const submit = async (e: FormEvent) => {
    e.preventDefault(); setError("");
    if (!form.addressLine.trim()) return setError("La dirección es obligatoria.");
    setSaving(true);
    try {
      const payload = { ...form, addressLine: form.addressLine.trim(), district: form.district?.trim() || null, province: form.province?.trim() || null, department: form.department?.trim() || null };
      if (address) await clientsApi.updateAddress(clientId, address.clientAddressId, { ...payload, isActive: address.isActive });
      else await clientsApi.addAddress(clientId, payload);
      onSaved(); onClose();
    } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo guardar la dirección."); }
    finally { setSaving(false); }
  };

  return (
    <Dialog open={open} onClose={onClose} title={address ? "Editar dirección" : "Nueva dirección"} description="Dirección legal, de facturación o entrega del cliente." footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button form="address-form" type="submit" loading={saving}>Guardar</Button></div>}>
      <form id="address-form" onSubmit={submit} className="space-y-4">
        {error && <InlineAlert message={error} />}
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo *</span><SearchableSelect value={form.addressType} onChange={(v) => setForm({ ...form, addressType: (v ?? "LEGAL") as AddressType })} options={typeOptions} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">País *</span><SearchableSelect value={form.countryCode} onChange={(v) => setForm({ ...form, countryCode: v ?? "PE" })} options={countryOptions} searchPlaceholder="Buscar país..." /></label>
        </div>
        <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Dirección *</span><Input value={form.addressLine} onChange={(e) => setForm({ ...form, addressLine: e.target.value })} placeholder="Av., calle, número, oficina..." /></label>
        <div className="grid gap-4 sm:grid-cols-3">
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Distrito</span><Input value={form.district ?? ""} onChange={(e) => setForm({ ...form, district: e.target.value })} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Provincia</span><Input value={form.province ?? ""} onChange={(e) => setForm({ ...form, province: e.target.value })} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Departamento</span><Input value={form.department ?? ""} onChange={(e) => setForm({ ...form, department: e.target.value })} /></label>
        </div>
        <label className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-3 text-sm text-slate-700"><input type="checkbox" className="size-4 accent-blue-700" checked={form.isPrimary} onChange={(e) => setForm({ ...form, isPrimary: e.target.checked })} /><span><strong>Dirección principal</strong><span className="block text-xs font-normal text-slate-500">Se utilizará como referencia principal del cliente.</span></span></label>
      </form>
    </Dialog>
  );
}
