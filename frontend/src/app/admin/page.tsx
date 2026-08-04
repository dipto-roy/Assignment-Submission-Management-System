"use client";

import { useRequireRole } from "@/lib/auth/useRequireRole";
import { useAuth } from "@/lib/auth/AuthContext";
import { UsersPanel } from "@/components/admin/UsersPanel";
import { ClassesPanel } from "@/components/admin/ClassesPanel";
import { SubjectsPanel } from "@/components/admin/SubjectsPanel";

export default function AdminDashboardPage() {
  const { isLoading } = useRequireRole("Admin");
  const { logout } = useAuth();

  if (isLoading) {
    return (
      <main className="p-8">
        <p className="text-sm text-black/60 dark:text-white/60">Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-4xl p-8">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-xl font-semibold">Admin Dashboard</h1>
        <button onClick={logout} className="text-sm underline">
          Sign out
        </button>
      </div>

      <div className="flex flex-col gap-10">
        <UsersPanel />
        <ClassesPanel />
        <SubjectsPanel />
      </div>
    </main>
  );
}
