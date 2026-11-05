import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { quotationsApi } from "./quotations.api";
import type { QuotationDetail } from "./types";

export function NewQuotationVersionDialog({
  open,
  quotation,
  onClose,
  onCreated
}: {
  open: boolean;
  quotation: QuotationDetail;
  onClose: () => void;
  onCreated: (quotation: QuotationDetail) => void;
}) {
  const [validUntil, setValidUntil] = useState("");
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setValidUntil(quotation.validUntil?.slice(0, 10) ?? "");
    setComment("");
    setError("");
  }, [open, quotation]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      const created = await quotationsApi.newVersion(quotation.quotationId, {
        validUntil: validUntil || null,
        comment: comment.trim() || null
      });
      onCreated(created);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo crear la nueva versión.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title="Crear nueva versión"
      description={`Se copiarán cabecera y conceptos de ${quotation.quotationNo} V${quotation.versionNo} a un nuevo borrador.`}
      onClose={onClose}
      className="sm:max-w-xl"
    >
      <form className="space-y-4" onSubmit={submit}>
        {error && <InlineAlert message={error} />}
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Nueva vigencia</span>
          <Input type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Motivo / comentario</span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Ej. Ajuste comercial solicitado por el cliente."
          />
        </label>
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Crear versión</Button>
        </div>
      </form>
    </Dialog>
  );
}
