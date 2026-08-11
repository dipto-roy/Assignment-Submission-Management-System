"use client";

import { useEffect, useState } from "react";
import {
  getAssignmentSubmissions,
  gradeSubmission,
  setSubmissionStatus,
} from "@/lib/api/submissions";
import { formatDateTime } from "@/lib/datetime";
import { inputClass, mutedTextClass, primaryButtonClass } from "@/components/ui/styles";
import type { SubmissionDetail, SubmissionStatus } from "@/types";
import { AttachmentPanel } from "@/components/attachments/AttachmentPanel";

const SUBMISSION_STATUSES: SubmissionStatus[] = ["Submitted", "Late", "Graded", "Returned"];

const FEEDBACK_MAX_LENGTH = 5_000;

interface SubmissionsReviewProps {
  assignmentId: string;
  maxMarks: number;
}

/** Per-assignment review table: student, timing, status control, and the grading form. */
export function SubmissionsReview({ assignmentId, maxMarks }: SubmissionsReviewProps) {
  const [submissions, setSubmissions] = useState<SubmissionDetail[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isActive = true;

    getAssignmentSubmissions(assignmentId)
      .then((data) => {
        if (isActive) setSubmissions(data);
      })
      .catch((e: unknown) => {
        if (isActive) setError(e instanceof Error ? e.message : "Failed to load submissions.");
      })
      .finally(() => {
        if (isActive) setIsLoading(false);
      });

    return () => {
      isActive = false;
    };
  }, [assignmentId]);

  const handleStatusChange = async (id: string, status: SubmissionStatus) => {
    try {
      const updated = await setSubmissionStatus(id, status);
      setSubmissions((prev) => prev.map((s) => (s.id === id ? updated : s)));
      setError(null);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to change status.");
    }
  };

  const handleGraded = (updated: SubmissionDetail) => {
    setSubmissions((prev) => prev.map((s) => (s.id === updated.id ? updated : s)));
    setError(null);
  };

  if (isLoading) {
    return <p className={mutedTextClass}>Loading submissions…</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      {submissions.length === 0 ? (
        <p className={mutedTextClass}>No submissions yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-160 text-left text-sm">
            <thead className="border-b border-black/10 text-xs uppercase text-black/50 dark:border-white/15 dark:text-white/50">
              <tr>
                <th className="py-2 pr-3 font-medium">Student</th>
                <th className="py-2 pr-3 font-medium">Submitted</th>
                <th className="py-2 pr-3 font-medium">Status</th>
                <th className="py-2 pr-3 font-medium">Marks</th>
                <th className="py-2 font-medium">Grade &amp; feedback</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-black/10 dark:divide-white/10">
              {submissions.map((submission) => (
                <tr key={submission.id} className="align-top">
                  <td className="py-3 pr-3">
                    <div>{submission.studentName}</div>
                    <div className="text-xs text-black/50 dark:text-white/50">
                      {submission.studentEmail}
                    </div>
                  </td>
                  <td className="py-3 pr-3">
                    <div>{formatDateTime(submission.submittedAt)}</div>
                    {submission.updatedAt && (
                      <div className="text-xs text-black/50 dark:text-white/50">
                        updated {formatDateTime(submission.updatedAt)}
                      </div>
                    )}
                  </td>
                  <td className="py-3 pr-3">
                    <select
                      value={submission.status}
                      onChange={(e) => handleStatusChange(submission.id, e.target.value as SubmissionStatus)}
                      aria-label={`Status for ${submission.studentName}`}
                      className={inputClass}
                    >
                      {SUBMISSION_STATUSES.map((status) => (
                        <option key={status} value={status}>
                          {status}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="py-3 pr-3">
                    {submission.marks ?? "—"} / {submission.assignmentMaxMarks}
                  </td>
                  <td className="py-3">
                    <GradeForm
                      submission={submission}
                      maxMarks={maxMarks}
                      onGraded={handleGraded}
                      onError={setError}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <details className="text-sm">
        <summary className="cursor-pointer text-black/60 dark:text-white/60">
          View submitted answers
        </summary>
        <ul className="mt-2 flex flex-col gap-3">
          {submissions.map((submission) => (
            <li key={submission.id}>
              <p className="font-medium">{submission.studentName}</p>
              <p className="whitespace-pre-wrap text-black/70 dark:text-white/70">
                {submission.content}
              </p>
              {/* Read-only: marking the work does not entitle a teacher to delete it. */}
              {submission.attachments.length > 0 && (
                <div className="mt-2">
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
    <div className="flex flex-col gap-2">
      <input
        type="number"
        min={0}
        max={maxMarks}
        step={1}
        value={marks}
        onChange={(e) => setMarks(e.target.value)}
        placeholder={`0–${maxMarks}`}
        aria-label={`Marks for ${submission.studentName}`}
        className={`${inputClass} w-28`}
      />
      <textarea
        value={feedback}
        onChange={(e) => setFeedback(e.target.value)}
        rows={2}
        maxLength={FEEDBACK_MAX_LENGTH}
        placeholder="Feedback (optional)"
        aria-label={`Feedback for ${submission.studentName}`}
        className={`${inputClass} w-full min-w-48`}
      />
      <button type="button" onClick={handleSave} disabled={isSaving} className={primaryButtonClass}>
        {isSaving ? "Saving…" : "Save grade"}
      </button>
    </div>
  );
}
