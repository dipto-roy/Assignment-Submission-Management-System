"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";

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
    <main className="mx-auto flex w-full max-w-md flex-1 flex-col items-center justify-center gap-4 px-4 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-danger-soft text-danger">
        <Icon name="alert-triangle" size="xl" />
      </span>

      <h1 className="text-xl font-semibold tracking-tight text-foreground">
        Something went wrong
      </h1>
      <p className="text-sm text-foreground-muted">
        {error.message || "An unexpected error occurred while loading this page."}
      </p>

      <Button icon="refresh" onClick={reset}>
        Try again
      </Button>
    </main>
  );
}
