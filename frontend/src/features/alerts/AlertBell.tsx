import { Bell, CheckCheck, ChevronRight, CircleAlert, Clock3, Info, TriangleAlert } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { alertsApi } from "./alerts.api";
import type { AlertItem, AlertSeverity, AlertSummary } from "./types";

function severityIcon(severity: AlertSeverity) {
  if (severity === "CRITICAL") return <CircleAlert className="size-4 text-red-600" />;
  if (severity === "WARNING") return <TriangleAlert className="size-4 text-amber-600" />;
  return <Info className="size-4 text-blue-600" />;
}

function relativeTime(value: string) {
  const diff = Date.now() - new Date(value).getTime();
  const minutes = Math.max(0, Math.round(diff / 60000));
  if (minutes < 1) return "ahora";
  if (minutes < 60) return `hace ${minutes} min`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `hace ${hours} h`;
  const days = Math.round(hours / 24);
  return `hace ${days} d`;
}

export function AlertBell() {
  const navigate = useNavigate();
  const ref = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [latest, setLatest] = useState<AlertItem[]>([]);
  const [summary, setSummary] = useState<AlertSummary>({
    active: 0,
    unread: 0,
    critical: 0,
    warning: 0
  });

  const load = useCallback(async () => {
    try {
      const [nextSummary, nextPage] = await Promise.all([alertsApi.summary(), alertsApi.list({ page: 1, pageSize: 10 })]);
      setSummary(nextSummary);
      setLatest(nextPage.items);
    } catch {
      // El Header no debe romper la navegación si el servicio de alertas no responde.
    }
  }, []);

  useEffect(() => {
    void load();
    const timer = window.setInterval(() => void load(), 60_000);
    const onFocus = () => void load();
    window.addEventListener("focus", onFocus);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", onFocus);
    };
  }, [load]);

  useEffect(() => {
    const handler = (event: PointerEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("pointerdown", handler);
    return () => document.removeEventListener("pointerdown", handler);
  }, []);

  const openAlert = async (item: AlertItem) => {
    if (!item.isRead) {
      await alertsApi.read(item.alertId).catch(() => undefined);
    }
    setOpen(false);
    navigate(item.actionPath || "/alertas");
    void load();
  };

  const markAll = async () => {
    setLoading(true);
    try {
      await alertsApi.readAll();
      await load();
    } finally {
      setLoading(false);
    }
  };

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        className="focus-ring relative rounded-xl p-2.5 text-slate-600 hover:bg-slate-100"
        aria-label="Notificaciones"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        <Bell className="size-5" />
        {summary.unread > 0 && (
          <span className="absolute -right-1 -top-1 grid min-w-5 place-items-center rounded-full bg-red-600 px-1.5 py-0.5 text-[10px] font-bold leading-4 text-white ring-2 ring-white">
            {summary.unread > 99 ? "99+" : summary.unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-[min(92vw,410px)] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl shadow-slate-900/15">
          <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3.5">
            <div>
              <div className="font-semibold text-slate-900">Notificaciones</div>
              <div className="mt-0.5 text-xs text-slate-500">
                {summary.unread} sin leer · {summary.active} activas
              </div>
            </div>
            {summary.unread > 0 && (
              <button
                type="button"
                disabled={loading}
                className="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-semibold text-blue-700 hover:bg-blue-50 disabled:opacity-50"
                onClick={() => void markAll()}
              >
                <CheckCheck className="size-4" />
                Marcar leídas
              </button>
            )}
          </div>

          <div className="max-h-[430px] overflow-y-auto">
            {latest.length === 0 ? (
              <div className="px-5 py-10 text-center">
                <Bell className="mx-auto size-8 text-slate-300" />
                <div className="mt-3 text-sm font-semibold text-slate-700">Sin alertas activas</div>
                <div className="mt-1 text-xs text-slate-500">No hay situaciones que requieran atención.</div>
              </div>
            ) : (
              latest.map((item) => (
                <button
                  key={item.alertId}
                  type="button"
                  onClick={() => void openAlert(item)}
                  className={`flex w-full gap-3 border-b border-slate-100 px-4 py-3.5 text-left transition hover:bg-slate-50 ${item.isRead ? "bg-white" : "bg-blue-50/35"}`}
                >
                  <div className="mt-0.5 grid size-8 shrink-0 place-items-center rounded-xl bg-slate-50 ring-1 ring-slate-200">
                    {severityIcon(item.severity)}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start gap-2">
                      <div className={`min-w-0 flex-1 text-sm ${item.isRead ? "font-medium text-slate-700" : "font-semibold text-slate-900"}`}>
                        {item.title}
                      </div>
                      {!item.isRead && <span className="mt-1.5 size-2 shrink-0 rounded-full bg-blue-600" />}
                    </div>
                    <p className="mt-1 line-clamp-2 text-xs leading-5 text-slate-500">{item.message}</p>
                    <div className="mt-2 flex items-center gap-2 text-[11px] text-slate-400">
                      <Clock3 className="size-3" />
                      <span>{relativeTime(item.triggeredAt)}</span>
                      {item.operationNo && <><span>·</span><span className="font-medium text-slate-500">{item.operationNo}</span></>}
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>

          <button
            type="button"
            onClick={() => { setOpen(false); navigate("/alertas"); }}
            className="flex w-full items-center justify-center gap-2 border-t border-slate-100 px-4 py-3 text-sm font-semibold text-blue-700 hover:bg-blue-50"
          >
            Ver todas las alertas
            <ChevronRight className="size-4" />
          </button>
        </div>
      )}
    </div>
  );
}
