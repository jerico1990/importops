import { AlertCircle } from "lucide-react";

export function InlineAlert({
  message
}: {
  message: string;
}) {
  return (
    <div className="flex gap-2 rounded-xl border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700">
      <AlertCircle className="mt-0.5 size-4 shrink-0" />
      <span>{message}</span>
    </div>
  );
}
