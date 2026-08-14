"use client";

import { PAGE_SIZE_OPTIONS } from "@/lib/api/query";
import { Icon } from "@/components/ui/Icon";
import type { PageMeta } from "@/types";

/**
 * Page controls for the dashboard lists.
 *
 * Renders as a `nav` with an accessible name so a screen reader can skip past it, and
 * announces the visible range in a live region because paging swaps the rows above without
 * moving focus. Page numbers are windowed (`…` on either side) so a 40-page list does not
 * produce 40 buttons.
 */

export interface PaginationProps {
  meta: PageMeta;
  onPageChange: (page: number) => void;
  /** Omit to hide the rows-per-page control (lists whose size is fixed by the caller). */
  onPageSizeChange?: (pageSize: number) => void;
  /** Names what is being paged, e.g. "users" — used in the range summary and labels. */
  label: string;
  isBusy?: boolean;
}

/** How many numbered pages flank the current one before the list collapses to an ellipsis. */
const WINDOW_RADIUS = 1;

export function Pagination({
  meta,
  onPageChange,
  onPageSizeChange,
  label,
  isBusy = false,
}: PaginationProps) {
  const { total, page, pageSize, totalPages } = meta;

  // A single page of results needs no controls — but the size selector still earns its keep
  // when the caller offers one, since it is what gets you off a single oversized page.
  if (totalPages <= 1 && !onPageSizeChange) return null;

  const firstRow = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const lastRow = Math.min(page * pageSize, total);

  return (
    <nav
      aria-label={`${label} pagination`}
      className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-border-subtle pt-3"
    >
      <p aria-live="polite" className="text-xs text-foreground-muted">
        {total === 0 ? (
          `No ${label}`
        ) : (
          <>
            Showing <span className="font-mono text-foreground">{firstRow}</span>–
            <span className="font-mono text-foreground">{lastRow}</span> of{" "}
            <span className="font-mono text-foreground">{total}</span> {label}
          </>
        )}
      </p>

      <div className="flex flex-wrap items-center gap-2">
        {onPageSizeChange && (
          <label className="flex items-center gap-1.5 text-xs text-foreground-muted">
            Rows
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              aria-label={`${label} per page`}
              className="min-h-9 rounded-lg border border-border-strong bg-surface px-2 py-1 text-xs text-foreground shadow-xs transition-[border-color] duration-150 hover:border-primary/60 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring"
            >
              {PAGE_SIZE_OPTIONS.map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </label>
        )}

        {totalPages > 1 && (
          <div className="flex items-center gap-1">
            <PageButton
              onClick={() => onPageChange(page - 1)}
              disabled={isBusy || page <= 1}
              ariaLabel="Previous page"
            >
              <Icon name="arrow-left" size="sm" />
            </PageButton>

            {buildPageWindow(page, totalPages).map((entry, index) =>
              entry === "gap" ? (
                <span
                  // Index is safe here: the window is derived purely from page/totalPages,
                  // so a given position always holds the same kind of entry.
                  key={`gap-${index}`}
                  aria-hidden="true"
                  className="px-1 text-xs text-foreground-subtle"
                >
                  …
                </span>
              ) : (
                <PageButton
                  key={entry}
                  onClick={() => onPageChange(entry)}
                  disabled={isBusy}
                  isCurrent={entry === page}
                  ariaLabel={`Page ${entry}`}
                >
                  <span className="font-mono text-xs">{entry}</span>
                </PageButton>
              ),
            )}

            <PageButton
              onClick={() => onPageChange(page + 1)}
              disabled={isBusy || page >= totalPages}
              ariaLabel="Next page"
            >
              <Icon name="arrow-left" size="sm" className="rotate-180" />
            </PageButton>
          </div>
        )}
      </div>
    </nav>
  );
}

interface PageButtonProps {
  onClick: () => void;
  disabled?: boolean;
  isCurrent?: boolean;
  ariaLabel: string;
  children: React.ReactNode;
}

function PageButton({ onClick, disabled, isCurrent, ariaLabel, children }: PageButtonProps) {
  const tone = isCurrent
    ? "border-primary bg-primary text-on-primary shadow-sm"
    : "border-border-strong bg-surface text-foreground-muted hover:border-primary hover:text-foreground";

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-label={ariaLabel}
      aria-current={isCurrent ? "page" : undefined}
      className={`inline-flex h-9 min-w-9 cursor-pointer items-center justify-center rounded-lg border px-2 transition-[background-color,border-color,color] duration-150 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring disabled:pointer-events-none disabled:opacity-45 ${tone}`}
    >
      {children}
    </button>
  );
}

/**
 * The page numbers to render: always the first and last page, plus a window around the
 * current one, with `"gap"` standing in for the runs left out.
 */
export function buildPageWindow(page: number, totalPages: number): (number | "gap")[] {
  const pages = new Set<number>([1, totalPages]);

  for (let offset = -WINDOW_RADIUS; offset <= WINDOW_RADIUS; offset += 1) {
    const candidate = page + offset;
    if (candidate >= 1 && candidate <= totalPages) pages.add(candidate);
  }

  const ordered = [...pages].sort((a, b) => a - b);

  return ordered.flatMap((value, index) => {
    const previous = ordered[index - 1];
    return previous !== undefined && value - previous > 1 ? ["gap" as const, value] : [value];
  });
}
