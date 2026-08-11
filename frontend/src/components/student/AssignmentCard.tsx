"use client";

import { useState } from "react";
import { AttachmentPanel } from "@/components/attachments/AttachmentPanel";
import { SubmissionForm } from "@/components/student/SubmissionForm";
import { describeTimeRemaining, formatDateTime, isPastDeadline } from "@/lib/datetime";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Badge } from "@/components/ui/primitives";
import { cardClass, mutedTextClass } from "@/components/ui/styles";
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
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-semibold text-foreground">{assignment.title}</h3>
            <SubmissionBadge submission={submission} isLocked={isLocked} />
          </div>

          <p className={`mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 ${mutedTextClass}`}>
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
              <Icon name="check" size="sm" />
              <span className="font-mono">{assignment.maxMarks}</span> marks
            </span>
          </p>

          {/* Colour alone never carries the deadline state — the icon changes with it. */}
          <p
            className={`mt-1 inline-flex items-center gap-1.5 text-sm font-medium ${
              isLocked ? "text-danger" : "text-foreground-muted"
            }`}
          >
            <Icon name={isLocked ? "alert-circle" : "clock"} size="sm" />
            {describeTimeRemaining(assignment.deadline)}
          </p>
        </div>

        <Button
          variant={isExpanded ? "subtle" : "secondary"}
          icon={isExpanded ? "eye-off" : "eye"}
          onClick={() => setIsExpanded(!isExpanded)}
          aria-expanded={isExpanded}
        >
          {isExpanded ? "Close" : submission ? "View / edit" : "Open"}
        </Button>
      </div>

      {isExpanded && (
        <div className="app-animate-in mt-5 flex flex-col gap-5 border-t border-border-subtle pt-5">
          <div>
            <h4 className="flex items-center gap-1.5 text-sm font-semibold text-foreground">
              <Icon name="file-text" size="sm" className="text-primary" />
              Instructions
            </h4>
            <p className="mt-1.5 whitespace-pre-wrap text-sm leading-relaxed text-foreground-muted">
              {assignment.description}
            </p>
          </div>

          {/* Read-only: the brief belongs to the teacher. */}
          {assignment.attachments.length > 0 && (
            <AttachmentPanel
              owner="assignment"
              ownerId={assignment.id}
              attachments={assignment.attachments}
              label="Assignment files"
            />
          )}

          {/* The student's own files. Editable until the deadline locks the submission,
              matching the rule the server enforces on the text content. */}
          {submission && (
            <AttachmentPanel
              owner="submission"
              ownerId={submission.id}
              attachments={submission.attachments}
              canModify={!isLocked}
              label="Your files"
            />
          )}

          {submission && (
            <dl className="grid gap-x-6 gap-y-2 rounded-lg bg-muted/60 p-4 text-sm sm:grid-cols-2">
              <DetailRow icon="send" label="Submitted">
                {formatDateTime(submission.submittedAt)}
              </DetailRow>

              {submission.updatedAt && (
                <DetailRow icon="refresh" label="Last updated">
                  {formatDateTime(submission.updatedAt)}
                </DetailRow>
              )}

              <DetailRow icon="check-circle" label="Marks">
                <span className="font-mono">
                  {submission.marks ?? "Not graded"}
                  {submission.marks != null && ` / ${assignment.maxMarks}`}
                </span>
              </DetailRow>

              {submission.feedback && (
                <div className="sm:col-span-2">
                  <DetailRow icon="mail" label="Feedback">
                    <span className="whitespace-pre-wrap">{submission.feedback}</span>
                  </DetailRow>
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

function DetailRow({
  icon,
  label,
  children,
}: {
  icon: "send" | "refresh" | "check-circle" | "mail";
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex gap-2">
      <dt className="flex shrink-0 items-center gap-1.5 text-foreground-subtle">
        <Icon name={icon} size="sm" />
        {label}
      </dt>
      <dd className="min-w-0 text-foreground">{children}</dd>
    </div>
  );
}

function SubmissionBadge({ submission, isLocked }: { submission?: Submission; isLocked: boolean }) {
  if (submission) {
    const isComplete = submission.status === "Graded" || submission.status === "Returned";
    return (
      <Badge tone={isComplete ? "success" : "primary"} icon={isComplete ? "check-circle" : "send"}>
        {submission.status}
      </Badge>
    );
  }

  if (isLocked) {
    return (
      <Badge tone="danger" icon="alert-circle">
        Missed
      </Badge>
    );
  }

  return (
    <Badge tone="neutral" icon="clock">
      Not submitted
    </Badge>
  );
}
