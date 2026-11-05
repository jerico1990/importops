import { useEffect, useRef, useState, type ComponentProps, type FocusEvent } from "react";
import { Input } from "@/components/ui/Input";

export const parseMoneyInput = (value: string): string => {
  const cleaned = value.replace(/,/g, "").replace(/[^0-9.-]/g, "");
  if (cleaned === "" || cleaned === "-" || cleaned === ".") return cleaned;
  const [whole = "", ...decimalParts] = cleaned.split(".");
  return decimalParts.length ? `${whole}.${decimalParts.join("").slice(0, 2)}` : whole;
};

export const formatMoneyInput = (value?: string | number | null): string => {
  if (value === "" || value == null) return "";
  const numeric = typeof value === "number" ? value : Number(String(value).replace(/,/g, ""));
  if (!Number.isFinite(numeric)) return "";
  return new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(numeric);
};

const sanitizeMoneyTyping = (value: string): string =>
  value.replace(/[^0-9,.-]/g, "");

type MoneyInputProps = Omit<ComponentProps<typeof Input>, "type" | "value" | "onChange"> & {
  value: string | number | null | undefined;
  onValueChange: (value: string) => void;
};

export function MoneyInput({ value, onValueChange, onFocus, onBlur, inputMode = "decimal", ...props }: MoneyInputProps) {
  const [display, setDisplay] = useState(() => formatMoneyInput(value));
  const isFocusedRef = useRef(false);

  useEffect(() => {
    if (!isFocusedRef.current) {
      setDisplay(formatMoneyInput(value));
    }
  }, [value]);

  const handleFocus = (event: FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = true;
    setDisplay(value === "" || value == null ? "" : String(value));
    onFocus?.(event);
  };

  const handleBlur = (event: FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = false;
    const parsed = parseMoneyInput(display);
    setDisplay(formatMoneyInput(parseMoneyInput(display)));
    onValueChange(parsed);
    onBlur?.(event);
  };

  return (
    <Input
      {...props}
      type="text"
      inputMode={inputMode}
      value={display}
      onFocus={handleFocus}
      onBlur={handleBlur}
      onChange={(event) => {
        const typingValue = sanitizeMoneyTyping(event.target.value);
        setDisplay(typingValue);
        onValueChange(parseMoneyInput(typingValue));
      }}
    />
  );
}
