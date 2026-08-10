"use client";

import { formatDateTime } from "@/lib/datetime";
import { mutedTextClass } from "@/components/ui/styles";
import type { Submission } from "@/types";

/** `GET /submissions/mine` view — status, marks, and teacher feedback in one place. */
export function MySubmissionsPanel({ submissions }: { submissions: Submission[] }) {
  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">My submissions</h2>

      {submissions.length === 0 ? (
        <p className={mutedTextClass}>You have not submitted anything yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-160 text-left text-sm">
            <thead className="border-b border-black/10 text-xs uppercase text-black/50 dark:border-white/15 dark:text-white/50">
              <tr>
                <th className="py-2 pr-3 font-medium">Assignment</th>
                <th className="py-2 pr-3 font-medium">Submitted</th>
                <th className="py-2 pr-3 font-medium">Status</th>
                <th className="py-2 pr-3 font-medium">Marks</th>
                <th className="py-2 font-medium">Feedback</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-black/10 dark:divide-white/10">
              {submissions.map((submission) => (
                <tr key={submission.id} className="align-top">
                  <td className="py-3 pr-3">{submission.assignmentTitle}</td>
                  <td className="py-3 pr-3">
                    <div>{formatDateTime(submission.submittedAt)}</div>
                    {submission.updatedAt && (
                      <div className="text-xs text-black/50 dark:text-white/50">
                        updated {formatDateTime(submission.updatedAt)}
                      </div>
                    )}
                  </td>
                  <td className="py-3 pr-3">{submission.status}</td>
                  <td className="py-3 pr-3">{submission.marks ?? "—"}</td>
                  <td className="whitespace-pre-wrap py-3">
                    {submission.feedback ?? <span className={mutedTextClass}>No feedback yet</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
