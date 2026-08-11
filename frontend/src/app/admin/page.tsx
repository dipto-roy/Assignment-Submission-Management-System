"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { UsersPanel } from "@/components/admin/UsersPanel";
import { ClassesPanel } from "@/components/admin/ClassesPanel";
import { SubjectsPanel } from "@/components/admin/SubjectsPanel";
import { EnrollmentPanel } from "@/components/admin/EnrollmentPanel";
import { AssignmentsOversightPanel } from "@/components/admin/AssignmentsOversightPanel";
import { LoadingLine, PageHeader } from "@/components/ui/primitives";
import { panelClass } from "@/components/ui/styles";

export default function AdminDashboardPage() {
  const { isLoading } = useRequireRole("Admin");

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
        icon="shield"
        title="Admin dashboard"
        description="People, classes, subjects, enrollment, and oversight of every assignment."
      />

      {/* Each panel gets its own surface so five stacked admin tools stay visually separate. */}
      <div className="flex flex-col gap-6">
        <div className={panelClass}>
          <UsersPanel />
        </div>
        <div className={panelClass}>
          <ClassesPanel />
        </div>
        <div className={panelClass}>
          <SubjectsPanel />
        </div>
        <div className={panelClass}>
          <EnrollmentPanel />
        </div>
        <div className={panelClass}>
          <AssignmentsOversightPanel />
        </div>
      </div>
    </main>
  );
}
