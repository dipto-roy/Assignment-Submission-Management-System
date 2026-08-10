import { apiFetch } from "@/lib/api/client";
import { FULL_PAGE, toQueryString, type PageParams } from "@/lib/api/query";
import type { Assignment, AssignmentStatus, CreateAssignmentInput, UpdateAssignmentInput } from "@/types";

export interface AssignmentListParams extends PageParams {
  status?: AssignmentStatus;
  subjectId?: string;
  classId?: string;
  search?: string;
}

/**
 * Assignment endpoints (plan §4). GET is role-filtered server-side:
 * Admin sees all, Teacher only their own subjects, Student only Published
 * assignments for their class.
 */
export const getAssignments = (params: AssignmentListParams = FULL_PAGE) =>
  apiFetch<Assignment[]>(`/assignments${toQueryString({ ...FULL_PAGE, ...params })}`);

export const getAssignment = (id: string) => apiFetch<Assignment>(`/assignments/${id}`);

export const createAssignment = (input: CreateAssignmentInput) =>
  apiFetch<Assignment>("/assignments", { method: "POST", body: input });

export const updateAssignment = (id: string, input: UpdateAssignmentInput) =>
  apiFetch<Assignment>(`/assignments/${id}`, { method: "PUT", body: input });

export const deleteAssignment = (id: string) =>
  apiFetch<void>(`/assignments/${id}`, { method: "DELETE" });

/** `publish: true` → Published, `false` → back to Draft. */
export const setAssignmentPublishState = (id: string, publish: boolean) =>
  apiFetch<Assignment>(`/assignments/${id}/publish`, { method: "PATCH", body: { publish } });
