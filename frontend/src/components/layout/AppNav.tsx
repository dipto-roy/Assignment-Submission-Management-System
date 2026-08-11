"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import { NotificationBell } from "@/components/notifications/NotificationBell";
import { subtleButtonClass } from "@/components/ui/styles";
import type { UserRole } from "@/types";

const DASHBOARD_BY_ROLE: Record<UserRole, { href: string; label: string }> = {
  Admin: { href: "/admin", label: "Admin" },
  Teacher: { href: "/teacher", label: "Teacher" },
  Student: { href: "/student", label: "Student" },
};

/**
 * Shared nav. It only renders links for the signed-in user's own role — the real
 * enforcement is `useRequireRole` on each page plus server-side authorization.
 */
export function AppNav() {
  const { user, isLoading, logout } = useAuth();
  const pathname = usePathname();

  if (isLoading || !user || pathname === "/login") {
    return null;
  }

  const dashboard = DASHBOARD_BY_ROLE[user.role];

  return (
    <header className="border-b border-black/10 dark:border-white/15">
      <nav
        aria-label="Main navigation"
        className="mx-auto flex max-w-4xl flex-wrap items-center justify-between gap-2 px-4 py-3 sm:px-8"
      >
        <div className="flex flex-wrap items-center gap-4">
          <Link href={dashboard.href} className="text-sm font-semibold">
            Assignments
          </Link>
          <Link
            href={dashboard.href}
            aria-current={pathname === dashboard.href ? "page" : undefined}
            className={`text-sm ${
              pathname === dashboard.href ? "underline" : "text-black/60 dark:text-white/60"
            }`}
          >
            {dashboard.label} dashboard
          </Link>
        </div>

        <div className="flex items-center gap-3 text-sm">
          <NotificationBell />
          <span className="text-black/60 dark:text-white/60">
            {user.name} · {user.role}
          </span>
          <button type="button" onClick={logout} className={subtleButtonClass}>
            Sign out
          </button>
        </div>
      </nav>
    </header>
  );
}
