import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { Country, Port, TransportMode } from "@/features/catalogs/types";
import { operationsApi } from "@/features/operations/operations.api";
import type { OperationDetail } from "@/features/operations/types";
import { shipmentsApi } from "./shipments.api";
import type { ShipmentDetail } from "./types";
import { HttpError } from "@/lib/api/http";

const dateTimeLocal = (value?: string | null) => value ? value.slice(0, 16) : "";

export function ShipmentFormDialog({
  open,
  operationId,
  shipment,
  onClose,
  onSaved
}: {
  open: boolean;
  operationId: number;
  shipment?: ShipmentDetail | null;
  onClose: () => void;
  onSaved: (shipment: ShipmentDetail) => void;
}) {
  const editing = Boolean(shipment);
  const [operation, setOperation] = useState<OperationDetail | null>(null);
  const [modes, setModes] = useState<TransportMode[]>([]);
  const [countries, setCountries] = useState<Country[]>([]);
  const [originPorts, setOriginPorts] = useState<Port[]>([]);
  const [destinationPorts, setDestinationPorts] = useState<Port[]>([]);

  const [transportModeId, setTransportModeId] = useState<string>();
  const [carrierName, setCarrierName] = useState("");
  const [vesselFlightName, setVesselFlightName] = useState("");
  const [bookingNo, setBookingNo] = useState("");
  const [masterDocumentNo, setMasterDocumentNo] = useState("");
  const [houseDocumentNo, setHouseDocumentNo] = useState("");
  const [originCountryCode, setOriginCountryCode] = useState<string>();
  const [destinationCountryCode, setDestinationCountryCode] = useState<string>();
  const [originPortId, setOriginPortId] = useState<string>();
  const [destinationPortId, setDestinationPortId] = useState<string>();
  const [etd, setEtd] = useState("");
  const [eta, setEta] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    Promise.all([
      operationsApi.get(operationId),
      catalogsApi.transportModes(),
      catalogsApi.countries()
    ]).then(([op, modeData, countryData]) => {
      setOperation(op);
      setModes(modeData);
      setCountries(countryData);
    }).catch((e) => {
      setError(e instanceof Error ? e.message : "No se pudieron cargar los datos de la operación.");
    });
  }, [open, operationId]);

  useEffect(() => {
    if (!open) return;
    setTransportModeId(
      shipment?.transportModeId
        ? String(shipment.transportModeId)
        : operation?.transportModeId
          ? String(operation.transportModeId)
          : undefined
    );
    setOriginCountryCode(operation?.originCountryCode ?? undefined);
    setDestinationCountryCode(operation?.destinationCountryCode ?? undefined);
    setCarrierName(shipment?.carrierName ?? "");
    setVesselFlightName(shipment?.vesselFlightName ?? "");
    setBookingNo(shipment?.bookingNo ?? "");
    setMasterDocumentNo(shipment?.masterDocumentNo ?? "");
    setHouseDocumentNo(shipment?.houseDocumentNo ?? "");
    setOriginPortId(
      shipment?.originPortId
        ? String(shipment.originPortId)
        : operation?.originPortId
          ? String(operation.originPortId)
          : undefined
    );
    setDestinationPortId(
      shipment?.destinationPortId
        ? String(shipment.destinationPortId)
        : operation?.destinationPortId
          ? String(operation.destinationPortId)
          : undefined
    );
    setEtd(dateTimeLocal(shipment?.etd));
    setEta(dateTimeLocal(shipment?.eta));
    setNotes(shipment?.notes ?? "");
    setError("");
  }, [open, shipment, operation]);

  const selectedMode = useMemo(
    () => modes.find((x) => String(x.transportModeId) === transportModeId),
    [modes, transportModeId]
  );

  useEffect(() => {
    if (!open) return;
    const portType = selectedMode?.code === "SEA" ? "SEA" : selectedMode?.code === "AIR" ? "AIR" : undefined;
    if (!originCountryCode) { setOriginPorts([]); return; }
    catalogsApi.ports(originCountryCode, portType).then(setOriginPorts).catch(() => setOriginPorts([]));
  }, [open, originCountryCode, selectedMode?.code]);

  useEffect(() => {
    if (!open) return;
    const portType = selectedMode?.code === "SEA" ? "SEA" : selectedMode?.code === "AIR" ? "AIR" : undefined;
    if (!destinationCountryCode) { setDestinationPorts([]); return; }
    catalogsApi.ports(destinationCountryCode, portType).then(setDestinationPorts).catch(() => setDestinationPorts([]));
  }, [open, destinationCountryCode, selectedMode?.code]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!transportModeId) {
      setError("Selecciona la modalidad de transporte.");
      return;
    }
    if (["SEA", "AIR"].includes(selectedMode?.code ?? "") && (!originCountryCode || !destinationCountryCode || !originPortId || !destinationPortId)) {
      setError("País, origen y destino son obligatorios para transporte marítimo/aéreo.");
      return;
    }
    if (etd && eta && new Date(eta) <= new Date(etd)) {
      setError("ETA debe ser posterior a ETD.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      const payload = {
        transportModeId: Number(transportModeId),
        carrierName: carrierName.trim() || null,
        vesselFlightName: vesselFlightName.trim() || null,
        bookingNo: bookingNo.trim() || null,
        masterDocumentNo: masterDocumentNo.trim() || null,
        houseDocumentNo: houseDocumentNo.trim() || null,
        originPortId: originPortId ? Number(originPortId) : null,
        destinationPortId: destinationPortId ? Number(destinationPortId) : null,
        etd: etd || null,
        eta: eta || null,
        notes: notes.trim() || null
      };

      const saved = shipment
        ? await shipmentsApi.update(shipment.shipmentId, {
            ...payload,
            rowVersion: shipment.rowVersion
          })
        : await shipmentsApi.create(operationId, payload);

      onSaved(saved);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo guardar el embarque.");
    } finally {
      setSaving(false);
    }
  };

  const masterLabel =
    selectedMode?.code === "AIR" ? "MAWB" :
    selectedMode?.code === "SEA" ? "MBL" :
    "Documento maestro";

  const houseLabel =
    selectedMode?.code === "AIR" ? "HAWB" :
    selectedMode?.code === "SEA" ? "HBL" :
    "Documento house";

  return (
    <Dialog
      open={open}
      title={editing ? "Editar embarque" : "Nuevo embarque"}
      description="Los datos logísticos quedan vinculados al expediente de operación."
      onClose={onClose}
      className="sm:max-w-4xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Modalidad *</span>
            <SearchableSelect
              value={transportModeId}
              onChange={setTransportModeId}
              options={modes.map((x) => ({
                value: String(x.transportModeId),
                label: x.name,
                description: x.code
              }))}
            />
          </div>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Carrier / transportista</span>
            <Input value={carrierName} onChange={(e) => setCarrierName(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              {selectedMode?.code === "AIR" ? "Vuelo" : selectedMode?.code === "SEA" ? "Nave / viaje" : "Unidad / referencia"}
            </span>
            <Input value={vesselFlightName} onChange={(e) => setVesselFlightName(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Booking</span>
            <Input value={bookingNo} onChange={(e) => setBookingNo(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">{masterLabel}</span>
            <Input value={masterDocumentNo} onChange={(e) => setMasterDocumentNo(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">{houseLabel}</span>
            <Input value={houseDocumentNo} onChange={(e) => setHouseDocumentNo(e.target.value)} />
          </label>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">País origen *</span>
            <SearchableSelect value={originCountryCode} onChange={(v) => { setOriginCountryCode(v); setOriginPortId(undefined); }} options={countries.map((x) => ({ value: x.countryCode, label: x.countryName, description: x.countryCode }))} placeholder="Seleccionar país" />
          </div>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">País destino *</span>
            <SearchableSelect value={destinationCountryCode} onChange={(v) => { setDestinationCountryCode(v); setDestinationPortId(undefined); }} options={countries.map((x) => ({ value: x.countryCode, label: x.countryName, description: x.countryCode }))} placeholder="Seleccionar país" />
          </div>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Puerto / terminal origen *</span>
            <SearchableSelect
              value={originPortId}
              onChange={setOriginPortId}
              options={originPorts.map((x) => ({
                value: String(x.portId),
                label: x.portName,
                description: x.portCode ?? x.cityName ?? undefined
              }))}
              placeholder="Seleccionar origen"
            />
          </div>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Puerto / terminal destino *</span>
            <SearchableSelect
              value={destinationPortId}
              onChange={setDestinationPortId}
              options={destinationPorts.map((x) => ({
                value: String(x.portId),
                label: x.portName,
                description: x.portCode ?? x.cityName ?? undefined
              }))}
              placeholder="Seleccionar destino"
            />
          </div>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">ETD</span>
            <Input type="datetime-local" value={etd} onChange={(e) => setEtd(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">ETA</span>
            <Input type="datetime-local" value={eta} onChange={(e) => setEta(e.target.value)} />
          </label>
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas internas</span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>{editing ? "Guardar cambios" : "Crear embarque"}</Button>
        </div>
      </form>
    </Dialog>
  );
}
