"use client";

import { useState } from "react";
import {
  getAssignmentSubmissionsPage,
  gradeSubmission,
  setSubmissionStatus,
} from "@/lib/api/submissions";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { Pagination } from "@/components/ui/Pagination";
import { formatDateTime } from "@/lib/datetime";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Alert, EmptyState, LoadingLine } from "@/components/ui/primitives";
import { AttachmentPanel } from "@/components/attachments/AttachmentPanel";
import {
  inputClass,
  subtleTextClass,
  tableClass,
  tableHeadClass,
  tableRowClass,
  textareaClass,
} from "@/components/ui/styles";
import type { SubmissionDetail, SubmissionStatus } from "@/types";

const SUBMISSION_STATUSES: SubmissionStatus[] = ["Submitted", "Late", "Graded", "Returned"];

const FEEDBACK_MAX_LENGTH = 5_000;

interface SubmissionsReviewProps {
  assignmentId: string;
  maxMarks: number;
}

/** Per-assignment review table: student, timing, status control, and the grading form. */
export function SubmissionsReview({ assignmentId, maxMarks }: SubmissionsReviewProps) {
  const [writeError, setWriteError] = useState<string | null>(null);

  const {
    items: submissions,
    meta,
    isLoading,
    isRefreshing,
    error: loadError,
    setPage,
    setPageSize,
    setItems,
  } = usePagedList<SubmissionDetail>(
    (params) => getAssignmentSubmissionsPage(assignmentId, params),
    { filters: [assignmentId], errorMessage: "Failed to load submissions." },
  );

  const error = writeError ?? loadError;

  const handleStatusChange = async (id: string, status: SubmissionStatus) => {
    try {
      const updated = await setSubmissionStatus(id, status);
      setItems((prev) => prev.map((s) => (s.id === id ? updated : s)));
      setWriteError(null);
    } catch (e: unknown) {
      setWriteError(e instanceof Error ? e.message : "Failed to change status.");
    }
  };

  const handleGraded = (updated: SubmissionDetail) => {
    setItems((prev) => prev.map((s) => (s.id === updated.id ? updated : s)));
    setWriteError(null);
  };

  if (isLoading) {
    return <LoadingLine label="Loading submissions…" />;
  }

  return (
    <div className="flex flex-col gap-4">
      {error && <Alert>{error}</Alert>}

      {submissions.length === 0 ? (
        <EmptyState
          icon="inbox"
          title="No submissions yet."
          description="Students who hand this assignment in will appear here for grading."
        />
      ) : (
        <div className="overflow-x-auto rounded-xl border border-border-subtle bg-surface">
          <table className={tableClass}>
            <thead className={tableHeadClass}>
              <tr>
                <th className="px-4 py-3">Student</th>
                <th className="px-4 py-3">Submitted</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Marks</th>
                <th className="px-4 py-3">Grade &amp; feedback</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {submissions.map((submission) => (
                <tr key={submission.id} className={tableRowClass}>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary-soft-foreground">
                        <Icon name="user" size="sm" />
                      </span>
                      <div className="min-w-0">
                        <div className="font-medium text-foreground">{submission.studentName}</div>
                        <div className={subtleTextClass}>{submission.studentEmail}</div>
                      </div>
                    </div>
                  </td>

                  <td className="px-4 py-3">
                    <div className="text-foreground">{formatDateTime(submission.submittedAt)}</div>
                    {submission.updatedAt && (
                      <div className={subtleTextClass}>
                        updated {formatDateTime(submission.updatedAt)}
                      </div>
                    )}
                  </td>

                  <td className="px-4 py-3">
                    <select
                      value={submission.status}
                      onChange={(e) =>
                        handleStatusChange(submission.id, e.target.value as SubmissionStatus)
                      }
                      aria-label={`Status for ${submission.studentName}`}
                      className={`${inputClass} min-w-32`}
                    >
                      {SUBMISSION_STATUSES.map((status) => (
                        <option key={status} value={status}>
                          {status}
                        </option>
                      ))}
                    </select>
                  </td>

                  <td className="px-4 py-3 font-mono text-foreground">
                    {submission.marks ?? "—"} / {submission.assignmentMaxMarks}
                  </td>

                  <td className="px-4 py-3">
                    <GradeForm
                      submission={submission}
                      maxMarks={maxMarks}
                      onGraded={handleGraded}
                      onError={setWriteError}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {submissions.length > 0 && (
        <Pagination
          meta={meta}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
          label="submissions"
          isBusy={isRefreshing}
        />
      )}

      {submissions.length > 0 && (
        <details className="group rounded-xl border border-border-subtle bg-surface">
          <summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 px-4 py-3 text-sm font-medium text-foreground transition-colors duration-150 hover:bg-muted">
            <Icon
              name="chevron-down"
              size="sm"
              className="text-primary transition-transform duration-200 group-open:rotate-180"
            />
            View submitted answers
          </summary>

          <ul className="divide-y divide-border-subtle border-t border-border-subtle">
            {submissions.map((submission) => (
              <li key={submission.id} className="px-4 py-3 text-sm">
                <p className="font-medium text-foreground">{submission.studentName}</p>
                <p className="mt-1 whitespace-pre-wrap leading-relaxed text-foreground-muted">
                  {submission.content}
                </p>

                {/* Read-only: marking the work does not entitle a teacher to delete it. */}
                {submission.attachments.length > 0 && (
                  <div className="mt-3">
                    <AttachmentPanel
                      owner="submission"
                      ownerId={submission.id}
                      attachments={submission.attachments}
                      label="Submitted files"
                    />
                  </div>
                )}
              </li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}

interface GradeFormProps {
  submission: SubmissionDetail;
  maxMarks: number;
  onGraded: (updated: SubmissionDetail) => void;
  onError: (message: string) => void;
}

/** Marks + feedback. `marks <= maxMarks` mirrors the server rule in SubmissionValidators/§7.6. */
function GradeForm({ submission, maxMarks, onGraded, onError }: GradeFormProps) {
  const [marks, setMarks] = useState(submission.marks != null ? String(submission.marks) : "");
  const [feedback, setFeedback] = useState(submission.feedback ?? "");
  const [isSaving, setIsSaving] = useState(false);

  const validate = (): string | null => {
    const value = Number(marks);
    if (marks.trim() === "" || !Number.isInteger(value)) return "Marks must be a whole number.";
    if (value < 0) return "Marks cannot be negative.";
    if (value > maxMarks) return `Marks cannot exceed the assignment maximum of ${maxMarks}.`;
    if (feedback.length > FEEDBACK_MAX_LENGTH) {
      return `Feedback must be ${FEEDBACK_MAX_LENGTH} characters or fewer.`;
    }
    return null;
  };

  const handleSave = async () => {
    const validationError = validate();
    if (validationError) {
      onError(validationError);
      return;
    }

    setIsSaving(true);
    try {
      onGraded(
        await gradeSubmission(submission.id, {
          marks: Number(marks),
          feedback: feedback.trim() === "" ? null : feedback.trim(),
        }),
      );
    } catch (e: unknown) {
      onError(e instanceof Error ? e.message : "Failed to save grade.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex min-w-56 flex-col gap-2">
      <input
        type="number"
        min={0}
        max={maxMarks}
        step={1}
        value={marks}
        onChange={(e) => setMarks(e.target.value)}
        placeholder={`0–${maxMarks}`}
        aria-label={`Marks for ${submission.studentName}`}
        className={`${inputClass} w-28 font-mono`}
      />

      <textarea
        value={feedback}
        onChange={(e) => setFeedback(e.target.value)}
        rows={2}
        maxLength={FEEDBACK_MAX_LENGTH}
        placeholder="Feedback (optional)"
        aria-label={`Feedback for ${submission.studentName}`}
        className={`${textareaClass} min-h-16`}
      />

      <Button icon="check" isBusy={isSaving} onClick={handleSave} className="self-start">
        {isSaving ? "Saving…" : "Save grade"}
      </Button>
    </div>
  );
}
