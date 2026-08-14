"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Alert } from "@/components/ui/primitives";
import { PasswordInput } from "@/components/ui/PasswordInput";
import { fieldLabelClass, inputClass } from "@/components/ui/styles";

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const user = await login(email, password);
      router.push(`/${user.role.toLowerCase()}`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Login failed.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main
      id="main-content"
      className="relative flex flex-1 items-center justify-center overflow-hidden px-4 py-12 sm:px-8"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 h-80 bg-[radial-gradient(55%_60%_at_50%_0%,var(--app-primary-soft),transparent)] opacity-70"
      />

      <form
        onSubmit={handleSubmit}
        className="app-animate-in relative w-full max-w-sm rounded-2xl border border-border-subtle bg-surface p-7 shadow-lg"
      >
        <span className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-on-primary shadow-sm">
          <Icon name="graduation-cap" size="xl" />
        </span>

        <h1 className="text-xl font-semibold tracking-tight text-foreground">Sign in</h1>
        <p className="mb-6 mt-1 text-sm text-foreground-muted">
          Use the account your school issued you.
        </p>

        <div className="flex flex-col gap-4">
          <label className={fieldLabelClass}>
            Email
            <span className="relative block">
              <Icon
                name="mail"
                size="sm"
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-foreground-subtle"
              />
              <input
                type="email"
                required
                autoComplete="email"
                placeholder="you@school.edu"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={`${inputClass} pl-9`}
              />
            </span>
          </label>

          <label className={fieldLabelClass}>
            Password
            <PasswordInput
              leadingIcon="shield"
              required
              autoComplete="current-password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>

          {error && <Alert>{error}</Alert>}

          <Button type="submit" icon="log-in" isBusy={isSubmitting} className="w-full">
            {isSubmitting ? "Signing in…" : "Sign in"}
          </Button>
        </div>
      </form>
    </main>
  );
}
