import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { CodeNameCatalog } from "@/features/catalogs/types";
import { shipmentsApi } from "./shipments.api";
import type { ShipmentContainer } from "./types";

export function ShipmentContainerDialog({
  open,
  shipmentId,
  container,
  onClose,
  onSaved
}: {
  open: boolean;
  shipmentId: number;
  container?: ShipmentContainer | null;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}) {
  const [types, setTypes] = useState<CodeNameCatalog[]>([]);
  const [containerNo, setContainerNo] = useState("");
  const [containerType, setContainerType] = useState<string>();
  const [sealNo, setSealNo] = useState("");
  const [packageCount, setPackageCount] = useState("");
  const [grossWeightKg, setGrossWeightKg] = useState("");
  const [volumeM3, setVolumeM3] = useState("");
  const [notes, setNotes] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    catalogsApi.shipmentContainerTypes().then(setTypes).catch(() => setTypes([]));
    setContainerNo(container?.containerNo ?? "");
    setContainerType(container?.containerType ?? undefined);
    setSealNo(container?.sealNo ?? "");
    setPackageCount(container?.packageCount?.toString() ?? "");
    setGrossWeightKg(container?.grossWeightKg?.toString() ?? "");
    setVolumeM3(container?.volumeM3?.toString() ?? "");
    setNotes(container?.notes ?? "");
    setIsActive(container?.isActive ?? true);
    setError("");
  }, [open, container]);

  const n = (value: string) => value.trim() === "" ? null : Number(value);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!containerNo.trim()) {
      setError("El número de contenedor es obligatorio.");
      return;
    }

    const payload = {
      containerNo: containerNo.trim(),
      containerType: containerType || null,
      sealNo: sealNo.trim() || null,
      packageCount: n(packageCount),
      grossWeightKg: n(grossWeightKg),
      volumeM3: n(volumeM3),
      notes: notes.trim() || null,
      isActive
    };

    setSaving(true);
    setError("");
    try {
      if (container) {
        await shipmentsApi.updateContainer(
          shipmentId,
          container.shipmentContainerId,
          payload
        );
      } else {
        await shipmentsApi.addContainer(shipmentId, payload);
      }
      await onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo guardar el contenedor.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={container ? "Editar contenedor" : "Agregar contenedor"}
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-2">
          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Número *</span>
            <Input value={containerNo} onChange={(e) => setContainerNo(e.target.value)} />
          </label>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo</span>
            <SearchableSelect
              value={containerType}
              onChange={setContainerType}
              options={types.map((x) => ({
                value: x.code,
                label: x.name,
                description: x.code
              }))}
            />
          </div>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Sello</span>
            <Input value={sealNo} onChange={(e) => setSealNo(e.target.value)} />
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
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas</span>
          <textarea
            className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>

        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="size-4 accent-blue-700" />
          Contenedor activo
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar contenedor</Button>
        </div>
      </form>
    </Dialog>
  );
}
