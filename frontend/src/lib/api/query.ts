/**
 * Query-string helpers for the paginated list endpoints (plan §10.4).
 */

/** Matches `PageQuery.MaxPageSize` on the backend — larger values are clamped server-side. */
export const MAX_PAGE_SIZE = 100;

/**
 * The dashboards render whole lists rather than paged tables, so they ask for the largest
 * page the API allows. Without this they would silently show only the server default (20).
 */
export const FULL_PAGE = { pageSize: MAX_PAGE_SIZE } as const;

/**
 * Rows per page for the paginated dashboard lists. Small enough that a page fits on a
 * laptop screen without scrolling past the controls that follow it.
 */
export const DEFAULT_PAGE_SIZE = 10;

/** Offered in the "rows per page" control. Every value is <= `MAX_PAGE_SIZE`. */
export const PAGE_SIZE_OPTIONS = [10, 25, 50] as const;

export interface PageParams {
  page?: number;
  pageSize?: number;
}

type QueryValue = string | number | boolean | undefined | null;

/** Builds `?a=1&b=2`, dropping empty values. Returns "" when nothing is left. */
export function toQueryString(params: Record<string, QueryValue>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}
