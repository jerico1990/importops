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
import type { Currency } from "@/features/catalogs/types";
import { quotationsApi } from "./quotations.api";
import type { QuotationDetail } from "./types";
import { HttpError } from "@/lib/api/http";

const dateOnly = (value?: string | null) => value ? value.slice(0, 10) : "";

export function QuotationHeaderDialog({
  open,
  requestId,
  quotation,
  onClose,
  onSaved
}: {
  open: boolean;
  requestId?: number;
  quotation?: QuotationDetail | null;
  onClose: () => void;
  onSaved: (quotation: QuotationDetail) => void;
}) {
  const editing = Boolean(quotation);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [currencyCode, setCurrencyCode] = useState<string>();
  const [issueDate, setIssueDate] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [terms, setTerms] = useState("");
  const [internalNotes, setInternalNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    catalogsApi.currencies().then(setCurrencies).catch(() => setCurrencies([]));
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setCurrencyCode(quotation?.currencyCode ?? undefined);
    setIssueDate(dateOnly(quotation?.issueDate) || new Date().toISOString().slice(0, 10));
    setValidUntil(dateOnly(quotation?.validUntil));
    setTerms(quotation?.terms ?? "");
    setInternalNotes(quotation?.internalNotes ?? "");
    setError("");
  }, [open, quotation]);

  useEffect(() => {
    if (!currencyCode && currencies.length > 0) {
      setCurrencyCode(currencies[0].currencyCode);
    }
  }, [currencies, currencyCode]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (!currencyCode) {
      setError("Selecciona una moneda.");
      return;
    }

    setSaving(true);
    try {
      const saved = editing && quotation
        ? await quotationsApi.update(quotation.quotationId, {
            rowVersion: quotation.rowVersion,
            currencyCode,
            issueDate: issueDate || null,
            validUntil: validUntil || null,
            terms: terms.trim() || null,
            internalNotes: internalNotes.trim() || null
          })
        : await quotationsApi.createFromRequest(requestId!, {
            currencyCode,
            issueDate: issueDate || null,
            validUntil: validUntil || null,
            terms: terms.trim() || null,
            internalNotes: internalNotes.trim() || null,
            items: []
          });

      onSaved(saved);
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo guardar la cotización.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={editing ? "Editar cabecera de cotización" : "Crear cotización"}
      description={
        editing
          ? "Solo las cotizaciones en borrador pueden modificarse."
          : "Se creará un borrador vinculado a la solicitud. Luego podrás agregar los conceptos."
      }
      onClose={onClose}
      className="sm:max-w-3xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-3">
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Moneda *</span>
            <SearchableSelect
              value={currencyCode}
              onChange={setCurrencyCode}
              options={currencies.map((x) => ({
                value: x.currencyCode,
                label: `${x.currencyCode} — ${x.currencyName}`
              }))}
              placeholder="Seleccionar moneda"
            />
          </div>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Fecha emisión</span>
            <Input type="date" value={issueDate} onChange={(e) => setIssueDate(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Válida hasta</span>
            <Input type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
          </label>
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Términos comerciales</span>
          <textarea
            className="focus-ring min-h-28 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={terms}
            onChange={(e) => setTerms(e.target.value)}
            placeholder="Condiciones de pago, alcance, exclusiones, vigencia u otras condiciones comerciales."
          />
        </label>

        <label className="block">
          <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas internas</span>
          <textarea
            className="focus-ring min-h-24 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
            value={internalNotes}
            onChange={(e) => setInternalNotes(e.target.value)}
            placeholder="Información exclusiva del equipo interno."
          />
        </label>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>
            {editing ? "Guardar cambios" : "Crear borrador"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
