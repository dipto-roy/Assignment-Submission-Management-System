import { apiFetch } from "@/lib/api/client";
import type {
  GradeSubmissionInput,
  Submission,
  SubmissionDetail,
  SubmissionStatus,
} from "@/types";

/** Teacher/Admin review view for one assignment (plan §4, business rule §7.5). */
export const getAssignmentSubmissions = (assignmentId: string) =>
  apiFetch<SubmissionDetail[]>(`/assignments/${assignmentId}/submissions`);

/** Student's own submissions only (business rule §7.4). */
export const getMySubmissions = () => apiFetch<Submission[]>("/submissions/mine");

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
