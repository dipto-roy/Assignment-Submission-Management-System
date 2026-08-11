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
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import {
  Alert,
  Badge,
  EmptyState,
  LoadingLine,
  SectionHeading,
} from "@/components/ui/primitives";
import { cardClass, mutedTextClass, panelClass } from "@/components/ui/styles";
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
    <section className="flex flex-col gap-8">
      <div className={panelClass}>
        <SectionHeading
          icon="plus"
          title="New assignment"
          description="Create it as a draft, then publish when the brief is final."
        />

        {isLoadingSubjects ? (
          <LoadingLine label="Loading subjects…" />
        ) : subjects.length === 0 ? (
          <EmptyState
            icon="book-open"
            title="No subjects assigned to you"
            description="Ask an admin to assign you a subject before creating assignments."
          />
        ) : (
          <AssignmentForm subjects={subjects} onSubmit={handleCreate} />
        )}

        {subjectsError && <Alert className="mt-3">{subjectsError}</Alert>}
      </div>

      <div>
        <SectionHeading
          icon="layers"
          title="My assignments"
          meta={
            assignments.length > 0 ? (
              <Badge tone="primary">{assignments.length}</Badge>
            ) : undefined
          }
        />

        {error && <Alert className="mb-3">{error}</Alert>}

        {isLoading ? (
          <LoadingLine label="Loading assignments…" />
        ) : assignments.length === 0 ? (
          <EmptyState
            icon="inbox"
            title="No assignments yet"
            description="The first assignment you create for your subjects will appear here."
          />
        ) : (
          <ul className="flex flex-col gap-4">
            {assignments.map((assignment) => (
              <li key={assignment.id} className={cardClass}>
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="font-semibold text-foreground">{assignment.title}</h3>
                      <StatusBadge status={assignment.status} />
                    </div>

                    <p
                      className={`mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 ${mutedTextClass}`}
                    >
                      <span className="inline-flex items-center gap-1.5">
                        <Icon name="book-open" size="sm" />
                        {assignment.subjectName}
                      </span>
                      <span className="inline-flex items-center gap-1.5">
                        <Icon name="users" size="sm" />
                        {assignment.className}
                      </span>
                      <span className="inline-flex items-center gap-1.5">
                        <Icon name="calendar-clock" size="sm" />
                        {formatDateTime(assignment.deadline)}
                      </span>
                      <span className="inline-flex items-center gap-1.5">
                        <Icon name="clock" size="sm" />
                        {describeTimeRemaining(assignment.deadline)}
                      </span>
                      <span className="inline-flex items-center gap-1.5">
                        <Icon name="check" size="sm" />
                        <span className="font-mono">{assignment.maxMarks}</span> marks
                      </span>
                    </p>
                  </div>

                  <div className="flex flex-wrap items-center gap-1">
                    <Button
                      variant="subtle"
                      icon={assignment.status === "Published" ? "eye-off" : "upload"}
                      onClick={() => handleTogglePublish(assignment)}
                    >
                      {assignment.status === "Published" ? "Unpublish" : "Publish"}
                    </Button>

                    <Button
                      variant="subtle"
                      icon={editingId === assignment.id ? "x" : "edit"}
                      aria-expanded={editingId === assignment.id}
                      onClick={() =>
                        setEditingId(editingId === assignment.id ? null : assignment.id)
                      }
                    >
                      {editingId === assignment.id ? "Close editor" : "Edit"}
                    </Button>

                    <Button
                      variant="subtle"
                      icon="users"
                      aria-expanded={expandedId === assignment.id}
                      onClick={() =>
                        setExpandedId(expandedId === assignment.id ? null : assignment.id)
                      }
                    >
                      {expandedId === assignment.id ? "Hide submissions" : "Submissions"}
                    </Button>

                    <Button
                      variant="danger"
                      icon="trash"
                      onClick={() => handleDelete(assignment.id)}
                    >
                      Delete
                    </Button>
                  </div>
                </div>

                <p className="mt-3 whitespace-pre-wrap text-sm leading-relaxed text-foreground-muted">
                  {assignment.description}
                </p>

                {/* The teacher owns the assignment, so they may add and remove its files. */}
                <div className="mt-4">
                  <AttachmentPanel
                    owner="assignment"
                    ownerId={assignment.id}
                    attachments={assignment.attachments}
                    canModify
                    label="Assignment files"
                  />
                </div>

                {editingId === assignment.id && (
                  <div className="app-animate-in mt-5 border-t border-border-subtle pt-5">
                    <AssignmentForm
                      subjects={subjects}
                      assignment={assignment}
                      onSubmit={(input) => handleUpdate(assignment.id, input)}
                      onCancel={() => setEditingId(null)}
                    />
                  </div>
                )}

                {expandedId === assignment.id && (
                  <div className="app-animate-in mt-5 border-t border-border-subtle pt-5">
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
    <Badge tone={isPublished ? "success" : "neutral"} icon={isPublished ? "check-circle" : "edit"}>
      {status}
    </Badge>
  );
}
