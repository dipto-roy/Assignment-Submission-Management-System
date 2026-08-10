"use client";

import { useState } from "react";
import { SubmissionForm } from "@/components/student/SubmissionForm";
import { describeTimeRemaining, formatDateTime, isPastDeadline } from "@/lib/datetime";
import { cardClass, mutedTextClass, subtleButtonClass } from "@/components/ui/styles";
import type { Assignment, Submission } from "@/types";

interface AssignmentCardProps {
  assignment: Assignment;
  submission?: Submission;
  onSaved: (saved: Submission) => void;
}

/** Assignment detail plus the student's own submission state and editor. */
export function AssignmentCard({ assignment, submission, onSaved }: AssignmentCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const isLocked = isPastDeadline(assignment.deadline);

  return (
    <li className={cardClass}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-medium">{assignment.title}</h3>
            <SubmissionBadge submission={submission} isLocked={isLocked} />
          </div>
          <p className={mutedTextClass}>
            {assignment.subjectName} — {assignment.className} · due{" "}
            {formatDateTime(assignment.deadline)} · {assignment.maxMarks} marks
          </p>
          <p className={`text-sm ${isLocked ? "text-red-600 dark:text-red-400" : "text-black/60 dark:text-white/60"}`}>
            {describeTimeRemaining(assignment.deadline)}
          </p>
        </div>

        <button type="button" onClick={() => setIsExpanded(!isExpanded)} className={subtleButtonClass}>
          {isExpanded ? "Close" : submission ? "View / edit" : "Open"}
        </button>
      </div>

      {isExpanded && (
        <div className="mt-4 flex flex-col gap-4 border-t border-black/10 pt-4 dark:border-white/15">
          <div>
            <h4 className="text-sm font-medium">Instructions</h4>
            <p className="mt-1 whitespace-pre-wrap text-sm text-black/70 dark:text-white/70">
              {assignment.description}
            </p>
          </div>

          {submission && (
            <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-2">
              <div className="flex gap-2">
                <dt className="text-black/50 dark:text-white/50">Submitted</dt>
                <dd>{formatDateTime(submission.submittedAt)}</dd>
              </div>
              {submission.updatedAt && (
                <div className="flex gap-2">
                  <dt className="text-black/50 dark:text-white/50">Last updated</dt>
                  <dd>{formatDateTime(submission.updatedAt)}</dd>
                </div>
              )}
              <div className="flex gap-2">
                <dt className="text-black/50 dark:text-white/50">Marks</dt>
                <dd>
                  {submission.marks ?? "Not graded"}
                  {submission.marks != null && ` / ${assignment.maxMarks}`}
                </dd>
              </div>
              {submission.feedback && (
                <div className="flex gap-2 sm:col-span-2">
                  <dt className="text-black/50 dark:text-white/50">Feedback</dt>
                  <dd className="whitespace-pre-wrap">{submission.feedback}</dd>
                </div>
              )}
            </dl>
          )}

          <SubmissionForm assignment={assignment} submission={submission} onSaved={onSaved} />
        </div>
      )}
    </li>
  );
}

function SubmissionBadge({ submission, isLocked }: { submission?: Submission; isLocked: boolean }) {
  const label = submission ? submission.status : isLocked ? "Missed" : "Not submitted";

  const tone =
    submission?.status === "Graded" || submission?.status === "Returned"
      ? "bg-green-600/10 text-green-700 dark:text-green-400"
      : submission
        ? "bg-blue-600/10 text-blue-700 dark:text-blue-400"
        : isLocked
          ? "bg-red-600/10 text-red-700 dark:text-red-400"
          : "bg-black/10 text-black/60 dark:bg-white/10 dark:text-white/60";

  return <span className={`rounded-full px-2 py-0.5 text-xs ${tone}`}>{label}</span>;
}
