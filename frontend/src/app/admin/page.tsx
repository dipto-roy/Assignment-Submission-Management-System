"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { UsersPanel } from "@/components/admin/UsersPanel";
import { ClassesPanel } from "@/components/admin/ClassesPanel";
import { SubjectsPanel } from "@/components/admin/SubjectsPanel";
import { EnrollmentPanel } from "@/components/admin/EnrollmentPanel";
import { AssignmentsOversightPanel } from "@/components/admin/AssignmentsOversightPanel";
import { mutedTextClass } from "@/components/ui/styles";

export default function AdminDashboardPage() {
  const { isLoading } = useRequireRole("Admin");

  if (isLoading) {
    return (
      <main className="p-8">
        <p className={mutedTextClass}>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-4xl p-4 sm:p-8">
      <h1 className="mb-6 text-xl font-semibold">Admin Dashboard</h1>

      <div className="flex flex-col gap-10">
        <UsersPanel />
        <ClassesPanel />
        <SubjectsPanel />
        <EnrollmentPanel />
        <AssignmentsOversightPanel />
      </div>
    </main>
  );
}
