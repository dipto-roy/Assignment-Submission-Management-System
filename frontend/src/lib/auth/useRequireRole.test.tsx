import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useRequireRole } from "@/lib/auth/useRequireRole";
import { useAuth } from "@/lib/auth/AuthContext";
import type { User, UserRole } from "@/types";

const replace = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace }),
}));

vi.mock("@/lib/auth/AuthContext", () => ({
  useAuth: vi.fn(),
}));

const userWithRole = (role: UserRole): User => ({
  id: "u-1",
  name: `${role} user`,
  email: `${role.toLowerCase()}@lms.test`,
  role,
});

function Guarded({ role }: { role: UserRole }) {
  const { isLoading } = useRequireRole(role);
  return <span data-testid="state">{isLoading ? "blocked" : "allowed"}</span>;
}

const mockAuth = (user: User | null, isLoading = false) =>
  vi.mocked(useAuth).mockReturnValue({
    user,
    isLoading,
    login: vi.fn(),
    logout: vi.fn(),
  });

describe("useRequireRole", () => {
  beforeEach(() => {
    replace.mockReset();
    vi.mocked(useAuth).mockReset();
  });

  it("waits without redirecting while auth is still hydrating", () => {
    mockAuth(null, true);

    render(<Guarded role="Teacher" />);

    expect(screen.getByTestId("state")).toHaveTextContent("blocked");
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects an unauthenticated visitor to /login", async () => {
    mockAuth(null);

    render(<Guarded role="Teacher" />);

    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.getByTestId("state")).toHaveTextContent("blocked");
  });

  it("redirects a student away from the teacher dashboard", async () => {
    mockAuth(userWithRole("Student"));

    render(<Guarded role="Teacher" />);

    await waitFor(() => expect(replace).toHaveBeenCalledWith("/student"));
  });

  it("redirects a teacher away from the admin dashboard", async () => {
    mockAuth(userWithRole("Teacher"));

    render(<Guarded role="Admin" />);

    await waitFor(() => expect(replace).toHaveBeenCalledWith("/teacher"));
  });

  it("allows a matching role through without redirecting", async () => {
    mockAuth(userWithRole("Teacher"));

    render(<Guarded role="Teacher" />);

    expect(screen.getByTestId("state")).toHaveTextContent("allowed");
    await waitFor(() => expect(replace).not.toHaveBeenCalled());
  });
});
