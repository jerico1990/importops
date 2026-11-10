export const formatDate = (value?: string | null) => {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("es-PE", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(date);
};


export const formatMoney = (value: number, currency = "USD") => {
  const raw = (currency || "USD").trim().toUpperCase();

  const normalized = ({
    PE: "PEN",
    S: "PEN",
    SOL: "PEN",
    SOLES: "PEN",
    US: "USD",
    DOLAR: "USD"
  } as Record<string, string>)[raw] ?? raw;

  try {
    return new Intl.NumberFormat("es-PE", {
      style: "currency",
      currency: normalized,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  } catch (error) {
    if (!(error instanceof RangeError)) throw error;

    return `${normalized || "?"} ${new Intl.NumberFormat("es-PE", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value)}`;
  }
};



export const formatAmount = (value: number) =>
  new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
