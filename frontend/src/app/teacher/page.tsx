"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { useAuth } from "@/lib/auth/AuthContext";
import { AssignmentsPanel } from "@/components/teacher/AssignmentsPanel";
import { mutedTextClass, subtleButtonClass } from "@/components/ui/styles";

export default function TeacherDashboardPage() {
  const { isLoading } = useRequireRole("Teacher");
  const { user, logout } = useAuth();

  if (isLoading) {
    return (
      <main className="p-8">
        <p className={mutedTextClass}>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-4xl p-4 sm:p-8">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-xl font-semibold">Teacher Dashboard</h1>
          {user && <p className={mutedTextClass}>Signed in as {user.name}</p>}
        </div>
        <button onClick={logout} className={subtleButtonClass}>
          Sign out
        </button>
      </div>

      <AssignmentsPanel />
    </main>
  );
}
