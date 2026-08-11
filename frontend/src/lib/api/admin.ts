import { apiFetch, apiFetchPaged } from "@/lib/api/client";
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

/** Same endpoint as `getUsers`, keeping the page totals the list controls need. */
export const getUsersPage = (params: UserListParams) =>
  apiFetchPaged<UserSummary>(`/users${toQueryString({ ...params })}`);
export const createUser = (input: CreateUserInput) =>
  apiFetch<UserSummary>("/users", { method: "POST", body: input });
export const updateUser = (id: string, input: UpdateUserInput) =>
  apiFetch<UserSummary>(`/users/${id}`, { method: "PUT", body: input });
export const deleteUser = (id: string) => apiFetch<void>(`/users/${id}`, { method: "DELETE" });

// ---- Classes ----
/**
 * `FULL_PAGE` by default on purpose: this list fills the class pickers, and a picker that
 * silently stops at the server's default page size would hide classes a form needs to
 * offer. Callers rendering a paged table use `getClassesPage` instead.
 */
export const getClasses = (params: PageParams = FULL_PAGE) =>
  apiFetch<SchoolClass[]>(`/classes${toQueryString({ ...FULL_PAGE, ...params })}`);

export const getClassesPage = (params: PageParams) =>
  apiFetchPaged<SchoolClass>(`/classes${toQueryString({ ...params })}`);
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
/** Full list by default, for the same picker reason as `getClasses`. */
export const getSubjects = (params: PageParams = FULL_PAGE) =>
  apiFetch<Subject[]>(`/subjects${toQueryString({ ...FULL_PAGE, ...params })}`);

export const getSubjectsPage = (params: PageParams) =>
  apiFetchPaged<Subject>(`/subjects${toQueryString({ ...params })}`);
export const createSubject = (input: CreateSubjectInput) =>
  apiFetch<Subject>("/subjects", { method: "POST", body: input });
export const deleteSubject = (id: string) => apiFetch<void>(`/subjects/${id}`, { method: "DELETE" });
export const assignTeacher = (subjectId: string, teacherId: string) =>
  apiFetch<Subject>(`/subjects/${subjectId}/assign-teacher`, { method: "POST", body: { teacherId } });
