"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import type { UserRole } from "@/types";

/**
 * Redirects to /login if unauthenticated, or to the caller's own dashboard if
 * authenticated with a different role. Server-side authorization is still the
 * real enforcement layer (see backend [Authorize(Roles = ...)]); this is UX only.
 */
export function useRequireRole(role: UserRole): { isLoading: boolean } {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;

    if (!user) {
      router.replace("/login");
      return;
    }

    if (user.role !== role) {
      router.replace(`/${user.role.toLowerCase()}`);
    }
  }, [user, isLoading, role, router]);

  return { isLoading: isLoading || !user || user.role !== role };
}
