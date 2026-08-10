"use client";

import { useState } from "react";
import { createSubmission, updateSubmission } from "@/lib/api/submissions";
import { isPastDeadline } from "@/lib/datetime";
import { inputClass, mutedTextClass, primaryButtonClass } from "@/components/ui/styles";
import type { Assignment, Submission } from "@/types";

const CONTENT_MAX_LENGTH = 20_000;

interface SubmissionFormProps {
  assignment: Assignment;
  /** The student's existing submission, if they have already submitted. */
  submission?: Submission;
  onSaved: (saved: Submission) => void;
}

/**
 * Submit or update a text answer. The deadline lock mirrors business rules §7.1/§7.2;
 * the API rejects late writes regardless of what the UI allows.
 */
export function SubmissionForm({ assignment, submission, onSaved }: SubmissionFormProps) {
  const [content, setContent] = useState(submission?.content ?? "");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const isLocked = isPastDeadline(assignment.deadline);

  if (isLocked) {
    return (
      <div className="flex flex-col gap-2">
        <p className={mutedTextClass}>
          {submission
            ? "The deadline has passed — your submission is locked and can no longer be edited."
            : "The deadline has passed — this assignment can no longer be submitted."}
        </p>
        {submission && (
          <p className="whitespace-pre-wrap text-sm text-black/70 dark:text-white/70">
            {submission.content}
          </p>
        )}
      </div>
    );
  }

  const validate = (): string | null => {
    if (!content.trim()) return "Your answer cannot be empty.";
    if (content.length > CONTENT_MAX_LENGTH) {
      return `Your answer must be ${CONTENT_MAX_LENGTH} characters or fewer.`;
    }
    return null;
  };

  const handleSave = async () => {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setNotice(null);
    setIsSaving(true);
    try {
      const saved = submission
        ? await updateSubmission(submission.id, content.trim())
        : await createSubmission(assignment.id, content.trim());

      onSaved(saved);
      setNotice(submission ? "Submission updated." : "Submitted.");
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to save your submission.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex flex-col gap-2">
      <label className="flex flex-col gap-1 text-sm">
        <span>{submission ? "Update your answer" : "Your answer"}</span>
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          rows={5}
          maxLength={CONTENT_MAX_LENGTH}
          placeholder="Type your answer here…"
          className={`${inputClass} w-full`}
        />
      </label>

      <p className={mutedTextClass}>
        {content.length.toLocaleString()} / {CONTENT_MAX_LENGTH.toLocaleString()} characters
      </p>

      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
      {notice && (
        <p role="status" className="text-sm text-green-700 dark:text-green-400">
          {notice}
        </p>
      )}

      <div>
        <button type="button" onClick={handleSave} disabled={isSaving} className={primaryButtonClass}>
          {isSaving ? "Saving…" : submission ? "Update submission" : "Submit"}
        </button>
      </div>
    </div>
  );
}
