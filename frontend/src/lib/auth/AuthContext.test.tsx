import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "@/lib/auth/AuthContext";
import { getToken, setToken } from "@/lib/api/client";
import { fetchCurrentUser, login as loginRequest } from "@/lib/api/auth";
import type { User } from "@/types";

vi.mock("@/lib/api/auth", () => ({
  fetchCurrentUser: vi.fn(),
  login: vi.fn(),
}));

const STUDENT: User = {
  id: "u-1",
  name: "Sam Student",
  email: "student@lms.test",
  role: "Student",
};

function Probe() {
  const { user, isLoading, login, logout } = useAuth();

  return (
    <div>
      <span data-testid="state">
        {isLoading ? "loading" : user ? `signed-in:${user.name}` : "signed-out"}
      </span>
      <button onClick={() => login("student@lms.test", "pw")}>sign in</button>
      <button onClick={logout}>sign out</button>
    </div>
  );
}

const renderProbe = () =>
  render(
    <AuthProvider>
      <Probe />
    </AuthProvider>,
  );

describe("AuthProvider", () => {
  beforeEach(() => {
    vi.mocked(fetchCurrentUser).mockReset();
    vi.mocked(loginRequest).mockReset();
  });

  it("settles as signed out when no token is stored", async () => {
    renderProbe();

    await waitFor(() => expect(screen.getByTestId("state")).toHaveTextContent("signed-out"));
    expect(fetchCurrentUser).not.toHaveBeenCalled();
  });

  it("hydrates the user from a stored token", async () => {
    setToken("jwt-123");
    vi.mocked(fetchCurrentUser).mockResolvedValue(STUDENT);

    renderProbe();

    await waitFor(() =>
      expect(screen.getByTestId("state")).toHaveTextContent("signed-in:Sam Student"),
    );
  });

  it("discards a token the server rejects", async () => {
    setToken("expired-jwt");
    vi.mocked(fetchCurrentUser).mockRejectedValue(new Error("401"));

    renderProbe();

    await waitFor(() => expect(screen.getByTestId("state")).toHaveTextContent("signed-out"));
    expect(getToken()).toBeNull();
  });

  it("stores the token and user after a successful login", async () => {
    vi.mocked(loginRequest).mockResolvedValue({
      token: "fresh-jwt",
      expiresAtUtc: "2026-08-11T00:00:00Z",
      user: STUDENT,
    });

    renderProbe();
    await waitFor(() => expect(screen.getByTestId("state")).toHaveTextContent("signed-out"));

    await userEvent.click(screen.getByRole("button", { name: "sign in" }));

    await waitFor(() =>
      expect(screen.getByTestId("state")).toHaveTextContent("signed-in:Sam Student"),
    );
    expect(getToken()).toBe("fresh-jwt");
  });

  it("clears the token and user on logout", async () => {
    setToken("jwt-123");
    vi.mocked(fetchCurrentUser).mockResolvedValue(STUDENT);

    renderProbe();
    await waitFor(() =>
      expect(screen.getByTestId("state")).toHaveTextContent("signed-in:Sam Student"),
    );

    await userEvent.click(screen.getByRole("button", { name: "sign out" }));

    expect(screen.getByTestId("state")).toHaveTextContent("signed-out");
    expect(getToken()).toBeNull();
  });

  it("throws when useAuth is used outside the provider", () => {
    // React logs the thrown error; silence it so the run stays readable.
    vi.spyOn(console, "error").mockImplementation(() => {});

    expect(() => render(<Probe />)).toThrow("useAuth must be used within an AuthProvider.");
  });
});
