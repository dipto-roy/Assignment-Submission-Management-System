import { describe, expect, it } from "vitest";
import {
  describeTimeRemaining,
  formatDateTime,
  fromDateTimeLocalValue,
  isPastDeadline,
  toDateTimeLocalValue,
} from "@/lib/datetime";

const NOW = new Date("2026-08-10T12:00:00Z");

describe("isPastDeadline", () => {
  it("is false while the deadline is in the future", () => {
    expect(isPastDeadline("2026-08-11T12:00:00Z", NOW)).toBe(false);
  });

  it("is true once the deadline has passed", () => {
    expect(isPastDeadline("2026-08-09T12:00:00Z", NOW)).toBe(true);
  });

  it("treats the exact deadline instant as passed", () => {
    expect(isPastDeadline("2026-08-10T12:00:00Z", NOW)).toBe(true);
  });

  it("does not lock on an unparsable date", () => {
    expect(isPastDeadline("not-a-date", NOW)).toBe(false);
  });
});

describe("describeTimeRemaining", () => {
  it("reports days and hours when more than a day remains", () => {
    expect(describeTimeRemaining("2026-08-12T16:00:00Z", NOW)).toBe("2d 4h left");
  });

  it("reports hours and minutes within the last day", () => {
    expect(describeTimeRemaining("2026-08-10T15:30:00Z", NOW)).toBe("3h 30m left");
  });

  it("reports minutes within the last hour", () => {
    expect(describeTimeRemaining("2026-08-10T12:45:00Z", NOW)).toBe("45m left");
  });

  it("reports a passed deadline", () => {
    expect(describeTimeRemaining("2026-08-10T11:59:00Z", NOW)).toBe("Deadline passed");
  });
});

describe("datetime-local conversion", () => {
  it("round-trips a timestamp through the input format", () => {
    const iso = "2026-08-20T09:30:00.000Z";
    expect(fromDateTimeLocalValue(toDateTimeLocalValue(iso))).toBe(iso);
  });

  it("produces a value the datetime-local input accepts", () => {
    expect(toDateTimeLocalValue("2026-08-20T09:30:00Z")).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
  });

  it("returns an empty string for missing or unparsable input", () => {
    expect(toDateTimeLocalValue(null)).toBe("");
    expect(toDateTimeLocalValue("not-a-date")).toBe("");
  });
});

describe("formatDateTime", () => {
  it("renders a dash for a missing value", () => {
    expect(formatDateTime(null)).toBe("—");
  });

  it("returns the raw value when it cannot be parsed", () => {
    expect(formatDateTime("not-a-date")).toBe("not-a-date");
  });
});
