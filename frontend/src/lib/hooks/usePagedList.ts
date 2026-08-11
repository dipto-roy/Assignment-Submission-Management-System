"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { DEFAULT_PAGE_SIZE } from "@/lib/api/query";
import type { Paged, PageMeta } from "@/types";

/**
 * Page state for one server-paginated list.
 *
 * Every dashboard list needs the same four things — the current page, the rows on it, the
 * totals behind it, and a way to reload after a write — so they live here once instead of
 * being re-derived in each panel.
 *
 * Two behaviours are worth knowing about:
 *  - changing a filter resets to page 1, because page 3 of the old result set says nothing
 *    about the new one;
 *  - deleting the last row on the last page steps back a page rather than leaving the user
 *    staring at an empty list.
 */

export interface PagedListState<T> {
  items: T[];
  meta: PageMeta;
  isLoading: boolean;
  /** True while a page other than the first load is in flight — used to disable controls. */
  isRefreshing: boolean;
  error: string | null;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  /** Refetches the current page. Call after a create/delete so totals stay honest. */
  reload: () => void;
  /** Local edit of the loaded rows, for updates that return the changed row in full. */
  setItems: (update: (previous: T[]) => T[]) => void;
}

export interface PagedListOptions {
  /** Values that, when changed, invalidate the current page (filters, search, ids). */
  filters?: readonly unknown[];
  initialPageSize?: number;
  /** Message shown when the request fails without one of its own. */
  errorMessage?: string;
}

const EMPTY_META: PageMeta = { total: 0, page: 1, pageSize: DEFAULT_PAGE_SIZE, totalPages: 0 };

export function usePagedList<T>(
  fetchPage: (params: { page: number; pageSize: number }) => Promise<Paged<T>>,
  { filters = [], initialPageSize = DEFAULT_PAGE_SIZE, errorMessage = "Failed to load." }: PagedListOptions = {},
): PagedListState<T> {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [items, setItems] = useState<T[]>([]);
  const [meta, setMeta] = useState<PageMeta>({ ...EMPTY_META, pageSize: initialPageSize });
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  // The fetcher is nearly always an inline closure over the panel's filters, so a new
  // identity arrives every render. Holding it in a ref keeps the effect keyed on the
  // values that actually matter (page, size, filters) instead of refetching in a loop.
  const fetchRef = useRef(fetchPage);
  const hasLoadedRef = useRef(false);

  // Declared before the fetching effect so it has already re-pointed the ref by the time
  // that one runs; on the first commit `useRef` above has it right anyway.
  useEffect(() => {
    fetchRef.current = fetchPage;
  });

  const filterKey = JSON.stringify(filters);

  // A filter change makes the current page meaningless — go back to the first one. Done
  // during render rather than in an effect so the stale page never reaches a request.
  const [lastFilterKey, setLastFilterKey] = useState(filterKey);
  if (filterKey !== lastFilterKey) {
    setLastFilterKey(filterKey);
    setPage(1);
  }

  useEffect(() => {
    let isActive = true;

    if (hasLoadedRef.current) setIsRefreshing(true);

    fetchRef
      .current({ page, pageSize })
      .then((result) => {
        if (!isActive) return;

        // A page that came back empty behind a non-empty total means rows were removed
        // from under us; step back rather than show a blank list.
        if (result.items.length === 0 && result.meta.total > 0 && page > 1) {
          setPage(Math.min(page - 1, Math.max(result.meta.totalPages, 1)));
          return;
        }

        setItems(result.items);
        setMeta(result.meta);
        setError(null);
      })
      .catch((e: unknown) => {
        if (isActive) setError(e instanceof Error ? e.message : errorMessage);
      })
      .finally(() => {
        if (!isActive) return;
        hasLoadedRef.current = true;
        setIsLoading(false);
        setIsRefreshing(false);
      });

    return () => {
      isActive = false;
    };
  }, [page, pageSize, filterKey, reloadToken, errorMessage]);

  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  const changePageSize = useCallback((size: number) => {
    setPageSize(size);
    setPage(1);
  }, []);

  const updateItems = useCallback(
    (update: (previous: T[]) => T[]) => setItems((previous) => update(previous)),
    [],
  );

  return {
    items,
    meta,
    isLoading,
    isRefreshing,
    error,
    setPage,
    setPageSize: changePageSize,
    reload,
    setItems: updateItems,
  };
}
