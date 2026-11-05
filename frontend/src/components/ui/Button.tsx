import type { ButtonHTMLAttributes, ReactNode } from "react";
import { LoaderCircle } from "lucide-react";
import { cn } from "@/lib/cn";

type Variant = "primary" | "secondary" | "ghost" | "danger";

export function Button({
  children,
  variant = "primary",
  loading = false,
  className,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode;
  variant?: Variant;
  loading?: boolean;
}) {
  const variants: Record<Variant, string> = {
    primary:
      "bg-blue-700 text-white shadow-sm hover:bg-blue-800 active:bg-blue-900",
    secondary:
      "border border-slate-200 bg-white text-slate-700 shadow-sm hover:bg-slate-50",
    ghost:
      "bg-transparent text-slate-600 hover:bg-slate-100 hover:text-slate-900",
    danger:
      "bg-red-600 text-white shadow-sm hover:bg-red-700"
  };

  return (
    <button
      className={cn(
        "focus-ring inline-flex min-h-10 items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-55",
        variants[variant],
        className
      )}
      disabled={disabled || loading}
      {...props}
    >
      {loading && <LoaderCircle className="size-4 animate-spin" />}
      {children}
    </button>
  );
}
