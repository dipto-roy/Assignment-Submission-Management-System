"use client";

import { useMemo, useState } from "react";
import { formatDateTime } from "@/lib/datetime";
import { DEFAULT_PAGE_SIZE } from "@/lib/api/query";
import { Pagination } from "@/components/ui/Pagination";
import { Badge, EmptyState, SectionHeading } from "@/components/ui/primitives";
import {
  mutedTextClass,
  subtleTextClass,
  tableClass,
  tableHeadClass,
  tableRowClass,
} from "@/components/ui/styles";
import type { Submission, SubmissionStatus } from "@/types";

/** Status → badge tone/icon. One map so every status looks the same wherever it renders. */
export const SUBMISSION_BADGE: Record<
  SubmissionStatus,
  { tone: "primary" | "success" | "warning" | "info"; icon: "send" | "check-circle" | "clock" | "mail" }
> = {
  Submitted: { tone: "primary", icon: "send" },
  Late: { tone: "warning", icon: "clock" },
  Graded: { tone: "success", icon: "check-circle" },
  Returned: { tone: "info", icon: "mail" },
};

/**
 * `GET /submissions/mine` view — status, marks, and teacher feedback in one place.
 *
 * Paging happens here rather than on the server: the dashboard above already holds every
 * submission (it needs the whole set to tell each assignment card whether it was answered),
 * so slicing locally keeps the table readable without a second round trip per page.
 */
export function MySubmissionsPanel({ submissions }: { submissions: Submission[] }) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  const totalPages = Math.ceil(submissions.length / pageSize);

  // A submission arriving while on the last page can shrink the page count under the
  // current page; clamp rather than render a blank table.
  const currentPage = Math.min(page, Math.max(totalPages, 1));

  const visible = useMemo(
    () => submissions.slice((currentPage - 1) * pageSize, currentPage * pageSize),
    [submissions, currentPage, pageSize],
  );

  const meta = {
    total: submissions.length,
    page: currentPage,
    pageSize,
    totalPages,
  };

  const changePageSize = (size: number) => {
    setPageSize(size);
    setPage(1);
  };

  return (
    <section>
      <SectionHeading
        icon="layers"
        title="My submissions"
        description="Everything you have handed in, with the marks and feedback returned so far."
      />

      {submissions.length === 0 ? (
        <EmptyState
          icon="send"
          title="You have not submitted anything yet"
          description="Open an assignment above and write your answer to make your first submission."
        />
      ) : (
        <>
        <div className="overflow-x-auto rounded-xl border border-border-subtle bg-surface shadow-sm">
          <table className={tableClass}>
            <thead className={tableHeadClass}>
              <tr>
                <th className="px-4 py-3">Assignment</th>
                <th className="px-4 py-3">Submitted</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Marks</th>
                <th className="px-4 py-3">Feedback</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {visible.map((submission) => {
                const badge = SUBMISSION_BADGE[submission.status];

                return (
                  <tr key={submission.id} className={tableRowClass}>
                    <td className="px-4 py-3 font-medium text-foreground">
                      {submission.assignmentTitle}
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
                      <Badge tone={badge.tone} icon={badge.icon}>
                        {submission.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 font-mono text-foreground">
                      {submission.marks ?? "—"}
                    </td>
                    <td className="whitespace-pre-wrap px-4 py-3 text-foreground">
                      {submission.feedback ?? (
                        <span className={mutedTextClass}>No feedback yet</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <Pagination
          meta={meta}
          onPageChange={setPage}
          onPageSizeChange={changePageSize}
          label="submissions"
        />
        </>
      )}
    </section>
  );
}
