"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { StudentDashboard } from "@/components/student/StudentDashboard";
import { mutedTextClass } from "@/components/ui/styles";

export default function StudentDashboardPage() {
  const { isLoading } = useRequireRole("Student");

  if (isLoading) {
    return (
      <main className="p-8">
        <p className={mutedTextClass}>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-4xl p-4 sm:p-8">
      <h1 className="mb-6 text-xl font-semibold">Student Dashboard</h1>

      <StudentDashboard />
    </main>
  );
}
