"use client";

import { useEffect } from "react";

/** Route-level error boundary (plan §10.3): keeps a failed fetch from blanking the app. */
export default function ErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Surfaced in the browser console for debugging; server-side detail is logged by the API.
    console.error(error);
  }, [error]);

  return (
    <main className="mx-auto flex max-w-md flex-col items-start gap-4 p-8">
      <h1 className="text-xl font-semibold">Something went wrong</h1>
      <p className="text-sm text-black/60 dark:text-white/60">
        {error.message || "An unexpected error occurred while loading this page."}
      </p>
      <button
        type="button"
        onClick={reset}
        className="rounded bg-black px-3 py-1 text-sm text-white dark:bg-white dark:text-black"
      >
        Try again
      </button>
    </main>
  );
}
