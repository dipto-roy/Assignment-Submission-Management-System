"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { AssignmentsPanel } from "@/components/teacher/AssignmentsPanel";
import { mutedTextClass } from "@/components/ui/styles";

export default function TeacherDashboardPage() {
  const { isLoading } = useRequireRole("Teacher");

  if (isLoading) {
    return (
      <main className="p-8">
        <p className={mutedTextClass}>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-4xl p-4 sm:p-8">
      <h1 className="mb-6 text-xl font-semibold">Teacher Dashboard</h1>

      <AssignmentsPanel />
    </main>
  );
}
