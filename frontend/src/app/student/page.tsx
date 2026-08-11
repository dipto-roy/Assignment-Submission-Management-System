"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { StudentDashboard } from "@/components/student/StudentDashboard";
import { LoadingLine, PageHeader } from "@/components/ui/primitives";

export default function StudentDashboardPage() {
  const { isLoading } = useRequireRole("Student");

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
        icon="graduation-cap"
        title="Student dashboard"
        description="Everything published for your class, and what you have handed in."
      />

      <StudentDashboard />
    </main>
  );
}
