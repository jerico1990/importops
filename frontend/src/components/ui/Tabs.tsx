import { cn } from "@/lib/cn";

export type TabItem = { value: string; label: string; count?: number };

export function Tabs({
  value,
  onChange,
  items
}: {
  value: string;
  onChange: (value: string) => void;
  items: TabItem[];
}) {
  return (
    <div className="app-scrollbar overflow-x-auto border-b border-slate-200">
      <div className="flex min-w-max gap-1 px-1">
        {items.map((item) => (
          <button
            key={item.value}
            type="button"
            onClick={() => onChange(item.value)}
            className={cn(
              "relative flex h-12 items-center gap-2 px-3 text-sm font-semibold transition",
              value === item.value
                ? "text-blue-700"
                : "text-slate-500 hover:text-slate-800"
            )}
          >
            {item.label}
            {item.count !== undefined && (
              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] text-slate-600">{item.count}</span>
            )}
            {value === item.value && <span className="absolute inset-x-2 bottom-0 h-0.5 rounded-full bg-blue-600" />}
          </button>
        ))}
      </div>
    </div>
  );
}
