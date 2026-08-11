"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import { NotificationBell } from "@/components/notifications/NotificationBell";
import { Button } from "@/components/ui/Button";
import { Icon, type IconName } from "@/components/ui/Icon";
import type { UserRole } from "@/types";

interface DashboardLink {
  href: string;
  label: string;
  icon: IconName;
}

const DASHBOARD_BY_ROLE: Record<UserRole, DashboardLink> = {
  Admin: { href: "/admin", label: "Admin", icon: "shield" },
  Teacher: { href: "/teacher", label: "Teacher", icon: "book-open" },
  Student: { href: "/student", label: "Student", icon: "graduation-cap" },
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
  const isCurrent = pathname === dashboard.href;

  return (
    <header className="sticky top-0 z-30 border-b border-border-subtle bg-surface/85 backdrop-blur-md">
      <nav
        aria-label="Main navigation"
        className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-2.5 sm:px-8"
      >
        <div className="flex flex-wrap items-center gap-1 sm:gap-2">
          <Link
            href={dashboard.href}
            className="flex items-center gap-2 rounded-lg px-2 py-2 font-semibold tracking-tight text-foreground transition-colors duration-150 hover:bg-muted"
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-on-primary shadow-xs">
              <Icon name="graduation-cap" size="md" />
            </span>
            <span className="hidden sm:inline">Assignments</span>
          </Link>

          <Link
            href={dashboard.href}
            aria-current={isCurrent ? "page" : undefined}
            className={`flex min-h-11 items-center gap-2 rounded-lg px-3 text-sm font-medium transition-colors duration-150 ${
              isCurrent
                ? "bg-primary-soft text-primary-soft-foreground"
                : "text-foreground-muted hover:bg-muted hover:text-foreground"
            }`}
          >
            <Icon name={dashboard.icon} size="sm" />
            {dashboard.label} dashboard
          </Link>
        </div>

        <div className="flex items-center gap-1 sm:gap-2">
          <NotificationBell />

          {/* The role chip is the fastest way to answer "which account am I in?". */}
          <span className="hidden items-center gap-2 rounded-full border border-border-subtle bg-muted/60 py-1 pl-1 pr-3 text-sm md:flex">
            <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary-soft text-primary-soft-foreground">
              <Icon name="user" size="sm" />
            </span>
            <span className="font-medium text-foreground">{user.name}</span>
            <span className="text-xs text-foreground-subtle">{user.role}</span>
          </span>

          {/* The label is display:none on small screens, so it is also gone from the
              accessibility tree — `aria-label` keeps the control named either way. */}
          <Button variant="subtle" icon="log-out" onClick={logout} aria-label="Sign out">
            <span className="hidden sm:inline">Sign out</span>
          </Button>
        </div>
      </nav>
    </header>
  );
}
