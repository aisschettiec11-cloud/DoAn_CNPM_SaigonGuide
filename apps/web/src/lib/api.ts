let csrf: string | null = null;
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
  }
}
export async function refreshCsrf() {
  const r = await fetch("/api/v1/csrf", {
    credentials: "same-origin",
    cache: "no-store",
  });
  if (!r.ok) throw new ApiError("Không thể kết nối máy chủ.", r.status);
  csrf = (await r.json()).token;
  return csrf!;
}
export async function api<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const method = options.method ?? "GET";
  const headers = new Headers(options.headers);
  if (!(options.body instanceof FormData) && options.body)
    headers.set("Content-Type", "application/json");
  if (!["GET", "HEAD"].includes(method))
    headers.set("X-CSRF-TOKEN", csrf ?? (await refreshCsrf()));
  let r: Response;
  try {
    r = await fetch(`/api/v1${path}`, {
      ...options,
      headers,
      credentials: "same-origin",
      cache: "no-store",
    });
  } catch {
    throw new ApiError(
      "Không có kết nối. Vui lòng kiểm tra mạng và thử lại.",
      0,
    );
  }
  if (!r.ok) {
    let body: {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    } = {};
    try {
      body = await r.json();
    } catch {
      /* non-JSON response */
    }
    if (r.status === 403) csrf = null;
    throw new ApiError(
      body.detail ??
        (body.errors
          ? Object.values(body.errors).flat().join(" ")
          : undefined) ??
        body.title ??
        `Yêu cầu thất bại (${r.status}).`,
      r.status,
    );
  }
  if (r.status === 204) return undefined as T;
  return r.json();
}
export const post = <T>(path: string, body: unknown) =>
  api<T>(path, { method: "POST", body: JSON.stringify(body) });
