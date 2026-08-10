import { apiFetch } from "@/lib/api/client";
import { FULL_PAGE, toQueryString, type PageParams } from "@/lib/api/query";
import type {
  CreateClassInput,
  CreateSubjectInput,
  CreateUserInput,
  EnrolledStudent,
  SchoolClass,
  Subject,
  UpdateUserInput,
  UserSummary,
} from "@/types";

// ---- Users ----
export interface UserListParams extends PageParams {
  role?: UserSummary["role"];
  search?: string;
}

export const getUsers = (params: UserListParams = FULL_PAGE) =>
  apiFetch<UserSummary[]>(`/users${toQueryString({ ...FULL_PAGE, ...params })}`);
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

// ---- Enrollment ----
export const getClassStudents = (classId: string) =>
  apiFetch<EnrolledStudent[]>(`/classes/${classId}/students`);

/** Enrolling moves the student out of any class they were previously in (plan §11). */
export const enrollStudent = (classId: string, studentId: string) =>
  apiFetch<EnrolledStudent>(`/classes/${classId}/students`, { method: "POST", body: { studentId } });

export const unenrollStudent = (classId: string, studentId: string) =>
  apiFetch<void>(`/classes/${classId}/students/${studentId}`, { method: "DELETE" });

// ---- Subjects ----
export const getSubjects = () => apiFetch<Subject[]>("/subjects");
export const createSubject = (input: CreateSubjectInput) =>
  apiFetch<Subject>("/subjects", { method: "POST", body: input });
export const deleteSubject = (id: string) => apiFetch<void>(`/subjects/${id}`, { method: "DELETE" });
export const assignTeacher = (subjectId: string, teacherId: string) =>
  apiFetch<Subject>(`/subjects/${subjectId}/assign-teacher`, { method: "POST", body: { teacherId } });
