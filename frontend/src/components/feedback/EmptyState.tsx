import { Inbox } from "lucide-react";
import type { ReactNode } from "react";

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="px-5 py-12 text-center">
      <div className="mx-auto grid size-11 place-items-center rounded-xl bg-slate-100 text-slate-500"><Inbox className="size-5" /></div>
      <h3 className="mt-4 text-sm font-semibold text-slate-800">{title}</h3>
      {description && <p className="mx-auto mt-1 max-w-md text-sm leading-6 text-slate-500">{description}</p>}
      {action && <div className="mt-5 flex justify-center">{action}</div>}
    </div>
  );
}
