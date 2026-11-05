import {
  ChevronDown,
  PanelLeftClose,
  X
} from "lucide-react";
import {
  useMemo,
  useState
} from "react";
import {
  NavLink,
  useLocation
} from "react-router-dom";
import { navigation, type NavItem } from "@/config/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { cn } from "@/lib/cn";

function Logo({
  compact = false
}: {
  compact?: boolean;
}) {
  return (
    <div className="flex min-w-0 items-center gap-2.5">
      <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-blue-600 text-xs font-extrabold tracking-tight text-white shadow-sm shadow-blue-900/20">
        R&B
      </div>
      {!compact && (
        <div className="min-w-0">
          <div className="whitespace-nowrap text-[15px] font-bold leading-5 tracking-tight text-white">
            R&B Freight Cargo S.A.C.
          </div>
          <div className="truncate text-xs font-medium text-slate-300">
            ImportOps · Importaciones y Aduanas
          </div>
        </div>
      )}
    </div>
  );
}

function SidebarItem({
  item,
  collapsed,
  closeMobile
}: {
  item: NavItem;
  collapsed: boolean;
  closeMobile: () => void;
}) {
  const location = useLocation();
  const { hasAnyPermission } = useAuth();

  const canSee =
    !item.permissions?.length ||
    hasAnyPermission(...item.permissions);

  const visibleChildren =
    item.children?.filter(
      (child) =>
        !child.permissions?.length ||
        hasAnyPermission(...child.permissions)
    ) ?? [];

  const containsActiveChild = visibleChildren.some(
    (child) =>
      child.path &&
      (location.pathname === child.path ||
        location.pathname.startsWith(`${child.path}/`))
  );

  const [open, setOpen] = useState(containsActiveChild);

  if (!canSee) return null;

  if (visibleChildren.length > 0) {
    return (
      <div>
        <button
          type="button"
          className={cn(
            "flex w-full items-center rounded-lg py-2.5 text-sm font-medium transition",
            collapsed
              ? "justify-center px-2"
              : "justify-between px-3",
            containsActiveChild
              ? "bg-white/10 text-white"
              : "text-slate-300 hover:bg-white/5 hover:text-white"
          )}
          onClick={() => setOpen((current) => !current)}
          title={collapsed ? item.label : undefined}
        >
          <span className="flex items-center gap-3">
            {item.icon && <item.icon className="size-[18px]" />}
            {!collapsed && item.label}
          </span>
          {!collapsed && (
            <ChevronDown
              className={cn(
                "size-4 transition",
                open && "rotate-180"
              )}
            />
          )}
        </button>

        {open && !collapsed && (
          <div className="ml-[21px] mt-1 space-y-1 border-l border-white/10 pl-3">
            {visibleChildren.map((child) => (
              <SidebarItem
                key={child.path ?? child.label}
                item={child}
                collapsed={false}
                closeMobile={closeMobile}
              />
            ))}
          </div>
        )}
      </div>
    );
  }

  if (!item.path) return null;

  return (
    <NavLink
      to={item.path}
      onClick={closeMobile}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) =>
        cn(
          "flex items-center gap-3 rounded-lg py-2.5 text-sm font-medium transition",
          collapsed ? "justify-center px-2" : "px-3",
          isActive
            ? "bg-blue-600 text-white shadow-sm shadow-blue-950/20"
            : "text-slate-300 hover:bg-white/5 hover:text-white"
        )
      }
    >
      {item.icon && <item.icon className="size-[18px] shrink-0" />}
      {!collapsed && <span>{item.label}</span>}
    </NavLink>
  );
}

export function Sidebar({
  desktopCollapsed,
  onToggleDesktop,
  mobileOpen,
  onCloseMobile
}: {
  desktopCollapsed: boolean;
  onToggleDesktop: () => void;
  mobileOpen: boolean;
  onCloseMobile: () => void;
}) {
  const { hasAnyPermission } = useAuth();

  const visibleGroups = useMemo(
    () =>
      navigation
        .map((group) => ({
          ...group,
          items: group.items.filter(
            (item) =>
              !item.permissions?.length ||
              hasAnyPermission(...item.permissions)
          )
        }))
        .filter((group) => group.items.length > 0),
    [hasAnyPermission]
  );

  const content = (
    <>
      <div
        className={cn(
          "flex h-18 items-center border-b border-white/10",
          desktopCollapsed
            ? "justify-center px-3"
            : "justify-between px-4"
        )}
      >
        <Logo compact={desktopCollapsed} />

        <button
          type="button"
          className="focus-ring hidden rounded-lg p-2 text-slate-400 transition hover:bg-white/5 hover:text-white lg:block"
          onClick={onToggleDesktop}
          title={
            desktopCollapsed
              ? "Expandir menú"
              : "Contraer menú"
          }
        >
          <PanelLeftClose
            className={cn(
              "size-5 transition",
              desktopCollapsed && "rotate-180"
            )}
          />
        </button>

        <button
          type="button"
          className="focus-ring rounded-lg p-2 text-slate-300 hover:bg-white/5 lg:hidden"
          onClick={onCloseMobile}
          aria-label="Cerrar menú"
        >
          <X className="size-5" />
        </button>
      </div>

      <div className="app-scrollbar flex-1 overflow-y-auto px-3 py-4">
        <nav className="space-y-5">
          {visibleGroups.map((group) => (
            <div key={group.label}>
              {!desktopCollapsed && (
                <div className="mb-2 px-3 text-[10px] font-bold uppercase tracking-[0.18em] text-slate-500">
                  {group.label}
                </div>
              )}

              <div className="space-y-1">
                {group.items.map((item) => (
                  <SidebarItem
                    key={item.path ?? item.label}
                    item={item}
                    collapsed={desktopCollapsed}
                    closeMobile={onCloseMobile}
                  />
                ))}
              </div>
            </div>
          ))}
        </nav>
      </div>

      {!desktopCollapsed && (
        <div className="border-t border-white/10 px-4 py-4">
          <div className="rounded-xl bg-white/[0.045] px-3 py-3">
            <div className="text-xs font-semibold text-slate-200">
              R&B Freight Cargo S.A.C.
            </div>
            <div className="mt-1 text-[11px] leading-relaxed text-slate-500">
              ImportOps · Gestión integral de importaciones.
            </div>
          </div>
        </div>
      )}
    </>
  );

  return (
    <>
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 hidden flex-col bg-[#102947] transition-[width] duration-200 lg:flex",
          desktopCollapsed ? "w-[76px]" : "w-[292px]"
        )}
      >
        {content}
      </aside>

      {mobileOpen && (
        <>
          <button
            type="button"
            className="fixed inset-0 z-40 bg-slate-950/45 backdrop-blur-[1px] lg:hidden"
            aria-label="Cerrar menú"
            onClick={onCloseMobile}
          />
          <aside className="fixed inset-y-0 left-0 z-50 flex w-[286px] flex-col bg-[#102947] shadow-2xl lg:hidden">
            {content}
          </aside>
        </>
      )}
    </>
  );
}
