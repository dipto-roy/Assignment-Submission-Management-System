import Link from "next/link";
import { Icon } from "@/components/ui/Icon";
import { primaryButtonClass } from "@/components/ui/styles";

const HIGHLIGHTS = [
  {
    icon: "book-open",
    title: "Teachers",
    body: "Set assignments, attach briefs, publish when ready, and grade in one place.",
  },
  {
    icon: "graduation-cap",
    title: "Students",
    body: "See what is due, submit before the deadline, and read your marks and feedback.",
  },
  {
    icon: "shield",
    title: "Admins",
    body: "Run classes, subjects, and enrollment, with read-only oversight of every submission.",
  },
] as const;

// TODO(phase 2): redirect authenticated users straight to their role dashboard.
export default function HomePage() {
  return (
    <main
      id="main-content"
      className="relative flex flex-1 flex-col items-center justify-center overflow-hidden px-4 py-16 sm:px-8"
    >
      {/* Atmosphere only — a soft teal wash behind the fold so the page is not flat white. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 h-96 bg-[radial-gradient(60%_60%_at_50%_0%,var(--app-primary-soft),transparent)] opacity-70"
      />

      <div className="relative flex max-w-2xl flex-col items-center gap-5 text-center">
        <span className="inline-flex items-center gap-2 rounded-full border border-border-subtle bg-surface px-3 py-1.5 text-xs font-medium text-foreground-muted shadow-xs">
          <Icon name="sparkles" size="sm" className="text-primary" />
          For schools and colleges
        </span>

        <h1 className="text-balance text-4xl font-semibold tracking-tight text-foreground sm:text-5xl">
          Assignment &amp; Submission
          <span className="block text-primary">Management System</span>
        </h1>

        <p className="max-w-md text-pretty text-base text-foreground-muted">
          One role-based workflow for setting work, handing it in on time, and returning marks
          and feedback.
        </p>

        <Link href="/login" className={`${primaryButtonClass} mt-2 px-6`}>
          <Icon name="log-in" size="sm" />
          Sign in
        </Link>
      </div>

      <ul className="relative mt-14 grid w-full max-w-4xl gap-4 sm:grid-cols-3">
        {HIGHLIGHTS.map((item) => (
          <li
            key={item.title}
            className="rounded-xl border border-border-subtle bg-surface p-5 text-left shadow-sm transition-shadow duration-200 hover:shadow-md"
          >
            <span className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary-soft text-primary-soft-foreground">
              <Icon name={item.icon} size="lg" />
            </span>
            <h2 className="text-sm font-semibold text-foreground">{item.title}</h2>
            <p className="mt-1 text-sm text-foreground-muted">{item.body}</p>
          </li>
        ))}
      </ul>
    </main>
  );
}
