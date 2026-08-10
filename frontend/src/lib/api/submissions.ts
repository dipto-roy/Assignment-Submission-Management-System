import { apiFetch } from "@/lib/api/client";
import { FULL_PAGE, toQueryString, type PageParams } from "@/lib/api/query";
import type {
  GradeSubmissionInput,
  Submission,
  SubmissionDetail,
  SubmissionStatus,
} from "@/types";

export interface SubmissionListParams extends PageParams {
  status?: SubmissionStatus;
}

/** Teacher/Admin review view for one assignment (plan §4, business rule §7.5). */
export const getAssignmentSubmissions = (
  assignmentId: string,
  params: SubmissionListParams = FULL_PAGE,
) =>
  apiFetch<SubmissionDetail[]>(
    `/assignments/${assignmentId}/submissions${toQueryString({ ...FULL_PAGE, ...params })}`,
  );

/** Student's own submissions only (business rule §7.4). */
export const getMySubmissions = (params: SubmissionListParams = FULL_PAGE) =>
  apiFetch<Submission[]>(`/submissions/mine${toQueryString({ ...FULL_PAGE, ...params })}`);

export const createSubmission = (assignmentId: string, content: string) =>
  apiFetch<Submission>(`/assignments/${assignmentId}/submissions`, {
    method: "POST",
    body: { content },
  });

export const updateSubmission = (id: string, content: string) =>
  apiFetch<Submission>(`/submissions/${id}`, { method: "PUT", body: { content } });

/** Marks + feedback. Server rejects marks > assignment.maxMarks (business rule §7.6). */
export const gradeSubmission = (id: string, input: GradeSubmissionInput) =>
  apiFetch<SubmissionDetail>(`/submissions/${id}/grade`, { method: "PATCH", body: input });

export const setSubmissionStatus = (id: string, status: SubmissionStatus) =>
  apiFetch<SubmissionDetail>(`/submissions/${id}/status`, { method: "PATCH", body: { status } });
