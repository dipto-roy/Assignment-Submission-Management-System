/** Date helpers shared by the teacher and student dashboards. */

/** Formats an ISO timestamp for display, falling back to the raw value if unparsable. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

/**
 * Converts an ISO timestamp to the `YYYY-MM-DDTHH:mm` shape `<input type="datetime-local">`
 * requires, expressed in the browser's local timezone.
 */
export function toDateTimeLocalValue(iso: string | null | undefined): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

/** Converts a `datetime-local` value (local time) back to an ISO 8601 UTC string. */
export function fromDateTimeLocalValue(value: string): string {
  return new Date(value).toISOString();
}

export function isPastDeadline(iso: string, now: Date = new Date()): boolean {
  const deadline = new Date(iso);
  if (Number.isNaN(deadline.getTime())) return false;
  return deadline.getTime() <= now.getTime();
}

/** Human-readable time remaining, e.g. "2d 4h left" or "Deadline passed". */
export function describeTimeRemaining(iso: string, now: Date = new Date()): string {
  const deadline = new Date(iso);
  if (Number.isNaN(deadline.getTime())) return "—";

  const diffMs = deadline.getTime() - now.getTime();
  if (diffMs <= 0) return "Deadline passed";

  const minutes = Math.floor(diffMs / 60_000);
  const days = Math.floor(minutes / (60 * 24));
  const hours = Math.floor((minutes % (60 * 24)) / 60);

  if (days > 0) return `${days}d ${hours}h left`;
  if (hours > 0) return `${hours}h ${minutes % 60}m left`;
  return `${minutes}m left`;
}
