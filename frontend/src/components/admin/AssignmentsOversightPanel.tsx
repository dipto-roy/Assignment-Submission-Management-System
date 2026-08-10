"use client";

import { useEffect, useState } from "react";
import { getAssignments } from "@/lib/api/assignments";
import { getAssignmentSubmissions } from "@/lib/api/submissions";
import { useClasses } from "@/lib/hooks/useClasses";
import { formatDateTime } from "@/lib/datetime";
import {
  cardClass,
  inputClass,
  mutedTextClass,
  subtleButtonClass,
} from "@/components/ui/styles";
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
  const [assignments, setAssignments] = useState<Assignment[] | null>(null);
  const [classId, setClassId] = useState("");
  const [status, setStatus] = useState<AssignmentStatus | "">("");
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string | null>(null);

  // Expanded assignment, with its submissions kept alongside the id they belong to so a
  // slow response can never render under a different assignment.
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [submissions, setSubmissions] = useState<{
    assignmentId: string;
    items: SubmissionDetail[];
  } | null>(null);

  useEffect(() => {
    let isActive = true;

    getAssignments({
      classId: classId || undefined,
      status: status || undefined,
      search: search.trim() || undefined,
    })
      .then((result) => {
        if (isActive) setAssignments(result);
      })
      .catch((e: unknown) => {
        if (isActive) setError(e instanceof Error ? e.message : "Failed to load assignments.");
      });

    return () => {
      isActive = false;
    };
  }, [classId, status, search]);

  useEffect(() => {
    if (!expandedId) return;

    let isActive = true;

    getAssignmentSubmissions(expandedId)
      .then((items) => {
        if (isActive) setSubmissions({ assignmentId: expandedId, items });
      })
      .catch((e: unknown) => {
        if (isActive) setError(e instanceof Error ? e.message : "Failed to load submissions.");
      });

    return () => {
      isActive = false;
    };
  }, [expandedId]);

  const toggle = (id: string) => {
    setError(null);
    setExpandedId((current) => (current === id ? null : id));
  };

  return (
    <section>
      <h2 className="mb-1 text-lg font-semibold">Assignments &amp; submissions</h2>
      <p className={`mb-3 ${mutedTextClass}`}>
        Every assignment across all classes, including drafts. Read-only — teachers own the
        edits and the grading.
      </p>

      <div className="mb-4 flex flex-wrap gap-2">
        <select
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          aria-label="Filter by class"
          className={inputClass}
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
          className={inputClass}
        >
          {STATUS_FILTERS.map((s) => (
            <option key={s} value={s}>
              {s || "All statuses"}
            </option>
          ))}
        </select>

        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search title…"
          aria-label="Search assignments"
          className={inputClass}
        />
      </div>

      {error && (
        <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      {assignments === null ? (
        <p className={mutedTextClass}>Loading assignments…</p>
      ) : assignments.length === 0 ? (
        <p className={mutedTextClass}>No assignments match these filters.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {assignments.map((assignment) => (
            <li key={assignment.id} className={cardClass}>
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <span className="font-medium">{assignment.title}</span>
                <span className={mutedTextClass}>{assignment.status}</span>
              </div>

              <p className={`mt-1 ${mutedTextClass}`}>
                {assignment.className} · {assignment.subjectName} · {assignment.teacherName}
              </p>
              <p className={mutedTextClass}>
                Due {formatDateTime(assignment.deadline)} · {assignment.maxMarks} marks
              </p>

              <button
                type="button"
                onClick={() => toggle(assignment.id)}
                aria-expanded={expandedId === assignment.id}
                className={`mt-2 ${subtleButtonClass}`}
              >
                {expandedId === assignment.id ? "Hide submissions" : "View submissions"}
              </button>

              {expandedId === assignment.id && <SubmissionList
                assignmentId={assignment.id}
                submissions={submissions}
              />}
            </li>
          ))}
        </ul>
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
    return <p className={`mt-2 ${mutedTextClass}`}>Loading submissions…</p>;
  }

  if (submissions.items.length === 0) {
    return <p className={`mt-2 ${mutedTextClass}`}>No submissions yet.</p>;
  }

  return (
    <ul className="mt-2 divide-y divide-black/10 dark:divide-white/10">
      {submissions.items.map((submission) => (
        <li key={submission.id} className="py-2 text-sm">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <span>
              {submission.studentName}{" "}
              <span className="text-black/50 dark:text-white/50">{submission.studentEmail}</span>
            </span>
            <span className={mutedTextClass}>
              {submission.status}
              {submission.marks !== null && submission.marks !== undefined
                ? ` · ${submission.marks}/${submission.assignmentMaxMarks}`
                : ""}
            </span>
          </div>
          <p className={mutedTextClass}>Submitted {formatDateTime(submission.submittedAt)}</p>
          {submission.feedback && <p className="mt-1">{submission.feedback}</p>}
        </li>
      ))}
    </ul>
  );
}
