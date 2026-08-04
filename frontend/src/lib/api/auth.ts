import { apiFetch } from "@/lib/api/client";
import type { User } from "@/types";

interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: User;
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/auth/login", { method: "POST", body: { email, password } });
}

export function fetchCurrentUser(): Promise<User> {
  return apiFetch<User>("/auth/me");
}
