import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiFetch, clearToken, getToken, setToken } from "@/lib/api/client";

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });

describe("token storage", () => {
  afterEach(() => clearToken());

  it("returns null when no token has been stored", () => {
    expect(getToken()).toBeNull();
  });

  it("round-trips a stored token", () => {
    setToken("jwt-123");
    expect(getToken()).toBe("jwt-123");
  });

  it("clears a stored token", () => {
    setToken("jwt-123");
    clearToken();
    expect(getToken()).toBeNull();
  });
});

describe("apiFetch", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    clearToken();
  });

  it("unwraps the data payload from a success envelope", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ success: true, data: { id: "1" }, error: null }));

    await expect(apiFetch<{ id: string }>("/things")).resolves.toEqual({ id: "1" });
  });

  it("returns the raw body when the response is not enveloped", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ id: "1" }));

    await expect(apiFetch<{ id: string }>("/things")).resolves.toEqual({ id: "1" });
  });

  it("throws the envelope error when success is false", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ success: false, data: null, error: "Deadline has passed." }),
    );

    await expect(apiFetch("/things")).rejects.toThrow("Deadline has passed.");
  });

  it("throws the server error message on a non-2xx response", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ success: false, data: null, error: "You do not own this submission." }, 403),
    );

    await expect(apiFetch("/things")).rejects.toThrow("You do not own this submission.");
  });

  it("falls back to a status message when the error body is not JSON", async () => {
    fetchMock.mockResolvedValue(new Response("<html>502</html>", { status: 502 }));

    await expect(apiFetch("/things")).rejects.toThrow("Request failed with status 502.");
  });

  it("resolves to undefined for a 204 response", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

    await expect(apiFetch<void>("/things", { method: "DELETE" })).resolves.toBeUndefined();
  });

  it("attaches the bearer token when one is stored", async () => {
    setToken("jwt-123");
    fetchMock.mockResolvedValue(jsonResponse({ success: true, data: [], error: null }));

    await apiFetch("/things");

    const [, init] = fetchMock.mock.calls[0];
    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer jwt-123");
  });

  it("omits the Authorization header when no token is stored", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ success: true, data: [], error: null }));

    await apiFetch("/things");

    const [, init] = fetchMock.mock.calls[0];
    expect(init.headers).not.toHaveProperty("Authorization");
  });

  it("serializes the request body as JSON", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ success: true, data: null, error: null }));

    await apiFetch("/things", { method: "POST", body: { name: "Physics" } });

    const [, init] = fetchMock.mock.calls[0];
    expect(init.body).toBe(JSON.stringify({ name: "Physics" }));
    expect((init.headers as Record<string, string>)["Content-Type"]).toBe("application/json");
  });
});
