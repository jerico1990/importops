import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import { clientsApi } from "./clients.api";
import type { AppUserCatalog, Country, Currency } from "@/features/catalogs/types";
import type { ClientDetail, ClientType, CreateClientRequest } from "./types";
import { HttpError } from "@/lib/api/http";

const blank: CreateClientRequest = {
  clientType: "COMPANY",
  legalName: "",
  tradeName: "",
  taxId: "",
  countryCode: "PE",
  email: "",
  phone: "",
  creditDays: 0,
  preferredCurrencyCode: "USD",
  notes: "",
  accountManagerUserId: null
};

export function ClientFormDialog({
  open,
  client,
  onClose,
  onSaved
}: {
  open: boolean;
  client?: ClientDetail | null;
  onClose: () => void;
  onSaved: (client: ClientDetail) => void;
}) {
  const [form, setForm] = useState<CreateClientRequest>(blank);
  const [countries, setCountries] = useState<Country[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [users, setUsers] = useState<AppUserCatalog[]>([]);
  const [saving, setSaving] = useState(false);
  const [loadingCatalogs, setLoadingCatalogs] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setError("");
    setForm(
      client
        ? {
            clientType: client.clientType,
            legalName: client.legalName,
            tradeName: client.tradeName ?? "",
            taxId: client.taxId ?? "",
            countryCode: client.countryCode,
            email: client.email ?? "",
            phone: client.phone ?? "",
            creditDays: client.creditDays ?? 0,
            preferredCurrencyCode: client.preferredCurrencyCode ?? "",
            notes: client.notes ?? "",
            accountManagerUserId: client.accountManagerUserId ?? null
          }
        : blank
    );

    setLoadingCatalogs(true);
    Promise.all([catalogsApi.countries(), catalogsApi.currencies(), catalogsApi.accountManagers()])
      .then(([countryRows, currencyRows, userRows]) => {
        setCountries(countryRows);
        setCurrencies(currencyRows);
        setUsers(userRows);
      })
      .catch(() => setError("No se pudieron cargar los catálogos del formulario."))
      .finally(() => setLoadingCatalogs(false));
  }, [open, client]);

  const countryOptions = useMemo(
    () => countries.map((x) => ({ value: x.countryCode, label: `${x.countryCode} — ${x.countryName}` })),
    [countries]
  );
  const currencyOptions = useMemo(
    () => currencies.map((x) => ({ value: x.currencyCode, label: `${x.currencyCode} — ${x.currencyName}` })),
    [currencies]
  );
  const userOptions = useMemo(
    () => users.map((x) => ({
      value: String(x.userId),
      label: [x.firstName, x.lastName].filter(Boolean).join(" ") || x.username,
      description: x.email ?? x.username
    })),
    [users]
  );

  const set = <K extends keyof CreateClientRequest>(key: K, value: CreateClientRequest[K]) =>
    setForm((current) => ({ ...current, [key]: value }));

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    if (!form.legalName.trim()) return setError("La razón social o nombre es obligatorio.");
    if (!form.countryCode) return setError("Selecciona el país del cliente.");
    if ((form.creditDays ?? 0) < 0) return setError("Los días de crédito no pueden ser negativos.");

    setSaving(true);
    try {
      const payload: CreateClientRequest = {
        ...form,
        legalName: form.legalName.trim(),
        tradeName: form.tradeName?.trim() || null,
        taxId: form.taxId?.trim() || null,
        email: form.email?.trim() || null,
        phone: form.phone?.trim() || null,
        preferredCurrencyCode: form.preferredCurrencyCode || null,
        notes: form.notes?.trim() || null
      };
      const saved = client
        ? await clientsApi.update(client.clientId, {
            ...payload,
            isActive: client.isActive,
            rowVersion: client.rowVersion
          })
        : await clientsApi.create(payload);
      onSaved(saved);
      onClose();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo guardar el cliente.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={client ? "Editar cliente" : "Nuevo cliente"}
      description={client ? `Actualiza los datos maestros de ${client.legalName}.` : "Registra la empresa o persona que solicitará servicios de importación."}
      className="sm:max-w-3xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" form="client-form" loading={saving}>{client ? "Guardar cambios" : "Crear cliente"}</Button>
        </div>
      }
    >
      <form id="client-form" onSubmit={submit} className="space-y-5">
        {error && <InlineAlert message={error} />}
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo de cliente</span>
            <SearchableSelect
              value={form.clientType}
              onChange={(value) => set("clientType", (value ?? "COMPANY") as ClientType)}
              options={[{ value: "COMPANY", label: "Empresa" }, { value: "PERSON", label: "Persona" }]}
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">{form.clientType === "COMPANY" ? "RUC / Tax ID" : "Documento"}</span>
            <Input value={form.taxId ?? ""} onChange={(e) => set("taxId", e.target.value)} placeholder="20601234567" />
          </label>
          <label className="block sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">{form.clientType === "COMPANY" ? "Razón social" : "Nombre completo"} *</span>
            <Input value={form.legalName} onChange={(e) => set("legalName", e.target.value)} required />
          </label>
          <label className="block sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Nombre comercial</span>
            <Input value={form.tradeName ?? ""} onChange={(e) => set("tradeName", e.target.value)} />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">País *</span>
            <SearchableSelect value={form.countryCode} onChange={(v) => set("countryCode", v ?? "PE")} options={countryOptions} placeholder={loadingCatalogs ? "Cargando..." : "Seleccionar país"} searchPlaceholder="Buscar país..." />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Moneda preferida</span>
            <SearchableSelect value={form.preferredCurrencyCode ?? undefined} onChange={(v) => set("preferredCurrencyCode", v ?? null)} options={currencyOptions} placeholder={loadingCatalogs ? "Cargando..." : "Seleccionar moneda"} />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Correo principal</span>
            <Input type="email" value={form.email ?? ""} onChange={(e) => set("email", e.target.value)} placeholder="contacto@empresa.com" />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Teléfono</span>
            <Input value={form.phone ?? ""} onChange={(e) => set("phone", e.target.value)} placeholder="+51 ..." />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Días de crédito</span>
            <Input type="number" min={0} value={form.creditDays ?? 0} onChange={(e) => set("creditDays", e.target.value === "" ? null : Number(e.target.value))} />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Ejecutivo de cuenta</span>
            <SearchableSelect value={form.accountManagerUserId ? String(form.accountManagerUserId) : undefined} onChange={(v) => set("accountManagerUserId", v ? Number(v) : null)} options={userOptions} placeholder={loadingCatalogs ? "Cargando..." : "Sin asignar"} searchPlaceholder="Buscar ejecutivo..." />
          </label>
          <label className="block sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas internas del maestro</span>
            <textarea className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 bg-white px-3.5 py-3 text-sm shadow-sm placeholder:text-slate-400" value={form.notes ?? ""} onChange={(e) => set("notes", e.target.value)} placeholder="Condiciones comerciales, referencias o información útil..." />
          </label>
        </div>
      </form>
    </Dialog>
  );
}
