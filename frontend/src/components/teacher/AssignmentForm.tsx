"use client";

import { useState, type FormEvent } from "react";
import { fromDateTimeLocalValue, toDateTimeLocalValue } from "@/lib/datetime";
import { inputClass, primaryButtonClass, subtleButtonClass } from "@/components/ui/styles";
import type { Assignment, CreateAssignmentInput, Subject } from "@/types";

const TITLE_MAX_LENGTH = 300;

interface AssignmentFormProps {
  subjects: Subject[];
  /** Present when editing; the subject cannot change after creation (API has no such field). */
  assignment?: Assignment;
  onSubmit: (input: CreateAssignmentInput) => Promise<void>;
  onCancel?: () => void;
}

/**
 * Create/edit form. Validation mirrors the server's AssignmentValidators so users
 * get immediate feedback; the API remains the enforcement layer.
 */
export function AssignmentForm({ subjects, assignment, onSubmit, onCancel }: AssignmentFormProps) {
  const isEditing = assignment !== undefined;

  const [title, setTitle] = useState(assignment?.title ?? "");
  const [description, setDescription] = useState(assignment?.description ?? "");
  const [deadline, setDeadline] = useState(toDateTimeLocalValue(assignment?.deadline));
  const [maxMarks, setMaxMarks] = useState(String(assignment?.maxMarks ?? ""));
  const [subjectId, setSubjectId] = useState(assignment?.subjectId ?? "");
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const validate = (): string | null => {
    if (!title.trim()) return "Title is required.";
    if (title.length > TITLE_MAX_LENGTH) return `Title must be ${TITLE_MAX_LENGTH} characters or fewer.`;
    if (!description.trim()) return "Description is required.";
    if (!deadline) return "Deadline is required.";
    if (new Date(deadline).getTime() <= Date.now()) return "Deadline must be in the future.";

    const marks = Number(maxMarks);
    if (!Number.isInteger(marks) || marks <= 0) return "Max marks must be a whole number greater than 0.";
    if (!isEditing && !subjectId) return "Select a subject.";

    return null;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setIsSaving(true);
    try {
      await onSubmit({
        title: title.trim(),
        description: description.trim(),
        deadline: fromDateTimeLocalValue(deadline),
        maxMarks: Number(maxMarks),
        subjectId: assignment?.subjectId ?? subjectId,
      });

      if (!isEditing) {
        setTitle("");
        setDescription("");
        setDeadline("");
        setMaxMarks("");
        setSubjectId("");
      }
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to save assignment.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm">
          <span>Title</span>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={TITLE_MAX_LENGTH}
            placeholder="e.g. Chapter 4 problem set"
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span>Subject</span>
          <select
            value={assignment?.subjectId ?? subjectId}
            onChange={(e) => setSubjectId(e.target.value)}
            disabled={isEditing}
            className={inputClass}
          >
            <option value="">Select subject…</option>
            {subjects.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name} ({s.code}) — {s.className}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span>Deadline</span>
          <input
            type="datetime-local"
            value={deadline}
            onChange={(e) => setDeadline(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span>Max marks</span>
          <input
            type="number"
            min={1}
            step={1}
            value={maxMarks}
            onChange={(e) => setMaxMarks(e.target.value)}
            placeholder="e.g. 100"
            className={inputClass}
          />
        </label>
      </div>

      <label className="flex flex-col gap-1 text-sm">
        <span>Description</span>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
          placeholder="What should students submit?"
          className={inputClass}
        />
      </label>

      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      <div className="flex items-center gap-3">
        <button type="submit" disabled={isSaving} className={primaryButtonClass}>
          {isSaving ? "Saving…" : isEditing ? "Save changes" : "Create assignment"}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className={subtleButtonClass}>
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}
