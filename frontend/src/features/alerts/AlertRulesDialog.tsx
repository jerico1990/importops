import { useCallback, useEffect, useState } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { Skeleton } from "@/components/ui/Skeleton";
import { alertsApi } from "./alerts.api";
import type { AlertRule, AlertSeverity, UpdateAlertRuleRequest } from "./types";

function thresholdLabel(hours?: number | null) {
  if (hours == null) return "Sin umbral de tiempo";
  return `${hours} hora${hours === 1 ? "" : "s"}`;
}

export function AlertRulesDialog({
  open,
  onClose,
  onChanged
}: {
  open: boolean;
  onClose: () => void;
  onChanged: () => Promise<void> | void;
}) {
  const [rows, setRows] = useState<AlertRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [savingId, setSavingId] = useState<number | null>(null);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      setRows(await alertsApi.rules());
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar la configuración de alertas.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (open) void load();
  }, [open, load]);

  const patch = (id: number, values: Partial<AlertRule>) =>
    setRows((current) => current.map((x) => x.alertRuleId === id ? { ...x, ...values } : x));

  const save = async (rule: AlertRule) => {
    const request: UpdateAlertRuleRequest = {
      isActive: rule.isActive,
      severity: rule.severity,
      thresholdHours: rule.thresholdHours ?? null,
      audience: rule.audience
    };

    setSavingId(rule.alertRuleId);
    setError("");
    try {
      const updated = await alertsApi.updateRule(rule.alertRuleId, request);
      setRows((current) => current.map((x) => x.alertRuleId === updated.alertRuleId ? updated : x));
      await onChanged();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo guardar la regla.");
    } finally {
      setSavingId(null);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Configuración de alertas"
      description="Activa reglas, define severidad y ajusta los umbrales que utiliza el seguimiento automático."
      className="sm:max-w-5xl"
      footer={<div className="flex justify-end"><Button variant="secondary" onClick={onClose}>Cerrar</Button></div>}
    >
      {error && <div className="mb-4"><InlineAlert message={error} /></div>}

      {loading ? (
        <div className="space-y-3">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-24" />)}</div>
      ) : (
        <div className="space-y-3">
          {rows.map((rule) => (
            <div key={rule.alertRuleId} className="rounded-xl border border-slate-200 bg-white p-4">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center">
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-semibold text-slate-900">{rule.name}</h3>
                    <span className="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold uppercase tracking-wide text-slate-500">{rule.alertType}</span>
                  </div>
                  <div className="mt-1 text-xs text-slate-500">{rule.alertType} · {thresholdLabel(rule.thresholdHours)}</div>
                </div>

                <label className="flex items-center gap-2 text-xs font-semibold text-slate-600">
                  <input type="checkbox" checked={rule.isActive} onChange={(e) => patch(rule.alertRuleId, { isActive: e.target.checked })} />
                  Activa
                </label>

                <label className="flex items-center gap-2 text-xs font-semibold text-slate-600">
                  Audiencia
                  <select value={rule.audience} onChange={(e) => patch(rule.alertRuleId, { audience: e.target.value as AlertRule["audience"] })} className="focus-ring h-9 rounded-lg border border-slate-200 bg-white px-2 text-xs">
                    <option value="INTERNAL">Internos</option>
                    <option value="CLIENT">Clientes</option>
                    <option value="BOTH">Ambos</option>
                  </select>
                </label>

                <select
                  value={rule.severity}
                  onChange={(e) => patch(rule.alertRuleId, { severity: e.target.value as AlertSeverity })}
                  className="focus-ring h-9 rounded-lg border border-slate-200 bg-white px-2 text-xs font-semibold text-slate-700"
                >
                  <option value="INFO">Informativa</option>
                  <option value="WARNING">Advertencia</option>
                  <option value="CRITICAL">Crítica</option>
                </select>

                <input
                  type="number"
                  min={0}
                  max={8760}
                  value={rule.thresholdHours ?? ""}
                  placeholder="Horas"
                  title="Umbral en horas; dejar vacío para reglas sin umbral temporal."
                  onChange={(e) => patch(rule.alertRuleId, { thresholdHours: e.target.value === "" ? null : Number(e.target.value) })}
                  className="focus-ring h-9 w-28 rounded-lg border border-slate-200 bg-white px-2 text-xs text-slate-700"
                />

                <Button variant="secondary" loading={savingId === rule.alertRuleId} onClick={() => void save(rule)}>Guardar</Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </Dialog>
  );
}
