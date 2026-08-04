import Link from "next/link";

// TODO(phase 2): redirect authenticated users straight to their role dashboard.
export default function HomePage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-8 text-center">
      <h1 className="text-2xl font-semibold">Assignment & Submission Management System</h1>
      <p className="max-w-md text-sm text-black/60 dark:text-white/60">
        Role-based assignment and submission workflow for schools and colleges.
      </p>
      <Link
        href="/login"
        className="rounded-md bg-foreground px-4 py-2 text-sm font-medium text-background"
      >
        Sign in
      </Link>
    </main>
  );
}
