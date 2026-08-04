import { apiFetch } from "@/lib/api/client";
import type {
  CreateClassInput,
  CreateSubjectInput,
  CreateUserInput,
  SchoolClass,
  Subject,
  UpdateUserInput,
  UserSummary,
} from "@/types";

// ---- Users ----
export const getUsers = () => apiFetch<UserSummary[]>("/users");
export const createUser = (input: CreateUserInput) =>
  apiFetch<UserSummary>("/users", { method: "POST", body: input });
export const updateUser = (id: string, input: UpdateUserInput) =>
  apiFetch<UserSummary>(`/users/${id}`, { method: "PUT", body: input });
export const deleteUser = (id: string) => apiFetch<void>(`/users/${id}`, { method: "DELETE" });

// ---- Classes ----
export const getClasses = () => apiFetch<SchoolClass[]>("/classes");
export const createClass = (input: CreateClassInput) =>
  apiFetch<SchoolClass>("/classes", { method: "POST", body: input });
export const deleteClass = (id: string) => apiFetch<void>(`/classes/${id}`, { method: "DELETE" });

// ---- Subjects ----
export const getSubjects = () => apiFetch<Subject[]>("/subjects");
export const createSubject = (input: CreateSubjectInput) =>
  apiFetch<Subject>("/subjects", { method: "POST", body: input });
export const deleteSubject = (id: string) => apiFetch<void>(`/subjects/${id}`, { method: "DELETE" });
export const assignTeacher = (subjectId: string, teacherId: string) =>
  apiFetch<Subject>(`/subjects/${subjectId}/assign-teacher`, { method: "POST", body: { teacherId } });
