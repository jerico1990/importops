import {
  useEffect,
  useMemo,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { MoneyInput } from "@/components/ui/MoneyInput";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { useAuth } from "@/features/auth/AuthContext";
import { clientsApi } from "@/features/clients/clients.api";
import type { ClientDetail, ClientListItem } from "@/features/clients/types";
import { suppliersApi } from "@/features/suppliers/suppliers.api";
import type { SupplierListItem } from "@/features/suppliers/types";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type {
  AppUserCatalog,
  Country,
  Currency,
  Incoterm,
  Port,
  TransportMode
} from "@/features/catalogs/types";
import { requestsApi } from "./requests.api";
import type {
  CreateImportRequest,
  ImportRequestDetail
} from "./types";
import { HttpError } from "@/lib/api/http";

const dateOnly = (value?: string | null) =>
  value ? value.slice(0, 10) : "";

const numberValue = (value: string) =>
  value.trim() === "" ? null : Number(value);

function userName(user: AppUserCatalog) {
  const full = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return full || user.username;
}

export function RequestFormDialog({
  open,
  request,
  onClose,
  onSaved
}: {
  open: boolean;
  request?: ImportRequestDetail | null;
  onClose: () => void;
  onSaved: (request: ImportRequestDetail) => void;
}) {
  const { user } = useAuth();
  const clientMode = user?.userType === "CLIENT";
  const editing = Boolean(request);

  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [client, setClient] = useState<ClientDetail | null>(null);
  const [suppliers, setSuppliers] = useState<SupplierListItem[]>([]);
  const [assignees, setAssignees] = useState<AppUserCatalog[]>([]);
  const [countries, setCountries] = useState<Country[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [transportModes, setTransportModes] = useState<TransportMode[]>([]);
  const [incoterms, setIncoterms] = useState<Incoterm[]>([]);
  const [originPorts, setOriginPorts] = useState<Port[]>([]);
  const [destinationPorts, setDestinationPorts] = useState<Port[]>([]);

  const [clientId, setClientId] = useState<string>();
  const [clientContactId, setClientContactId] = useState<string>();
  const [supplierId, setSupplierId] = useState<string>();
  const [assignedUserId, setAssignedUserId] = useState<string>();
  const [requestDate, setRequestDate] = useState("");
  const [transportModeId, setTransportModeId] = useState<string>();
  const [incotermCode, setIncotermCode] = useState<string>();
  const [originCountryCode, setOriginCountryCode] = useState<string>();
  const [destinationCountryCode, setDestinationCountryCode] = useState<string>();
  const [originPortId, setOriginPortId] = useState<string>();
  const [destinationPortId, setDestinationPortId] = useState<string>();
  const [merchandiseDescription, setMerchandiseDescription] = useState("");
  const [packageCount, setPackageCount] = useState("");
  const [grossWeightKg, setGrossWeightKg] = useState("");
  const [volumeM3, setVolumeM3] = useState("");
  const [declaredValue, setDeclaredValue] = useState("");
  const [declaredCurrencyCode, setDeclaredCurrencyCode] = useState<string>();
  const [desiredServiceDate, setDesiredServiceDate] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [internalNotes, setInternalNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [loadingCatalogs, setLoadingCatalogs] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;

    const initialize = async () => {
      setLoadingCatalogs(true);
      setError("");

      try {
        const [countryData, currencyData, transportData, incotermData] =
          await Promise.all([
            catalogsApi.countries(),
            catalogsApi.currencies(),
            catalogsApi.transportModes(),
            catalogsApi.incoterms()
          ]);

        setCountries(countryData);
        setCurrencies(currencyData);
        setTransportModes(transportData);
        setIncoterms(incotermData);

        if (clientMode && user?.clientId) {
          const ownClient = await clientsApi.get(user.clientId);
          setClient(ownClient);
          setClientId(String(ownClient.clientId));
        } else {
          const [clientPage, supplierPage, users] = await Promise.all([
            clientsApi.list({ page: 1, pageSize: 100 }),
            suppliersApi.list({ isActive: true, page: 1, pageSize: 100 }),
            catalogsApi.requestAssignees()
          ]);
          setClients(clientPage.items.filter((x) => x.isActive));
          setSuppliers(supplierPage.items);
          setAssignees(users);
        }
      } catch (e) {
        setError(
          e instanceof HttpError
            ? e.message
            : "No se pudieron cargar los catálogos de la solicitud."
        );
      } finally {
        setLoadingCatalogs(false);
      }
    };

    initialize();
  }, [open, clientMode, user?.clientId]);

  useEffect(() => {
    if (!open) return;

    setClientId(request ? String(request.clientId) : clientMode && user?.clientId ? String(user.clientId) : undefined);
    setClientContactId(request?.clientContactId ? String(request.clientContactId) : undefined);
    setSupplierId(request?.supplierId ? String(request.supplierId) : undefined);
    setAssignedUserId(request?.assignedUserId ? String(request.assignedUserId) : undefined);
    setRequestDate(dateOnly(request?.requestDate) || new Date().toISOString().slice(0, 10));
    setTransportModeId(request?.transportModeId ? String(request.transportModeId) : undefined);
    setIncotermCode(request?.incotermCode ?? undefined);
    setOriginCountryCode(request?.originCountryCode ?? undefined);
    setDestinationCountryCode(request?.destinationCountryCode ?? "PE");
    setOriginPortId(request?.originPortId ? String(request.originPortId) : undefined);
    setDestinationPortId(request?.destinationPortId ? String(request.destinationPortId) : undefined);
    setMerchandiseDescription(request?.merchandiseDescription ?? "");
    setPackageCount(request?.packageCount?.toString() ?? "");
    setGrossWeightKg(request?.grossWeightKg?.toString() ?? "");
    setVolumeM3(request?.volumeM3?.toString() ?? "");
    setDeclaredValue(request?.declaredValue?.toString() ?? "");
    setDeclaredCurrencyCode(request?.declaredCurrencyCode ?? undefined);
    setDesiredServiceDate(dateOnly(request?.desiredServiceDate));
    setCustomerNotes(request?.customerNotes ?? "");
    setInternalNotes(request?.internalNotes ?? "");
    setError("");
  }, [open, request, clientMode, user?.clientId]);

  useEffect(() => {
    if (!clientId) {
      setClient(null);
      setClientContactId(undefined);
      return;
    }

    clientsApi.get(Number(clientId))
      .then((result) => {
        setClient(result);
        if (
          clientContactId &&
          !result.contacts.some(
            (x) =>
              x.clientContactId === Number(clientContactId) &&
              x.isActive
          )
        ) {
          setClientContactId(undefined);
        }
      })
      .catch(() => setClient(null));
  }, [clientId]);

  useEffect(() => {
    setOriginPortId(undefined);
    if (!originCountryCode) {
      setOriginPorts([]);
      return;
    }
    catalogsApi.ports(originCountryCode)
      .then(setOriginPorts)
      .catch(() => setOriginPorts([]));
  }, [originCountryCode]);

  useEffect(() => {
    setDestinationPortId(undefined);
    if (!destinationCountryCode) {
      setDestinationPorts([]);
      return;
    }
    catalogsApi.ports(destinationCountryCode)
      .then(setDestinationPorts)
      .catch(() => setDestinationPorts([]));
  }, [destinationCountryCode]);

  // Preserve existing ports when opening edit.
  useEffect(() => {
    if (request?.originCountryCode && request.originPortId) {
      catalogsApi.ports(request.originCountryCode).then((ports) => {
        setOriginPorts(ports);
        setOriginPortId(String(request.originPortId));
      });
    }
    if (request?.destinationCountryCode && request.destinationPortId) {
      catalogsApi.ports(request.destinationCountryCode).then((ports) => {
        setDestinationPorts(ports);
        setDestinationPortId(String(request.destinationPortId));
      });
    }
  }, [request?.requestId]);

  const contactOptions = useMemo(
    () =>
      (client?.contacts ?? [])
        .filter((x) => x.isActive)
        .map((x) => ({
          value: String(x.clientContactId),
          label: x.fullName,
          description: x.email ?? x.positionName ?? undefined
        })),
    [client]
  );

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (!clientId) {
      setError("Debes seleccionar un cliente.");
      return;
    }
    if (!merchandiseDescription.trim()) {
      setError("La descripción de mercancía es obligatoria.");
      return;
    }
    if (declaredValue && !declaredCurrencyCode) {
      setError("Selecciona la moneda del valor declarado.");
      return;
    }

    const payload: CreateImportRequest = {
      clientId: Number(clientId),
      clientContactId: clientContactId ? Number(clientContactId) : null,
      supplierId: clientMode ? null : supplierId ? Number(supplierId) : null,
      assignedUserId: clientMode ? null : assignedUserId ? Number(assignedUserId) : null,
      requestDate: requestDate || null,
      transportModeId: transportModeId ? Number(transportModeId) : null,
      incotermCode: incotermCode || null,
      originCountryCode: originCountryCode || null,
      destinationCountryCode: destinationCountryCode || null,
      originPortId: originPortId ? Number(originPortId) : null,
      destinationPortId: destinationPortId ? Number(destinationPortId) : null,
      merchandiseDescription: merchandiseDescription.trim(),
      packageCount: numberValue(packageCount),
      grossWeightKg: numberValue(grossWeightKg),
      volumeM3: numberValue(volumeM3),
      declaredValue: numberValue(declaredValue),
      declaredCurrencyCode: declaredCurrencyCode || null,
      desiredServiceDate: desiredServiceDate || null,
      customerNotes: customerNotes.trim() || null,
      internalNotes: clientMode ? null : internalNotes.trim() || null,
      rowVersion: request?.rowVersion ?? ""
    };

    setSaving(true);
    try {
      const saved = request
        ? await requestsApi.update(request.requestId, payload)
        : await requestsApi.create(payload);
      onSaved(saved);
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo guardar la solicitud."
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={editing ? `Editar ${request?.requestNo}` : "Nueva solicitud de importación"}
      description={
        clientMode
          ? "Registra la información que tu operador necesita para revisar y cotizar la importación."
          : "Registra el requerimiento comercial y logístico que dará origen a la cotización."
      }
      onClose={onClose}
      className="sm:max-w-6xl"
    >
      <form className="space-y-6" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <section>
          <h3 className="text-sm font-bold uppercase tracking-[0.08em] text-slate-500">
            Solicitante
          </h3>
          <div className="mt-3 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {!clientMode ? (
              <div className="xl:col-span-2">
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Cliente *</span>
                <SearchableSelect
                  value={clientId}
                  onChange={(value) => {
                    setClientId(value);
                    setClientContactId(undefined);
                  }}
                  options={clients.map((x) => ({
                    value: String(x.clientId),
                    label: x.legalName,
                    description: x.taxId ?? x.email ?? undefined
                  }))}
                  placeholder="Seleccionar cliente"
                  searchPlaceholder="Razón social, RUC..."
                />
              </div>
            ) : (
              <div className="xl:col-span-2">
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Cliente</span>
                <div className="flex h-10 items-center rounded-lg border border-slate-200 bg-slate-50 px-3 text-sm font-medium text-slate-700">
                  {client?.legalName ?? user?.clientName ?? "Mi empresa"}
                </div>
              </div>
            )}

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Contacto</span>
              <SearchableSelect
                value={clientContactId}
                onChange={setClientContactId}
                options={contactOptions}
                placeholder="Sin contacto"
                searchPlaceholder="Buscar contacto..."
              />
            </div>

            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha solicitud</span>
              <Input
                type="date"
                value={requestDate}
                onChange={(e) => setRequestDate(e.target.value)}
              />
            </label>

            {!clientMode && (
              <>
                <div className="xl:col-span-2">
                  <span className="mb-1.5 block text-sm font-semibold text-slate-700">Proveedor</span>
                  <SearchableSelect
                    value={supplierId}
                    onChange={setSupplierId}
                    options={suppliers.map((x) => ({
                      value: String(x.supplierId),
                      label: x.legalName,
                      description: x.taxId ?? x.countryCode ?? undefined
                    }))}
                    placeholder="Sin proveedor asignado"
                    searchPlaceholder="Buscar proveedor..."
                  />
                </div>
                <div className="xl:col-span-2">
                  <span className="mb-1.5 block text-sm font-semibold text-slate-700">Responsable interno</span>
                  <SearchableSelect
                    value={assignedUserId}
                    onChange={setAssignedUserId}
                    options={assignees.map((x) => ({
                      value: String(x.userId),
                      label: userName(x),
                      description: x.email ?? x.username
                    }))}
                    placeholder="Sin asignar"
                    searchPlaceholder="Buscar responsable..."
                  />
                </div>
              </>
            )}
          </div>
        </section>

        <section className="border-t border-slate-100 pt-5">
          <h3 className="text-sm font-bold uppercase tracking-[0.08em] text-slate-500">
            Transporte y ruta
          </h3>
          <div className="mt-3 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Modalidad</span>
              <SearchableSelect
                value={transportModeId}
                onChange={setTransportModeId}
                options={transportModes.map((x) => ({
                  value: String(x.transportModeId),
                  label: x.name,
                  description: x.code
                }))}
                placeholder="Seleccionar"
              />
            </div>

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Incoterm</span>
              <SearchableSelect
                value={incotermCode}
                onChange={setIncotermCode}
                options={incoterms.map((x) => ({
                  value: x.incotermCode,
                  label: x.incotermCode,
                  description: x.description
                }))}
                placeholder="Seleccionar"
                searchPlaceholder="Buscar Incoterm..."
              />
            </div>

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">País de origen</span>
              <SearchableSelect
                value={originCountryCode}
                onChange={setOriginCountryCode}
                options={countries.map((x) => ({
                  value: x.countryCode,
                  label: `${x.countryCode} — ${x.countryName}`
                }))}
                placeholder="Seleccionar"
                searchPlaceholder="Buscar país..."
              />
            </div>

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Puerto de origen</span>
              <SearchableSelect
                value={originPortId}
                onChange={setOriginPortId}
                options={originPorts.map((x) => ({
                  value: String(x.portId),
                  label: `${x.portCode ? `${x.portCode} — ` : ""}${x.portName}`,
                  description: x.cityName ?? x.portType
                }))}
                placeholder={originCountryCode ? "Seleccionar puerto" : "Primero selecciona país"}
              />
            </div>

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">País de destino</span>
              <SearchableSelect
                value={destinationCountryCode}
                onChange={setDestinationCountryCode}
                options={countries.map((x) => ({
                  value: x.countryCode,
                  label: `${x.countryCode} — ${x.countryName}`
                }))}
                placeholder="Seleccionar"
                searchPlaceholder="Buscar país..."
              />
            </div>

            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Puerto de destino</span>
              <SearchableSelect
                value={destinationPortId}
                onChange={setDestinationPortId}
                options={destinationPorts.map((x) => ({
                  value: String(x.portId),
                  label: `${x.portCode ? `${x.portCode} — ` : ""}${x.portName}`,
                  description: x.cityName ?? x.portType
                }))}
                placeholder={destinationCountryCode ? "Seleccionar puerto" : "Primero selecciona país"}
              />
            </div>

            <label className="xl:col-span-2">
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha deseada de servicio</span>
              <Input
                type="date"
                value={desiredServiceDate}
                onChange={(e) => setDesiredServiceDate(e.target.value)}
              />
            </label>
          </div>
        </section>

        <section className="border-t border-slate-100 pt-5">
          <h3 className="text-sm font-bold uppercase tracking-[0.08em] text-slate-500">
            Mercancía
          </h3>

          <div className="mt-3 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <label className="md:col-span-2 xl:col-span-4">
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">
                Descripción de mercancía *
              </span>
              <textarea
                className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm shadow-sm"
                value={merchandiseDescription}
                onChange={(e) => setMerchandiseDescription(e.target.value)}
                placeholder="Describe la mercancía con suficiente detalle para revisar transporte y aduanas."
                required
              />
            </label>

            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Bultos</span>
              <Input type="number" min="0" step="0.001" value={packageCount} onChange={(e) => setPackageCount(e.target.value)} />
            </label>
            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Peso bruto (kg)</span>
              <Input type="number" min="0" step="0.001" value={grossWeightKg} onChange={(e) => setGrossWeightKg(e.target.value)} />
            </label>
            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Volumen (m³)</span>
              <Input type="number" min="0" step="0.001" value={volumeM3} onChange={(e) => setVolumeM3(e.target.value)} />
            </label>
            <div />

            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Valor declarado</span>
              <MoneyInput min="0" value={declaredValue} onValueChange={setDeclaredValue} />
            </label>
            <div>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">Moneda</span>
              <SearchableSelect
                value={declaredCurrencyCode}
                onChange={setDeclaredCurrencyCode}
                options={currencies.map((x) => ({
                  value: x.currencyCode,
                  label: `${x.currencyCode} — ${x.currencyName}`
                }))}
                placeholder="Seleccionar moneda"
              />
            </div>
          </div>
        </section>

        <section className="border-t border-slate-100 pt-5">
          <h3 className="text-sm font-bold uppercase tracking-[0.08em] text-slate-500">
            Observaciones
          </h3>
          <div className="mt-3 grid gap-4 md:grid-cols-2">
            <label>
              <span className="mb-1.5 block text-sm font-semibold text-slate-700">
                Notas del cliente
              </span>
              <textarea
                className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
                value={customerNotes}
                onChange={(e) => setCustomerNotes(e.target.value)}
                placeholder="Indicaciones, restricciones o información que debe conocer el operador."
              />
            </label>

            {!clientMode && (
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">
                  Notas internas
                </span>
                <textarea
                  className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
                  value={internalNotes}
                  onChange={(e) => setInternalNotes(e.target.value)}
                  placeholder="Información exclusiva del equipo interno."
                />
              </label>
            )}
          </div>
        </section>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving || loadingCatalogs}>
            {editing ? "Guardar cambios" : "Crear solicitud"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
