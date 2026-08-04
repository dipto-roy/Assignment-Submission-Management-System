// TODO(phase 2): wire to POST /auth/login via lib/api/client, redirect by role.
export default function LoginPage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-8">
      <div className="w-full max-w-sm rounded-lg border border-black/10 p-6 dark:border-white/10">
        <h1 className="mb-4 text-xl font-semibold">Sign in</h1>
        <p className="text-sm text-black/60 dark:text-white/60">
          Login form lands in Phase 2 (Auth).
        </p>
      </div>
    </main>
  );
}
