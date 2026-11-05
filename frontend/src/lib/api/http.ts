export type ApiErrorPayload = {
  code?: string;
  title?: string;
  detail?: string;
  message?: string;
  traceId?: string;
  correlationId?: string;
};

export class HttpError extends Error {
  status: number;
  code?: string;
  payload?: ApiErrorPayload;

  constructor(
    status: number,
    message: string,
    payload?: ApiErrorPayload
  ) {
    super(message);
    this.name = "HttpError";
    this.status = status;
    this.code = payload?.code;
    this.payload = payload;
  }
}

const configuredBaseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, "") ?? "";

function buildUrl(path: string) {
  if (/^https?:\/\//i.test(path)) return path;
  return `${configuredBaseUrl}${path.startsWith("/") ? path : `/${path}`}`;
}

async function parseBody(response: Response) {
  const type = response.headers.get("content-type") ?? "";

  if (response.status === 204) return undefined;

  if (type.includes("application/json")) {
    return response.json();
  }

  return response.text();
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const headers = new Headers(init.headers);

  if (
    init.body &&
    !(init.body instanceof FormData) &&
    !headers.has("Content-Type")
  ) {
    headers.set("Content-Type", "application/json");
  }

  headers.set("Accept", "application/json");

  const response = await fetch(buildUrl(path), {
    ...init,
    headers,
    credentials: "include"
  });

  const body = await parseBody(response);

  if (!response.ok) {
    const payload =
      body && typeof body === "object"
        ? (body as ApiErrorPayload)
        : undefined;

    const message =
      payload?.detail ??
      payload?.message ??
      payload?.title ??
      (typeof body === "string" ? body : undefined) ??
      `Error HTTP ${response.status}`;

    throw new HttpError(response.status, message, payload);
  }

  return body as T;
}

export const api = {
  get: <T>(path: string, init?: RequestInit) =>
    apiRequest<T>(path, { ...init, method: "GET" }),

  post: <T>(path: string, body?: unknown, init?: RequestInit) =>
    apiRequest<T>(path, {
      ...init,
      method: "POST",
      body:
        body === undefined
          ? undefined
          : body instanceof FormData
            ? body
            : JSON.stringify(body)
    }),

  put: <T>(path: string, body?: unknown, init?: RequestInit) =>
    apiRequest<T>(path, {
      ...init,
      method: "PUT",
      body:
        body === undefined
          ? undefined
          : body instanceof FormData
            ? body
            : JSON.stringify(body)
    }),

  delete: <T>(path: string, init?: RequestInit) =>
    apiRequest<T>(path, { ...init, method: "DELETE" })
};


export async function downloadFile(path: string): Promise<{ blob: Blob; fileName: string }> {
  const response = await fetch(buildUrl(path), { method: "GET", credentials: "include", headers: { Accept: "application/pdf" } });
  if (!response.ok) {
    const body = await parseBody(response);
    const payload = body && typeof body === "object" ? body as ApiErrorPayload : undefined;
    throw new HttpError(response.status, payload?.detail ?? payload?.message ?? payload?.title ?? `Error HTTP ${response.status}`, payload);
  }
  const disposition = response.headers.get("content-disposition") ?? "";
  const utf = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  const plain = disposition.match(/filename=\"?([^\";]+)\"?/i);
  const fileName = decodeURIComponent(utf?.[1] ?? plain?.[1] ?? "cotizacion.pdf");
  return { blob: await response.blob(), fileName };
}
