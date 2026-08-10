"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { useAuth } from "@/lib/auth/AuthContext";
import { StudentDashboard } from "@/components/student/StudentDashboard";
import { mutedTextClass, subtleButtonClass } from "@/components/ui/styles";

export default function StudentDashboardPage() {
  const { isLoading } = useRequireRole("Student");
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
          <h1 className="text-xl font-semibold">Student Dashboard</h1>
          {user && <p className={mutedTextClass}>Signed in as {user.name}</p>}
        </div>
        <button onClick={logout} className={subtleButtonClass}>
          Sign out
        </button>
      </div>

      <StudentDashboard />
    </main>
  );
}
