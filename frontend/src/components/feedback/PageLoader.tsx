import { Spinner } from "@/components/ui/Spinner";

export function PageLoader({
  label = "Cargando..."
}: {
  label?: string;
}) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6">
      <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-5 py-4 shadow-sm">
        <Spinner />
        <span className="text-sm font-medium text-slate-600">
          {label}
        </span>
      </div>
    </div>
  );
}
