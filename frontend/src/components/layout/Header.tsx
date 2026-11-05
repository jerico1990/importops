import {
  Bell,
  ChevronDown,
  Menu,
  Search
} from "lucide-react";
import {
  useEffect,
  useRef,
  useState
} from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthContext";
import { Badge } from "@/components/ui/Badge";
import { alertsApi } from "@/features/alerts/alerts.api";
import { Permissions } from "@/types/auth";

function initials(
  firstName?: string | null,
  lastName?: string | null,
  username?: string
) {
  const parts = [firstName, lastName]
    .filter(Boolean)
    .map((value) => value![0]?.toUpperCase())
    .join("");

  return parts || username?.slice(0, 2).toUpperCase() || "US";
}

export function Header({
  onOpenMobile
}: {
  onOpenMobile: () => void;
}) {
  const { user, logout, hasPermission } = useAuth();
  const navigate = useNavigate();
  const [profileOpen, setProfileOpen] = useState(false);
  const [alertsOpen, setAlertsOpen] = useState(false);
  const [alertSummary, setAlertSummary] = useState({ active: 0, unread: 0, critical: 0, warning: 0 });
  const [recentAlerts, setRecentAlerts] = useState<import("@/features/alerts/types").AlertItem[]>([]);
  const profileRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!user || !hasPermission(Permissions.AlertsView)) return;
    const loadAlerts = async () => {
      try {
        const [summary, page] = await Promise.all([alertsApi.summary(), alertsApi.list({ status: "ACTIVE", pageSize: 5 })]);
        setAlertSummary(summary); setRecentAlerts(page.items);
      } catch { /* la cabecera no debe bloquear la aplicación */ }
    };
    void loadAlerts();
    const id = window.setInterval(() => void loadAlerts(), 60000);
    return () => window.clearInterval(id);
  }, [user, hasPermission]);

  useEffect(() => {
    const handler = (event: PointerEvent) => {
      if (
        profileRef.current &&
        !profileRef.current.contains(event.target as Node)
      ) {
        setProfileOpen(false);
      }
    };

    document.addEventListener("pointerdown", handler);
    return () =>
      document.removeEventListener("pointerdown", handler);
  }, []);

  return (
    <header className="sticky top-0 z-30 border-b border-slate-200/80 bg-white/90 backdrop-blur">
      <div className="flex h-18 items-center gap-3 px-4 sm:px-6">
        <button
          type="button"
          className="focus-ring rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden"
          onClick={onOpenMobile}
          aria-label="Abrir menú"
        >
          <Menu className="size-5" />
        </button>

        <div className="relative hidden min-w-0 flex-1 md:block">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
          <input
            type="search"
            placeholder="Buscar operación, BL, DAM, cliente..."
            className="focus-ring h-10 w-full max-w-xl rounded-xl border border-slate-200 bg-slate-50/80 pl-9 pr-3 text-sm text-slate-800 placeholder:text-slate-400"
          />
        </div>

        <div className="ml-auto flex items-center gap-1.5 sm:gap-2">
          {hasPermission(Permissions.AlertsView) && (
            <div className="relative">
              <button type="button" className="focus-ring relative rounded-xl p-2.5 text-slate-600 hover:bg-slate-100" aria-label="Notificaciones" onClick={() => setAlertsOpen(v => !v)}>
                <Bell className="size-5" />
                {alertSummary.unread > 0 && <span className="absolute -right-0.5 -top-0.5 grid min-w-5 place-items-center rounded-full bg-red-600 px-1 text-[10px] font-bold leading-5 text-white">{alertSummary.unread > 99 ? "99+" : alertSummary.unread}</span>}
              </button>
              {alertsOpen && <div className="absolute right-0 mt-2 w-[360px] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl shadow-slate-900/10">
                <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3"><div><div className="font-semibold text-slate-900">Notificaciones</div><div className="text-xs text-slate-500">{alertSummary.active} activas · {alertSummary.unread} sin leer</div></div><Badge tone={alertSummary.critical > 0 ? "red" : "amber"}>{alertSummary.critical} críticas</Badge></div>
                <div className="max-h-80 overflow-auto">{recentAlerts.length === 0 ? <div className="px-4 py-8 text-center text-sm text-slate-500">No tienes alertas activas.</div> : recentAlerts.map(a => <button key={a.alertId} className={`block w-full border-b border-slate-100 px-4 py-3 text-left hover:bg-slate-50 ${!a.isRead ? "bg-blue-50/40" : ""}`} onClick={async()=>{await alertsApi.read(a.alertId);setAlertsOpen(false);if(a.actionPath)navigate(a.actionPath);else navigate("/alertas")}}><div className="flex items-center gap-2"><span className={`size-2 rounded-full ${a.severity === "CRITICAL" ? "bg-red-500" : a.severity === "WARNING" ? "bg-amber-500" : "bg-blue-500"}`} /><span className="truncate text-sm font-semibold text-slate-800">{a.title}</span></div><div className="mt-1 line-clamp-2 text-xs leading-5 text-slate-500">{a.message}</div></button>)}</div>
                <button className="w-full px-4 py-3 text-sm font-semibold text-blue-700 hover:bg-blue-50" onClick={()=>{setAlertsOpen(false);navigate("/alertas")}}>Ver todas las alertas</button>
              </div>}
            </div>
          )}

          <div
            ref={profileRef}
            className="relative"
          >
            <button
              type="button"
              className="focus-ring flex items-center gap-2 rounded-xl px-2 py-1.5 hover:bg-slate-100"
              onClick={() =>
                setProfileOpen((current) => !current)
              }
            >
              <div className="grid size-9 place-items-center rounded-xl bg-blue-100 text-xs font-bold text-blue-800">
                {initials(
                  user?.firstName,
                  user?.lastName,
                  user?.username
                )}
              </div>

              <div className="hidden max-w-40 text-left lg:block">
                <div className="truncate text-sm font-semibold text-slate-800">
                  {[user?.firstName, user?.lastName]
                    .filter(Boolean)
                    .join(" ") || user?.username}
                </div>
                <div className="truncate text-[11px] text-slate-500">
                  {user?.userType === "CLIENT"
                    ? user.clientName ?? "Cliente"
                    : user?.roles[0] ?? "Usuario"}
                </div>
              </div>

              <ChevronDown className="hidden size-4 text-slate-400 lg:block" />
            </button>

            {profileOpen && (
              <div className="absolute right-0 mt-2 w-64 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl shadow-slate-900/10">
                <div className="border-b border-slate-100 px-4 py-3">
                  <div className="text-sm font-semibold text-slate-900">
                    {user?.username}
                  </div>
                  <div className="mt-0.5 text-xs text-slate-500">
                    {user?.email || "Sin correo registrado"}
                  </div>
                </div>

                <div className="p-1.5">
                  <button
                    type="button"
                    className="w-full rounded-lg px-3 py-2 text-left text-sm text-slate-700 hover:bg-slate-50"
                    onClick={() => {
                      setProfileOpen(false);
                      navigate("/perfil");
                    }}
                  >
                    Mi perfil
                  </button>
                  <button
                    type="button"
                    className="w-full rounded-lg px-3 py-2 text-left text-sm font-medium text-red-600 hover:bg-red-50"
                    onClick={async () => {
                      await logout();
                      navigate("/login", { replace: true });
                    }}
                  >
                    Cerrar sesión
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
