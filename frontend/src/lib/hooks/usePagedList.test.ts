import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { pagedOf } from "@/lib/testing/paged";
import type { Paged } from "@/types";

interface Row {
  id: string;
}

/** A fake endpoint over a fixed row set, so page maths is exercised, not stubbed. */
function fakeEndpoint(rows: Row[]) {
  return vi.fn(
    ({ page, pageSize }: { page: number; pageSize: number }): Promise<Paged<Row>> =>
      Promise.resolve({
        items: rows.slice((page - 1) * pageSize, page * pageSize),
        meta: {
          total: rows.length,
          page,
          pageSize,
          totalPages: Math.ceil(rows.length / pageSize),
        },
      }),
  );
}

const rows = (count: number): Row[] =>
  Array.from({ length: count }, (_, index) => ({ id: `row-${index + 1}` }));

describe("usePagedList", () => {
  it("loads the first page and reports the totals", async () => {
    const fetchPage = fakeEndpoint(rows(25));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.items).toHaveLength(10);
    expect(result.current.meta).toMatchObject({ total: 25, page: 1, totalPages: 3 });
  });

  it("fetches the page it is moved to", async () => {
    const fetchPage = fakeEndpoint(rows(25));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    act(() => result.current.setPage(3));

    await waitFor(() => expect(result.current.meta.page).toBe(3));
    expect(result.current.items).toEqual(
      ["row-21", "row-22", "row-23", "row-24", "row-25"].map((id) => ({ id })),
    );
  });

  it("returns to the first page when the page size changes", async () => {
    const fetchPage = fakeEndpoint(rows(25));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    act(() => result.current.setPage(3));
    await waitFor(() => expect(result.current.meta.page).toBe(3));

    act(() => result.current.setPageSize(25));

    await waitFor(() => expect(result.current.items).toHaveLength(25));
    expect(result.current.meta.page).toBe(1);
  });

  it("returns to the first page when a filter changes", async () => {
    const fetchPage = fakeEndpoint(rows(25));
    const { result, rerender } = renderHook(
      ({ filter }: { filter: string }) => usePagedList<Row>(fetchPage, { filters: [filter] }),
      { initialProps: { filter: "a" } },
    );
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    act(() => result.current.setPage(2));
    await waitFor(() => expect(result.current.meta.page).toBe(2));

    rerender({ filter: "b" });

    await waitFor(() => expect(result.current.meta.page).toBe(1));
  });

  it("steps back when the current page is emptied by a deletion", async () => {
    const fetchPage = vi
      .fn<(p: { page: number; pageSize: number }) => Promise<Paged<Row>>>()
      .mockResolvedValueOnce({
        items: rows(10),
        meta: { total: 11, page: 1, pageSize: 10, totalPages: 2 },
      })
      .mockResolvedValueOnce({
        items: [{ id: "row-11" }],
        meta: { total: 11, page: 2, pageSize: 10, totalPages: 2 },
      })
      // That eleventh row is deleted, so page 2 now holds nothing.
      .mockResolvedValueOnce({
        items: [],
        meta: { total: 10, page: 2, pageSize: 10, totalPages: 1 },
      })
      .mockResolvedValue({
        items: rows(10),
        meta: { total: 10, page: 1, pageSize: 10, totalPages: 1 },
      });

    const { result } = renderHook(() => usePagedList<Row>(fetchPage));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    act(() => result.current.setPage(2));
    await waitFor(() => expect(result.current.meta.page).toBe(2));

    act(() => result.current.reload());

    await waitFor(() => expect(result.current.meta.page).toBe(1));
    expect(result.current.items).toHaveLength(10);
  });

  it("surfaces the failure message from a rejected load", async () => {
    const fetchPage = vi.fn().mockRejectedValue(new Error("Forbidden"));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));

    await waitFor(() => expect(result.current.error).toBe("Forbidden"));
    expect(result.current.items).toEqual([]);
  });

  it("falls back to the supplied message when the failure carries none", async () => {
    const fetchPage = vi.fn().mockRejectedValue("boom");
    const { result } = renderHook(() =>
      usePagedList<Row>(fetchPage, { errorMessage: "Failed to load users." }),
    );

    await waitFor(() => expect(result.current.error).toBe("Failed to load users."));
  });

  it("applies a local edit without refetching", async () => {
    const fetchPage = fakeEndpoint(rows(3));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    const callsBefore = fetchPage.mock.calls.length;
    act(() => result.current.setItems((previous) => previous.slice(0, 1)));

    expect(result.current.items).toEqual([{ id: "row-1" }]);
    expect(fetchPage).toHaveBeenCalledTimes(callsBefore);
  });

  it("refetches the current page on reload", async () => {
    const fetchPage = vi.fn().mockResolvedValue(pagedOf(rows(2)));
    const { result } = renderHook(() => usePagedList<Row>(fetchPage));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    act(() => result.current.reload());

    await waitFor(() => expect(fetchPage).toHaveBeenCalledTimes(2));
  });
});
