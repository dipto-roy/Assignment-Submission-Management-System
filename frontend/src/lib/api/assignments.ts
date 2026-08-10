import { apiFetch } from "@/lib/api/client";
import type { Assignment, CreateAssignmentInput, UpdateAssignmentInput } from "@/types";

/**
 * Assignment endpoints (plan §4). GET is role-filtered server-side:
 * Admin sees all, Teacher only their own subjects, Student only Published
 * assignments for their class.
 */
export const getAssignments = () => apiFetch<Assignment[]>("/assignments");

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
