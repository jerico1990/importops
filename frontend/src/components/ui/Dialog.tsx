import { X } from "lucide-react";
import { useEffect, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  className
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-end justify-center sm:items-center sm:p-5">
      <button
        type="button"
        className="absolute inset-0 bg-slate-950/40 backdrop-blur-[1px]"
        aria-label="Cerrar"
        onClick={onClose}
      />
      <section
        className={cn(
          "relative z-10 flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-3xl border border-slate-200 bg-white shadow-2xl sm:max-w-2xl sm:rounded-2xl",
          className
        )}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-100 px-5 py-4 sm:px-6">
          <div>
            <h2 className="text-lg font-semibold text-slate-950">{title}</h2>
            {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
          </div>
          <button
            type="button"
            className="focus-ring rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            onClick={onClose}
          >
            <X className="size-5" />
          </button>
        </header>
        <div className="app-scrollbar min-h-0 flex-1 overflow-y-auto px-5 py-5 sm:px-6">{children}</div>
        {footer && <footer className="border-t border-slate-100 bg-slate-50/60 px-5 py-4 sm:px-6">{footer}</footer>}
      </section>
    </div>
  );
}
