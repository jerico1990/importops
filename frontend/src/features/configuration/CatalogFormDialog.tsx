import { useEffect, useMemo, useState } from "react";
import { Lock } from "lucide-react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { HttpError } from "@/lib/api/http";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog, Country } from "@/features/catalogs/types";
import { configurationApi } from "./configuration.api";
import {
  STANDARD_COUNTRIES,
  STANDARD_CURRENCIES,
  STANDARD_INCOTERMS
} from "./standardCatalogs";
import type {
  CatalogAdminItem,
  CatalogKey,
  CatalogSaveRequest
} from "./types";

const labels: Record<CatalogKey, string> = {
  countries: "país",
  ports: "puerto",
  currencies: "moneda",
  incoterms: "Incoterm",
  "transport-modes": "modo de transporte",
  "document-types": "tipo de documento",
  "service-categories": "categoría de servicio",
  "expense-categories": "categoría de gasto",
  "payment-methods": "método de pago",
  "customs-regimes": "régimen aduanero",
  "customs-offices": "aduana",
  "customs-tax-concepts": "concepto aduanero",
  "quotation-concepts": "concepto de proforma"
};

const internalCatalogs = new Set<CatalogKey>([
  "transport-modes",
  "document-types",
  "service-categories",
  "expense-categories",
  "payment-methods",
  "customs-regimes",
  "customs-offices",
  "customs-tax-concepts",
  "quotation-concepts"
]);

function LockedCode({
  code,
  label = "Código interno"
}: {
  code: string;
  label?: string;
}) {
  return (
    <div className="sm:col-span-2 rounded-xl border border-slate-200 bg-slate-50 p-3">
      <div className="flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
        <Lock className="size-3.5" />
        {label}
      </div>
      <div className="mt-1 font-mono text-sm font-semibold text-slate-800">
        {code}
      </div>
      <div className="mt-1 text-xs text-slate-500">
        El código no puede modificarse después de crear el registro.
      </div>
    </div>
  );
}

export function CatalogFormDialog({
  open,
  onClose,
  catalog,
  item,
  onSaved
}: {
  open: boolean;
  onClose: () => void;
  catalog: CatalogKey;
  item: CatalogAdminItem | null;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CatalogSaveRequest>({});
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [registeredCountries, setRegisteredCountries] = useState<Country[]>([]);
  const [documentCategories, setDocumentCategories] = useState<CodeNameCatalog[]>([]);
  const [loadingRelations, setLoadingRelations] = useState(false);
  const [serviceCategories, setServiceCategories] = useState<{serviceCategoryId:number;code:string;name:string}[]>([]);
  const [expenseCategories, setExpenseCategories] = useState<{expenseCategoryId:number;code:string;name:string}[]>([]);
  const [conceptType, setConceptType] = useState<"SERVICE"|"EXPENSE"|"">("");

  useEffect(() => {
    if (!open) return;

    setForm(
      item
        ? {
            code: item.code,
            name: item.name,
            description: item.description ?? undefined,
            symbol: item.symbol ?? undefined,
            decimalPlaces: item.decimalPlaces ?? undefined,
            countryCode: item.countryCode ?? undefined,
            cityName: item.cityName ?? undefined,
            portType: item.portType ?? undefined,
            category: item.category ?? undefined,
            flag: item.flag ?? undefined,
            serviceCategoryId: item.serviceCategoryId ?? undefined, expenseCategoryId: item.expenseCategoryId ?? undefined,
            defaultTaxRate: item.defaultTaxRate ?? 0, isTaxable: item.isTaxable ?? false, isRebillableDefault: item.isRebillableDefault ?? false,
            displayOrder: item.displayOrder ?? 10, groupCode: item.groupCode ?? undefined, groupName: item.groupName ?? undefined
          }
        : {
            decimalPlaces: 2,
            portType: catalog === "ports" ? "SEA" : undefined,
            defaultTaxRate: catalog === "quotation-concepts" ? 18 : undefined, isTaxable: catalog === "quotation-concepts" ? true : undefined,
            isRebillableDefault: catalog === "quotation-concepts" ? true : undefined, displayOrder: catalog === "quotation-concepts" ? 10 : undefined
          }
    );
    setConceptType(catalog === "quotation-concepts" ? (item?.serviceCategoryId ? "SERVICE" : item?.expenseCategoryId ? "EXPENSE" : "") : "");
    setError("");
  }, [open, item, catalog]);

  useEffect(() => {
    if (!open || (catalog !== "ports" && catalog !== "document-types" && catalog !== "quotation-concepts")) return;

    let cancelled = false;
    setLoadingRelations(true);

    const requests: Promise<void>[] = [];

    if (catalog === "ports") {
      requests.push(
        catalogsApi
          .countries()
          .then((rows) => {
            if (!cancelled) setRegisteredCountries(rows);
          })
          .catch(() => {
            if (!cancelled) setRegisteredCountries([]);
          })
      );
    }

    if (catalog === "quotation-concepts") {
      requests.push(catalogsApi.serviceCategories().then(rows => { if (!cancelled) setServiceCategories(rows); }).catch(() => { if (!cancelled) setServiceCategories([]); }));
      requests.push(catalogsApi.expenseCategories().then(rows => { if (!cancelled) setExpenseCategories(rows); }).catch(() => { if (!cancelled) setExpenseCategories([]); }));
    }

    if (catalog === "document-types") {
      requests.push(
        catalogsApi
          .documentCategories()
          .then((rows) => {
            if (!cancelled) setDocumentCategories(rows);
          })
          .catch(() => {
            if (!cancelled) setDocumentCategories([]);
          })
      );
    }

    Promise.all(requests).finally(() => {
      if (!cancelled) setLoadingRelations(false);
    });

    return () => {
      cancelled = true;
    };
  }, [open, catalog]);

  const countryOptions = useMemo(() => {
    const rows = registeredCountries.map((country) => ({
      value: country.countryCode,
      label: `${country.countryName} — ${country.countryCode}`,
      description: country.iso3Code ? `ISO3: ${country.iso3Code}` : undefined
    }));

    if (
      item?.countryCode &&
      !rows.some((row) => row.value === item.countryCode)
    ) {
      rows.unshift({
        value: item.countryCode,
        label: `${item.countryName ?? "País registrado"} — ${item.countryCode}`,
        description: "País asociado actualmente al puerto"
      });
    }

    return rows;
  }, [registeredCountries, item]);

  const set = <K extends keyof CatalogSaveRequest>(
    key: K,
    value: CatalogSaveRequest[K]
  ) => setForm((current) => ({ ...current, [key]: value }));

  const selectCountry = (code?: string) => {
    const selected = STANDARD_COUNTRIES.find((country) => country.code === code);
    setForm((current) => ({
      ...current,
      code: selected?.code,
      name: selected?.name,
      description: selected?.iso3
    }));
  };

  const selectCurrency = (code?: string) => {
    const selected = STANDARD_CURRENCIES.find(
      (currency) => currency.code === code
    );
    setForm((current) => ({
      ...current,
      code: selected?.code,
      name: selected?.name,
      symbol: selected?.symbol,
      decimalPlaces: selected?.decimalPlaces ?? 2
    }));
  };

  const selectIncoterm = (code?: string) => {
    const selected = STANDARD_INCOTERMS.find(
      (incoterm) => incoterm.code === code
    );
    setForm((current) => ({
      ...current,
      code: selected?.code,
      name: selected?.name
    }));
  };

  const save = async () => {
    if (!form.name?.trim()) {
      setError("El nombre es obligatorio.");
      return;
    }

    if (
      !item &&
      ["countries", "currencies", "incoterms"].includes(catalog) &&
      !form.code?.trim()
    ) {
      setError("Selecciona un registro estándar.");
      return;
    }

    if (catalog === "ports" && (!form.countryCode || !form.portType)) {
      setError("País y tipo son obligatorios.");
      return;
    }

    if (catalog === "document-types" && !form.category) {
      setError("La categoría es obligatoria.");
      return;
    }

    if (catalog === "quotation-concepts" && (!conceptType || !form.groupCode || (conceptType === "SERVICE" && !form.serviceCategoryId) || (conceptType === "EXPENSE" && !form.expenseCategoryId))) {
      setError("Selecciona el tipo y la categoría del concepto.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      if (item) {
        await configurationApi.update(catalog, item.id, form);
      } else {
        await configurationApi.create(catalog, form);
      }
      onSaved();
      onClose();
    } catch (e) {
      setError(
        e instanceof HttpError ? e.message : "No se pudo guardar el registro."
      );
    } finally {
      setSaving(false);
    }
  };

  const standardCreate =
    !item && ["countries", "currencies", "incoterms"].includes(catalog);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={`${item ? "Editar" : "Nuevo"} ${labels[catalog]}`}
      description={
        internalCatalogs.has(catalog) && !item
          ? "R&B Freight Cargo S.A.C. · ImportOps generará automáticamente el código interno al guardar."
          : "Las validaciones de integridad se verifican nuevamente en el servidor."
      }
      className="sm:min-h-[560px] sm:max-w-2xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button loading={saving} onClick={() => void save()}>
            Guardar
          </Button>
        </div>
      }
    >
      {catalog === "quotation-concepts" && <div className="mb-4 rounded-xl border border-blue-100 bg-blue-50 p-4 text-sm text-blue-900">La clasificación se define aquí una sola vez. Al usar el concepto en una proforma, tipo, categoría e impuesto se aplicarán automáticamente.</div>}
      <div className="grid gap-4 sm:grid-cols-2">
        {error && (
          <div className="sm:col-span-2">
            <InlineAlert message={error} />
          </div>
        )}

        {standardCreate && catalog === "countries" && (
          <label className="sm:col-span-2 text-sm font-semibold text-slate-700">
            País *
            <div className="mt-1">
              <SearchableSelect
                value={form.code}
                onChange={selectCountry}
                options={STANDARD_COUNTRIES.map((country) => ({
                  value: country.code,
                  label: `${country.name} — ${country.code}`,
                  description: `ISO3: ${country.iso3}`
                }))}
                placeholder="Buscar país..."
                searchPlaceholder="Buscar por país o código ISO..."
              />
            </div>
          </label>
        )}

        {standardCreate && catalog === "currencies" && (
          <label className="sm:col-span-2 text-sm font-semibold text-slate-700">
            Moneda *
            <div className="mt-1">
              <SearchableSelect
                value={form.code}
                onChange={selectCurrency}
                options={STANDARD_CURRENCIES.map((currency) => ({
                  value: currency.code,
                  label: `${currency.name} — ${currency.code}`,
                  description: currency.symbol
                }))}
                placeholder="Buscar moneda..."
                searchPlaceholder="Buscar por moneda o código ISO..."
              />
            </div>
          </label>
        )}

        {standardCreate && catalog === "incoterms" && (
          <label className="sm:col-span-2 text-sm font-semibold text-slate-700">
            Incoterm *
            <div className="mt-1">
              <SearchableSelect
                value={form.code}
                onChange={selectIncoterm}
                options={STANDARD_INCOTERMS.map((incoterm) => ({
                  value: incoterm.code,
                  label: `${incoterm.code} — ${incoterm.name}`
                }))}
                placeholder="Seleccionar Incoterm..."
                searchPlaceholder="Buscar Incoterm..."
              />
            </div>
          </label>
        )}

        {item && (
          <LockedCode
            code={item.code}
            label={
              catalog === "countries"
                ? "Código ISO"
                : catalog === "currencies"
                  ? "Código ISO 4217"
                  : catalog === "incoterms"
                    ? "Código Incoterm"
                    : catalog === "ports"
                      ? "UN/LOCODE"
                      : "Código interno"
            }
          />
        )}

        {!item && catalog === "ports" && (
          <label className="text-sm font-semibold text-slate-700">
            UN/LOCODE
            <Input
              className="mt-1"
              value={form.code ?? ""}
              onChange={(event) => set("code", event.target.value.toUpperCase())}
              placeholder="Ej. PECLL (opcional)"
            />
            <span className="mt-1 block text-xs font-normal text-slate-500">
              Si se informa, no podrá modificarse después de crear el puerto.
            </span>
          </label>
        )}

        {!standardCreate && (
          <label className="text-sm font-semibold text-slate-700">
            {catalog === "incoterms" ? "Descripción" : "Nombre"}
            <Input
              className="mt-1"
              value={form.name ?? ""}
              onChange={(event) => set("name", event.target.value)}
            />
          </label>
        )}

        {standardCreate && form.code && (
          <div className="sm:col-span-2 rounded-xl bg-blue-50 p-3 text-sm text-blue-900">
            <strong>{form.name}</strong>
            <div className="mt-1 text-xs">
              Código:{" "}
              <span className="font-mono font-semibold">{form.code}</span>
          {catalog === "countries" && form.description
                ? ` · ISO3: ${form.description}`
                : ""}
            </div>
          </div>
        )}

        {catalog === "quotation-concepts" && (
          <>
            <label className="text-sm font-semibold text-slate-700">
              Tipo *
              <select
                className="mt-1 h-10 w-full rounded-lg border border-slate-200 px-3 text-sm"
                value={conceptType}
                onChange={(event) => {
                  const nextType = event.target.value as "SERVICE" | "EXPENSE" | "";
                  setConceptType(nextType);
                  setForm((current) => ({
                    ...current,
                    serviceCategoryId: undefined,
                    expenseCategoryId: undefined,
                    isRebillableDefault:
                      nextType === "EXPENSE" ? current.isRebillableDefault : false
                  }));
                }}
              >
                <option value="">Seleccionar tipo...</option>
                <option value="SERVICE">Servicio</option>
                <option value="EXPENSE">Gasto</option>
              </select>
            </label>

            {conceptType === "SERVICE" && (
              <label className="text-sm font-semibold text-slate-700">
                Categoría de servicio *
                <div className="mt-1">
                  <SearchableSelect
                    value={form.serviceCategoryId ? String(form.serviceCategoryId) : undefined}
                    onChange={(value) => set("serviceCategoryId", value ? Number(value) : undefined)}
                    options={serviceCategories.map((category) => ({
                      value: String(category.serviceCategoryId),
                      label: category.name,
                      description: category.code
                    }))}
                    placeholder="Seleccionar categoría..."
                    searchPlaceholder="Buscar categoría de servicio..."
                  />
                </div>
              </label>
            )}

            {conceptType === "EXPENSE" && (
              <label className="text-sm font-semibold text-slate-700">
                Categoría de gasto *
                <div className="mt-1">
                  <SearchableSelect
                    value={form.expenseCategoryId ? String(form.expenseCategoryId) : undefined}
                    onChange={(value) => set("expenseCategoryId", value ? Number(value) : undefined)}
                    options={expenseCategories.map((category) => ({
                      value: String(category.expenseCategoryId),
                      label: category.name,
                      description: category.code
                    }))}
                    placeholder="Seleccionar categoría..."
                    searchPlaceholder="Buscar categoría de gasto..."
                  />
                </div>
              </label>
            )}

            <label className="text-sm font-semibold text-slate-700">
              Grupo *
              <select
                className="mt-1 h-10 w-full rounded-lg border border-slate-200 px-3 text-sm"
                value={form.groupCode ?? ""}
                onChange={(event) => {
                  const groups: Record<string, string> = {
                    FREIGHT_INSURANCE: "Flete y seguro",
                    LOCAL_EXPENSES: "Gastos locales",
                    CUSTOMS_AGENCY: "Agencia de aduanas",
                    TRANSPORT: "Transporte",
                    OTHER: "Otros"
                  };
                  const groupCode = event.target.value || undefined;
                  setForm((current) => ({
                    ...current,
                    groupCode,
                    groupName: groupCode ? groups[groupCode] : undefined
                  }));
                }}
              >
                <option value="">Seleccionar grupo...</option>
                <option value="FREIGHT_INSURANCE">Flete y seguro</option>
                <option value="LOCAL_EXPENSES">Gastos locales</option>
                <option value="CUSTOMS_AGENCY">Agencia de aduanas</option>
                <option value="TRANSPORT">Transporte</option>
                <option value="OTHER">Otros</option>
              </select>
            </label>

            <label className="text-sm font-semibold text-slate-700">
              Orden
              <Input
                className="mt-1"
                type="number"
                min={0}
                value={form.displayOrder ?? 10}
                onChange={(event) => set("displayOrder", Number(event.target.value))}
              />
            </label>

            <label className="flex items-center gap-2 rounded-xl bg-slate-50 px-3 py-3 text-sm font-semibold text-slate-700">
              <input
                type="checkbox"
                checked={form.isTaxable ?? false}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    isTaxable: event.target.checked,
                    defaultTaxRate: event.target.checked ? current.defaultTaxRate ?? 18 : 0
                  }))
                }
              />
              Afecto a impuesto
            </label>

            <label className="text-sm font-semibold text-slate-700">
              Impuesto (%)
              <Input
                className="mt-1"
                type="number"
                min={0}
                max={100}
                step="0.0001"
                disabled={!form.isTaxable}
                value={form.isTaxable ? form.defaultTaxRate ?? 0 : 0}
                onChange={(event) => set("defaultTaxRate", Number(event.target.value))}
              />
            </label>

            {conceptType === "EXPENSE" && (
              <label className="sm:col-span-2 flex items-center gap-2 rounded-xl bg-slate-50 px-3 py-3 text-sm font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={form.isRebillableDefault ?? false}
                  onChange={(event) => set("isRebillableDefault", event.target.checked)}
                />
                Sujeto a rendición / refacturación por defecto
              </label>
            )}

            {loadingRelations && (
              <div className="sm:col-span-2 text-xs text-slate-500">
                Cargando categorías activas...
              </div>
            )}
          </>
        )}

        {catalog === "countries" && item && (
          <label className="text-sm font-semibold text-slate-700">
            Código ISO3
            <Input
              className="mt-1"
              value={form.description ?? ""}
              onChange={(event) =>
                set("description", event.target.value.toUpperCase())
              }
              maxLength={3}
            />
          </label>
        )}

        {catalog === "currencies" && form.code && (
          <>
            <label className="text-sm font-semibold text-slate-700">
              Símbolo
              <Input
                className="mt-1"
                value={form.symbol ?? ""}
                onChange={(event) => set("symbol", event.target.value)}
              />
            </label>
            <label className="text-sm font-semibold text-slate-700">
              Decimales
              <Input
                className="mt-1"
                type="number"
                min={0}
                max={6}
                value={form.decimalPlaces ?? 2}
                onChange={(event) =>
                  set("decimalPlaces", Number(event.target.value))
                }
              />
            </label>
          </>
        )}

        {catalog === "ports" && (
          <>
            <label className="sm:col-span-2 text-sm font-semibold text-slate-700">
              País *
              <div className="mt-1">
                <SearchableSelect
                  value={form.countryCode}
                  onChange={(value) => set("countryCode", value)}
                  options={countryOptions}
                  placeholder="Buscar país registrado..."
                  searchPlaceholder="Buscar por país o código ISO..."
                />
              </div>
              {loadingRelations && (
                <span className="mt-1 block text-xs font-normal text-slate-500">
                  Cargando países activos...
                </span>
              )}
            </label>
            <label className="text-sm font-semibold text-slate-700">
              Tipo
              <select
                className="mt-1 h-10 w-full rounded-lg border border-slate-200 px-3"
                value={form.portType ?? "SEA"}
                onChange={(event) => set("portType", event.target.value)}
              >
                <option value="SEA">Marítimo</option>
                <option value="AIR">Aéreo</option>
              </select>
            </label>
            <label className="text-sm font-semibold text-slate-700">
              Ciudad
              <Input
                className="mt-1"
                value={form.cityName ?? ""}
                onChange={(event) => set("cityName", event.target.value)}
              />
            </label>
          </>
        )}

        {catalog === "document-types" && (
          <>
            <label className="text-sm font-semibold text-slate-700">
              Categoría *
              <div className="mt-1">
                <SearchableSelect
                  value={form.category}
                  onChange={(value) => set("category", value)}
                  options={documentCategories.map((category) => ({
                    value: category.code,
                    label: category.name,
                    description: category.code
                  }))}
                  placeholder="Seleccionar categoría..."
                  searchPlaceholder="Buscar categoría..."
                />
              </div>
              {loadingRelations && (
                <span className="mt-1 block text-xs font-normal text-slate-500">
                  Cargando categorías...
                </span>
              )}
            </label>
            <label className="flex items-center gap-2 self-end pb-2 text-sm font-semibold text-slate-700">
              <input
                type="checkbox"
                checked={form.flag ?? false}
                onChange={(event) => set("flag", event.target.checked)}
              />
              Obligatorio por defecto
            </label>
          </>
        )}

        {catalog === "expense-categories" && (
          <label className="flex items-center gap-2 self-end pb-2 text-sm font-semibold text-slate-700">
            <input
              type="checkbox"
              checked={form.flag ?? false}
              onChange={(event) => set("flag", event.target.checked)}
            />
            Refacturable por defecto
          </label>
        )}
      </div>
    </Dialog>
  );
}
