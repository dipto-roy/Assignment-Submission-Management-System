import { DEFAULT_PAGE_SIZE } from "@/lib/api/query";
import type { Paged } from "@/types";

/**
 * Wraps rows in the envelope the `*Page` API helpers return, so a test can stub a
 * paginated endpoint without restating the page totals every time.
 */
export function pagedOf<T>(items: T[], pageSize = DEFAULT_PAGE_SIZE): Paged<T> {
  return {
    items,
    meta: {
      total: items.length,
      page: 1,
      pageSize,
      totalPages: Math.ceil(items.length / pageSize),
    },
  };
}
