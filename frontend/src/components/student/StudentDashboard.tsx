"use client";

import { useEffect, useState } from "react";
import { getAssignments } from "@/lib/api/assignments";
import { getMySubmissions } from "@/lib/api/submissions";
import { AssignmentCard } from "@/components/student/AssignmentCard";
import { MySubmissionsPanel } from "@/components/student/MySubmissionsPanel";
import { mutedTextClass } from "@/components/ui/styles";
import type { Assignment, Submission } from "@/types";

/**
 * Owns both student-facing lists so a new submission updates the assignment card
 * and the marks/feedback view together. `GET /assignments` is already filtered
 * server-side to Published assignments for the student's class (business rule §7.3).
 */
export function StudentDashboard() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getAssignments(), getMySubmissions()])
      .then(([loadedAssignments, loadedSubmissions]) => {
        setAssignments(loadedAssignments);
        setSubmissions(loadedSubmissions);
      })
      .catch((e: unknown) =>
        setError(e instanceof Error ? e.message : "Failed to load your assignments."),
      )
      .finally(() => setIsLoading(false));
  }, []);

  const handleSaved = (saved: Submission) => {
    setSubmissions((prev) => {
      const isKnown = prev.some((s) => s.id === saved.id);
      return isKnown ? prev.map((s) => (s.id === saved.id ? saved : s)) : [...prev, saved];
    });
  };

  const findSubmission = (assignmentId: string) =>
    submissions.find((s) => s.assignmentId === assignmentId);

  if (isLoading) {
    return <p className={mutedTextClass}>Loading…</p>;
  }

  return (
    <div className="flex flex-col gap-10">
      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      <section>
        <h2 className="mb-3 text-lg font-semibold">Assignments</h2>
        {assignments.length === 0 ? (
          <p className={mutedTextClass}>No published assignments for your class yet.</p>
        ) : (
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
        )}
      </section>

      <MySubmissionsPanel submissions={submissions} />
    </div>
  );
}
