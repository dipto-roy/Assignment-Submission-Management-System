"use client";

import { useEffect, useState } from "react";
import {
  createAssignment,
  deleteAssignment,
  getAssignments,
  setAssignmentPublishState,
  updateAssignment,
} from "@/lib/api/assignments";
import { AssignmentForm } from "@/components/teacher/AssignmentForm";
import { SubmissionsReview } from "@/components/teacher/SubmissionsReview";
import { useTeacherSubjects } from "@/lib/hooks/useTeacherSubjects";
import { describeTimeRemaining, formatDateTime } from "@/lib/datetime";
import {
  cardClass,
  dangerButtonClass,
  mutedTextClass,
  subtleButtonClass,
} from "@/components/ui/styles";
import type { Assignment, CreateAssignmentInput } from "@/types";
import { AttachmentPanel } from "@/components/attachments/AttachmentPanel";

/** Assignment list + CRUD + publish toggle, scoped by the API to the teacher's own subjects. */
export function AssignmentsPanel() {
  const { subjects, isLoading: isLoadingSubjects, error: subjectsError } = useTeacherSubjects();
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const reload = async () => {
    try {
      setAssignments(await getAssignments());
      setError(null);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load assignments.");
    }
  };

  useEffect(() => {
    getAssignments()
      .then(setAssignments)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "Failed to load assignments."))
      .finally(() => setIsLoading(false));
  }, []);

  const handleCreate = async (input: CreateAssignmentInput) => {
    await createAssignment(input);
    await reload();
  };

  const handleUpdate = async (id: string, input: CreateAssignmentInput) => {
    const { title, description, deadline, maxMarks } = input;
    await updateAssignment(id, { title, description, deadline, maxMarks });
    setEditingId(null);
    await reload();
  };

  const handleTogglePublish = async (assignment: Assignment) => {
    try {
      const updated = await setAssignmentPublishState(
        assignment.id,
        assignment.status !== "Published",
      );
      setAssignments((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setError(null);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to change publish state.");
    }
  };

  // The API blocks deletion once submissions exist (§11) — surface that message verbatim.
  const handleDelete = async (id: string) => {
    try {
      await deleteAssignment(id);
      setError(null);
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete assignment.");
    }
  };

  return (
    <section className="flex flex-col gap-6">
      <div>
        <h2 className="mb-3 text-lg font-semibold">New assignment</h2>
        {isLoadingSubjects ? (
          <p className={mutedTextClass}>Loading subjects…</p>
        ) : subjects.length === 0 ? (
          <p className={mutedTextClass}>
            You are not assigned to any subject yet. Ask an admin to assign one.
          </p>
        ) : (
          <AssignmentForm subjects={subjects} onSubmit={handleCreate} />
        )}
        {subjectsError && (
          <p role="alert" className="mt-2 text-sm text-red-600 dark:text-red-400">
            {subjectsError}
          </p>
        )}
      </div>

      <div>
        <h2 className="mb-3 text-lg font-semibold">My assignments</h2>

        {error && (
          <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">
            {error}
          </p>
        )}

        {isLoading ? (
          <p className={mutedTextClass}>Loading…</p>
        ) : assignments.length === 0 ? (
          <p className={mutedTextClass}>No assignments yet.</p>
        ) : (
          <ul className="flex flex-col gap-4">
            {assignments.map((assignment) => (
              <li key={assignment.id} className={cardClass}>
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="font-medium">{assignment.title}</h3>
                      <StatusBadge status={assignment.status} />
                    </div>
                    <p className={mutedTextClass}>
                      {assignment.subjectName} — {assignment.className} · due{" "}
                      {formatDateTime(assignment.deadline)} ·{" "}
                      {describeTimeRemaining(assignment.deadline)} · {assignment.maxMarks} marks
                    </p>
                  </div>

                  <div className="flex flex-wrap items-center gap-3">
                    <button
                      type="button"
                      onClick={() => handleTogglePublish(assignment)}
                      className={subtleButtonClass}
                    >
                      {assignment.status === "Published" ? "Unpublish" : "Publish"}
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingId(editingId === assignment.id ? null : assignment.id)}
                      className={subtleButtonClass}
                    >
                      {editingId === assignment.id ? "Close editor" : "Edit"}
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        setExpandedId(expandedId === assignment.id ? null : assignment.id)
                      }
                      className={subtleButtonClass}
                    >
                      {expandedId === assignment.id ? "Hide submissions" : "Submissions"}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(assignment.id)}
                      className={dangerButtonClass}
                    >
                      Delete
                    </button>
                  </div>
                </div>

                <p className="mt-2 whitespace-pre-wrap text-sm text-black/70 dark:text-white/70">
                  {assignment.description}
                </p>

                {/* The teacher owns the assignment, so they may add and remove its files. */}
                <div className="mt-3">
                  <AttachmentPanel
                    owner="assignment"
                    ownerId={assignment.id}
                    attachments={assignment.attachments}
                    canModify
                    label="Assignment files"
                  />
                </div>

                {editingId === assignment.id && (
                  <div className="mt-4 border-t border-black/10 pt-4 dark:border-white/15">
                    <AssignmentForm
                      subjects={subjects}
                      assignment={assignment}
                      onSubmit={(input) => handleUpdate(assignment.id, input)}
                      onCancel={() => setEditingId(null)}
                    />
                  </div>
                )}

                {expandedId === assignment.id && (
                  <div className="mt-4 border-t border-black/10 pt-4 dark:border-white/15">
                    <SubmissionsReview
                      assignmentId={assignment.id}
                      maxMarks={assignment.maxMarks}
                    />
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}

function StatusBadge({ status }: { status: Assignment["status"] }) {
  const isPublished = status === "Published";
  return (
    <span
      className={`rounded-full px-2 py-0.5 text-xs ${
        isPublished
          ? "bg-green-600/10 text-green-700 dark:text-green-400"
          : "bg-black/10 text-black/60 dark:bg-white/10 dark:text-white/60"
      }`}
    >
      {status}
    </span>
  );
}
