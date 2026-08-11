"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { AssignmentsPanel } from "@/components/teacher/AssignmentsPanel";
import { LoadingLine, PageHeader } from "@/components/ui/primitives";

export default function TeacherDashboardPage() {
  const { isLoading } = useRequireRole("Teacher");

  if (isLoading) {
    return (
      <main id="main-content" className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-8">
        <LoadingLine label="Checking your access…" />
      </main>
    );
  }

  return (
    <main id="main-content" className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-8">
      <PageHeader
        icon="book-open"
        title="Teacher dashboard"
        description="Set work for your subjects, publish it, and grade what comes back."
      />

      <AssignmentsPanel />
    </main>
  );
}
