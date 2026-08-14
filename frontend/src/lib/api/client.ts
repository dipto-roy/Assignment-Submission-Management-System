import type { ApiResponse, Paged, PageMeta } from "@/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1";

const TOKEN_STORAGE_KEY = "assignment_submission_jwt";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string): void {
  window.localStorage.setItem(TOKEN_STORAGE_KEY, token);
}

export function clearToken(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
}

interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
}

/**
 * Typed fetch wrapper: attaches JWT, throws on non-2xx with a readable message,
 * and normalizes responses to ApiResponse<T>.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    const message = await safeErrorMessage(response);
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const json = (await response.json()) as ApiResponse<T> | T;
  if (isApiResponse<T>(json)) {
    if (!json.success) {
      throw new Error(json.error ?? "Request failed.");
    }
    return json.data as T;
  }

  return json;
}

/**
 * Same request as `apiFetch`, but keeps the `meta` envelope field that paginated list
 * endpoints put their totals in. `apiFetch` throws that away, which is why a caller that
 * needs to render page controls has to come through here instead.
 *
 * A server that answers without `meta` (or with a partial one) still yields a usable page
 * describing the rows that did arrive, so a missing envelope degrades to a single page
 * rather than to a crash.
 */
export async function apiFetchPaged<T>(
  path: string,
  options: RequestOptions = {},
): Promise<Paged<T>> {
  const token = getToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    throw new Error(await safeErrorMessage(response));
  }

  const json = (await response.json()) as ApiResponse<T[]> | T[];

  if (Array.isArray(json)) {
    return { items: json, meta: singlePageMeta(json.length) };
  }

  if (!json.success) {
    throw new Error(json.error ?? "Request failed.");
  }

  const items = json.data ?? [];
  return { items, meta: normalizeMeta(json.meta, items.length) };
}

function singlePageMeta(count: number): PageMeta {
  return { total: count, page: 1, pageSize: count || 1, totalPages: count === 0 ? 0 : 1 };
}

function normalizeMeta(meta: PageMeta | null | undefined, count: number): PageMeta {
  if (!meta || typeof meta.total !== "number") return singlePageMeta(count);

  const pageSize = meta.pageSize > 0 ? meta.pageSize : count || 1;
  return {
    total: meta.total,
    page: meta.page > 0 ? meta.page : 1,
    pageSize,
    totalPages: meta.totalPages ?? Math.ceil(meta.total / pageSize),
  };
}

/**
 * Multipart upload. Separate from `apiFetch` because that helper always sets a JSON
 * Content-Type and stringifies its body; a multipart request needs the browser to set the
 * header itself so it can add the boundary token.
 */
export async function apiUpload<T>(path: string, file: File): Promise<T> {
  const token = getToken();
  const form = new FormData();

  // Part name must be "file" — it is what the endpoints bind against.
  form.append("file", file);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: form,
  });

  if (!response.ok) {
    throw new Error(await safeErrorMessage(response));
  }

  const json = (await response.json()) as ApiResponse<T> | T;
  if (isApiResponse<T>(json)) {
    if (!json.success) {
      throw new Error(json.error ?? "Upload failed.");
    }
    return json.data as T;
  }

  return json;
}

/**
 * Fetches a protected file as a blob.
 *
 * Downloads cannot be a plain anchor href: the endpoint requires an Authorization header,
 * which a browser navigation will not send. The bytes are pulled here and handed to a
 * temporary object URL instead.
 */
export async function apiDownloadBlob(path: string): Promise<Blob> {
  const token = getToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new Error(await safeErrorMessage(response));
  }

  return response.blob();
}

function isApiResponse<T>(value: unknown): value is ApiResponse<T> {
  return typeof value === "object" && value !== null && "success" in value;
}

async function safeErrorMessage(response: Response): Promise<string> {
  try {
    const body = await response.json();
    return body?.error ?? body?.title ?? `Request failed with status ${response.status}.`;
  } catch {
    return `Request failed with status ${response.status}.`;
  }
}
