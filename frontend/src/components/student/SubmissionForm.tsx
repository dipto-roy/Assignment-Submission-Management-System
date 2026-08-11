"use client";

import { useState } from "react";
import { createSubmission, updateSubmission } from "@/lib/api/submissions";
import { isPastDeadline } from "@/lib/datetime";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Alert } from "@/components/ui/primitives";
import { fieldLabelClass, subtleTextClass, textareaClass } from "@/components/ui/styles";
import type { Assignment, Submission } from "@/types";

const CONTENT_MAX_LENGTH = 20_000;

/** Past this share of the limit the counter warns instead of sitting quiet. */
const COUNTER_WARN_RATIO = 0.9;

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
      <div className="flex flex-col gap-3 rounded-lg border border-border-subtle bg-muted/60 p-4">
        <p className="flex items-start gap-2 text-sm text-foreground-muted">
          <Icon name="alert-circle" size="sm" className="mt-0.5 text-danger" />
          {submission
            ? "The deadline has passed — your submission is locked and can no longer be edited."
            : "The deadline has passed — this assignment can no longer be submitted."}
        </p>
        {submission && (
          <p className="whitespace-pre-wrap border-t border-border-subtle pt-3 text-sm leading-relaxed text-foreground">
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

  const isNearLimit = content.length > CONTENT_MAX_LENGTH * COUNTER_WARN_RATIO;

  return (
    <div className="flex flex-col gap-3">
      <label className={fieldLabelClass}>
        <span className="flex items-center gap-1.5">
          <Icon name="edit" size="sm" className="text-primary" />
          {submission ? "Update your answer" : "Your answer"}
        </span>
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          rows={6}
          maxLength={CONTENT_MAX_LENGTH}
          placeholder="Type your answer here…"
          className={textareaClass}
        />
      </label>

      <p
        className={`text-right font-mono ${
          isNearLimit ? "text-xs font-medium text-accent-soft-foreground" : subtleTextClass
        }`}
      >
        {content.length.toLocaleString()} / {CONTENT_MAX_LENGTH.toLocaleString()} characters
      </p>

      {error && <Alert>{error}</Alert>}
      {notice && <Alert tone="success">{notice}</Alert>}

      <div>
        <Button icon={submission ? "refresh" : "send"} isBusy={isSaving} onClick={handleSave}>
          {isSaving ? "Saving…" : submission ? "Update submission" : "Submit"}
        </Button>
      </div>
    </div>
  );
}
