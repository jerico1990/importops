import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { shipmentsApi } from "./shipments.api";
import type { ShipmentAvailableAction, ShipmentDetail } from "./types";

export function ShipmentActionDialog({
  open,
  shipmentId,
  action,
  onClose,
  onCompleted
}: {
  open: boolean;
  shipmentId: number;
  action?: ShipmentAvailableAction | null;
  onClose: () => void;
  onCompleted: (shipment: ShipmentDetail) => void;
}) {
  const [eventAt, setEventAt] = useState("");
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setEventAt("");
      setComment("");
      setError("");
    }
  }, [open, action?.code]);

  if (!action) return null;

  const submit = async (event: FormEvent) => {
    event.preventDefault();

    if (action.requiresEventAt && !eventAt) {
      setError("La fecha/hora del evento es obligatoria.");
      return;
    }
    if (action.requiresComment && !comment.trim()) {
      setError("El motivo/comentario es obligatorio.");
      return;
    }

    setSaving(true);
    setError("");
    try {
      const result = await shipmentsApi.action(
        shipmentId,
        action.code,
        eventAt || undefined,
        comment.trim() || undefined
      );
      onCompleted(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo completar la acción.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={action.name}
      description={action.blockedReason ?? undefined}
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        {action.requiresEventAt && (
          <label className="block">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha / hora real *</span>
            <Input type="datetime-local" value={eventAt} onChange={(e) => setEventAt(e.target.value)} />
          </label>
        )}

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">
            {action.code === "CANCEL" ? "Motivo" : "Comentario"}{action.requiresComment ? " *" : ""}
          </span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button
            type="submit"
            variant={action.code === "CANCEL" ? "danger" : "primary"}
            loading={saving}
            disabled={!action.isEnabled}
          >
            {action.name}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
