import {
  useEffect,
  useState
} from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "./Sidebar";
import { Header } from "./Header";
import { cn } from "@/lib/cn";

export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] =
    useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    const saved =
      window.localStorage.getItem("importops.sidebar.collapsed");
    setSidebarCollapsed(saved === "1");
  }, []);

  const toggleDesktop = () => {
    setSidebarCollapsed((current) => {
      const next = !current;
      window.localStorage.setItem(
        "importops.sidebar.collapsed",
        next ? "1" : "0"
      );
      return next;
    });
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Sidebar
        desktopCollapsed={sidebarCollapsed}
        onToggleDesktop={toggleDesktop}
        mobileOpen={mobileOpen}
        onCloseMobile={() => setMobileOpen(false)}
      />

      <div
        className={cn(
          "min-h-screen transition-[padding] duration-200",
          sidebarCollapsed
            ? "lg:pl-[76px]"
            : "lg:pl-[292px]"
        )}
      >
        <Header onOpenMobile={() => setMobileOpen(true)} />

        <main className="mx-auto w-full max-w-[1700px] px-4 py-5 sm:px-6 sm:py-6 xl:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
