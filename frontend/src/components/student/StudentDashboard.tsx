"use client";

import { useEffect, useState } from "react";
import { getAssignmentsPage } from "@/lib/api/assignments";
import { getMySubmissions } from "@/lib/api/submissions";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { Pagination } from "@/components/ui/Pagination";
import { AssignmentCard } from "@/components/student/AssignmentCard";
import { MySubmissionsPanel } from "@/components/student/MySubmissionsPanel";
import { Icon, type IconName } from "@/components/ui/Icon";
import { Alert, EmptyState, LoadingLine, SectionHeading, Skeleton } from "@/components/ui/primitives";
import type { Assignment, Submission } from "@/types";

/**
 * Owns both student-facing lists so a new submission updates the assignment card
 * and the marks/feedback view together. `GET /assignments` is already filtered
 * server-side to Published assignments for the student's class (business rule §7.3).
 */
export function StudentDashboard() {
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [isLoadingSubmissions, setIsLoadingSubmissions] = useState(true);
  const [submissionsError, setSubmissionsError] = useState<string | null>(null);

  const {
    items: assignments,
    meta,
    isLoading: isLoadingAssignments,
    isRefreshing,
    error: assignmentsError,
    setPage,
    setPageSize,
  } = usePagedList<Assignment>((params) => getAssignmentsPage(params), {
    errorMessage: "Failed to load your assignments.",
  });

  /**
   * The submission list is fetched whole rather than by page: it is what tells each
   * assignment card whether this student has already answered it, and a card on page 1
   * can easily correspond to a submission that a paged fetch would leave on page 3.
   */
  useEffect(() => {
    getMySubmissions()
      .then(setSubmissions)
      .catch((e: unknown) =>
        setSubmissionsError(e instanceof Error ? e.message : "Failed to load your submissions."),
      )
      .finally(() => setIsLoadingSubmissions(false));
  }, []);

  const isLoading = isLoadingAssignments || isLoadingSubmissions;
  const error = assignmentsError ?? submissionsError;

  const handleSaved = (saved: Submission) => {
    setSubmissions((prev) => {
      const isKnown = prev.some((s) => s.id === saved.id);
      return isKnown ? prev.map((s) => (s.id === saved.id ? saved : s)) : [...prev, saved];
    });
  };

  const findSubmission = (assignmentId: string) =>
    submissions.find((s) => s.assignmentId === assignmentId);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <LoadingLine label="Loading your assignments…" />
        <Skeleton className="h-32 w-full" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  // Both tiles below count over data held in full — the assignment total comes from the
  // page meta, and submissions are fetched whole — so paging never changes a headline
  // number. Counting the open ones would need every assignment, which is exactly what
  // pagination stops loading, so "handed in" stands in for it.
  const submittedCount = submissions.length;
  const gradedCount = submissions.filter(
    (s) => s.status === "Graded" || s.status === "Returned",
  ).length;

  return (
    <div className="flex flex-col gap-10">
      {error && <Alert>{error}</Alert>}

      {/* At-a-glance counts: the first question a student opens this page to answer. */}
      <dl className="grid gap-3 sm:grid-cols-3">
        <SummaryTile icon="inbox" label="Assignments" value={meta.total} />
        <SummaryTile icon="send" label="Handed in" value={submittedCount} tone="accent" />
        <SummaryTile icon="check-circle" label="Graded" value={gradedCount} tone="success" />
      </dl>

      <section>
        <SectionHeading
          icon="file-text"
          title="Assignments"
          description="Published work for your class, newest deadlines included."
        />

        {assignments.length === 0 ? (
          <EmptyState
            icon="inbox"
            title="Nothing published yet"
            description="When a teacher publishes an assignment for your class, it appears here."
          />
        ) : (
          <>
            <ul className="flex flex-col gap-4">
              {assignments.map((assignment) => (
                <AssignmentCard
                  key={assignment.id}
                  assignment={assignment}
                  submission={findSubmission(assignment.id)}
                  onSaved={handleSaved}
                />
              ))}
            </ul>

            <Pagination
              meta={meta}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
              label="assignments"
              isBusy={isRefreshing}
            />
          </>
        )}
      </section>

      <MySubmissionsPanel submissions={submissions} />
    </div>
  );
}

interface SummaryTileProps {
  icon: IconName;
  label: string;
  value: number;
  tone?: "primary" | "accent" | "success";
}

const TILE_TONE = {
  primary: "bg-primary-soft text-primary-soft-foreground",
  accent: "bg-accent-soft text-accent-soft-foreground",
  success: "bg-success-soft text-success",
} as const;

function SummaryTile({ icon, label, value, tone = "primary" }: SummaryTileProps) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-border-subtle bg-surface p-4 shadow-sm">
      <span className={`flex h-10 w-10 items-center justify-center rounded-lg ${TILE_TONE[tone]}`}>
        <Icon name={icon} size="lg" />
      </span>
      <div>
        <dd className="font-mono text-2xl font-medium leading-none text-foreground">{value}</dd>
        <dt className="mt-1 text-xs text-foreground-muted">{label}</dt>
      </div>
    </div>
  );
}
