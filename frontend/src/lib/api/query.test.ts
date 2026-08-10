import { describe, expect, it } from "vitest";
import { FULL_PAGE, MAX_PAGE_SIZE, toQueryString } from "@/lib/api/query";

describe("toQueryString", () => {
  it("returns an empty string when every value is omitted", () => {
    expect(toQueryString({ page: undefined, search: "" })).toBe("");
  });

  it("builds a query string from the values that are present", () => {
    expect(toQueryString({ page: 2, pageSize: 50 })).toBe("?page=2&pageSize=50");
  });

  it("drops null and empty values so filters can be passed through unconditionally", () => {
    expect(toQueryString({ status: undefined, search: null, pageSize: 100 })).toBe("?pageSize=100");
  });

  it("encodes values that are not URL-safe", () => {
    expect(toQueryString({ search: "algebra & geometry" })).toBe("?search=algebra+%26+geometry");
  });

  it("asks for the largest page the API allows by default", () => {
    expect(FULL_PAGE.pageSize).toBe(MAX_PAGE_SIZE);
  });
});
