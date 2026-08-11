"use client";

import { useEffect, useState } from "react";
import { getAssignmentsPage } from "@/lib/api/assignments";
import { getAssignmentSubmissions } from "@/lib/api/submissions";
import { useClasses } from "@/lib/hooks/useClasses";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { Pagination } from "@/components/ui/Pagination";
import { formatDateTime } from "@/lib/datetime";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import {
  Alert,
  Badge,
  EmptyState,
  LoadingLine,
  SectionHeading,
} from "@/components/ui/primitives";
import { cardClass, compactInputClass, mutedTextClass, subtleTextClass } from "@/components/ui/styles";
import type { Assignment, AssignmentStatus, SubmissionDetail } from "@/types";

const STATUS_FILTERS: readonly (AssignmentStatus | "")[] = ["", "Draft", "Published"];

/**
 * Admin oversight of every assignment and its submissions (roadmap §2, Admin bullet 4).
 *
 * Read-only by design: an Admin sees Draft assignments a student never would, and marks a
 * teacher has yet to return. Editing either from here would bypass the teacher-ownership
 * rules the API enforces on writes, so this panel only reads.
 */
export function AssignmentsOversightPanel() {
  const { classes } = useClasses();
  const [classId, setClassId] = useState("");
  const [status, setStatus] = useState<AssignmentStatus | "">("");
  const [search, setSearch] = useState("");
  const [submissionsError, setSubmissionsError] = useState<string | null>(null);

  const {
    items: assignments,
    meta,
    isLoading,
    isRefreshing,
    error: loadError,
    setPage,
    setPageSize,
  } = usePagedList<Assignment>(
    (params) =>
      getAssignmentsPage({
        ...params,
        classId: classId || undefined,
        status: status || undefined,
        search: search.trim() || undefined,
      }),
    {
      filters: [classId, status, search.trim()],
      errorMessage: "Failed to load assignments.",
    },
  );

  const error = submissionsError ?? loadError;

  // Expanded assignment, with its submissions kept alongside the id they belong to so a
  // slow response can never render under a different assignment.
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [submissions, setSubmissions] = useState<{
    assignmentId: string;
    items: SubmissionDetail[];
  } | null>(null);

  useEffect(() => {
    if (!expandedId) return;

    let isActive = true;

    getAssignmentSubmissions(expandedId)
      .then((items) => {
        if (isActive) setSubmissions({ assignmentId: expandedId, items });
      })
      .catch((e: unknown) => {
        if (isActive) {
          setSubmissionsError(e instanceof Error ? e.message : "Failed to load submissions.");
        }
      });

    return () => {
      isActive = false;
    };
  }, [expandedId]);

  const toggle = (id: string) => {
    setSubmissionsError(null);
    setExpandedId((current) => (current === id ? null : id));
  };

  return (
    <section>
      <SectionHeading
        icon="layers"
        title="Assignments & submissions"
        description="Every assignment across all classes, including drafts. Read-only — teachers own the edits and the grading."
      />

      <div className="mb-5 flex flex-wrap items-end gap-2">
        <select
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          aria-label="Filter by class"
          className={compactInputClass}
        >
          <option value="">All classes</option>
          {classes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
              {c.section ? ` — ${c.section}` : ""}
            </option>
          ))}
        </select>

        <select
          value={status}
          onChange={(e) => setStatus(e.target.value as AssignmentStatus | "")}
          aria-label="Filter by status"
          className={compactInputClass}
        >
          {STATUS_FILTERS.map((s) => (
            <option key={s} value={s}>
              {s || "All statuses"}
            </option>
          ))}
        </select>

        <span className="relative">
          <Icon
            name="search"
            size="sm"
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-foreground-subtle"
          />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search title…"
            aria-label="Search assignments"
            className={`${compactInputClass} pl-9`}
          />
        </span>
      </div>

      {error && <Alert className="mb-3">{error}</Alert>}

      {isLoading ? (
        <LoadingLine label="Loading assignments…" />
      ) : assignments.length === 0 ? (
        <EmptyState
          icon="search"
          title="No assignments match these filters."
          description="Widen the class or status filter, or clear the search."
        />
      ) : (
        <>
        <ul className="flex flex-col gap-3">
          {assignments.map((assignment) => (
            <li key={assignment.id} className={cardClass}>
              <div className="flex flex-wrap items-start justify-between gap-2">
                <span className="font-semibold text-foreground">{assignment.title}</span>
                <Badge
                  tone={assignment.status === "Published" ? "success" : "neutral"}
                  icon={assignment.status === "Published" ? "check-circle" : "edit"}
                >
                  {assignment.status}
                </Badge>
              </div>

              <p className={`mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 ${mutedTextClass}`}>
                <span className="inline-flex items-center gap-1.5">
                  <Icon name="users" size="sm" />
                  {assignment.className}
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <Icon name="book-open" size="sm" />
                  {assignment.subjectName}
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <Icon name="user" size="sm" />
                  {assignment.teacherName}
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <Icon name="calendar-clock" size="sm" />
                  {formatDateTime(assignment.deadline)}
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <Icon name="check" size="sm" />
                  <span className="font-mono">{assignment.maxMarks}</span> marks
                </span>
              </p>

              <Button
                variant="subtle"
                icon={expandedId === assignment.id ? "eye-off" : "eye"}
                onClick={() => toggle(assignment.id)}
                aria-expanded={expandedId === assignment.id}
                className="mt-2"
              >
                {expandedId === assignment.id ? "Hide submissions" : "View submissions"}
              </Button>

              {expandedId === assignment.id && (
                <SubmissionList assignmentId={assignment.id} submissions={submissions} />
              )}
            </li>
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
  );
}

interface SubmissionListProps {
  assignmentId: string;
  submissions: { assignmentId: string; items: SubmissionDetail[] } | null;
}

function SubmissionList({ assignmentId, submissions }: SubmissionListProps) {
  if (submissions?.assignmentId !== assignmentId) {
    return (
      <div className="mt-3">
        <LoadingLine label="Loading submissions…" />
      </div>
    );
  }

  if (submissions.items.length === 0) {
    return <p className={`mt-3 ${mutedTextClass}`}>No submissions yet.</p>;
  }

  return (
    <ul className="app-animate-in mt-3 divide-y divide-border-subtle rounded-lg border border-border-subtle bg-muted/40">
      {submissions.items.map((submission) => (
        <li key={submission.id} className="px-3 py-2.5 text-sm">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <span className="flex items-center gap-2">
              <Icon name="user" size="sm" className="text-foreground-subtle" />
              <span className="font-medium text-foreground">{submission.studentName}</span>
              <span className={subtleTextClass}>{submission.studentEmail}</span>
            </span>

            {/* One string on purpose: status and score read together at a glance. */}
            <span className={`font-mono ${mutedTextClass}`}>
              {submission.status}
              {submission.marks !== null && submission.marks !== undefined
                ? ` · ${submission.marks}/${submission.assignmentMaxMarks}`
                : ""}
            </span>
          </div>

          <p className={`mt-0.5 flex items-center gap-1.5 ${subtleTextClass}`}>
            <Icon name="clock" size="sm" />
            Submitted {formatDateTime(submission.submittedAt)}
          </p>

          {submission.feedback && (
            <p className="mt-1 text-foreground-muted">{submission.feedback}</p>
          )}
        </li>
      ))}
    </ul>
  );
}
