// Shared DTOs mirroring backend enums/entities (AssignmentSubmissionSystem.Domain / Application).
// Keep in sync with backend/src/Domain/Enums and Application DTOs as they land.

export type UserRole = "Admin" | "Teacher" | "Student";

export type AssignmentStatus = "Draft" | "Published";

export type SubmissionStatus = "Submitted" | "Late" | "Graded" | "Returned";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}

export interface UserSummary extends User {
  createdAt: string;
}

export interface CreateUserInput {
  name: string;
  email: string;
  password: string;
  role: UserRole;
  classId?: string | null;
}

export interface UpdateUserInput {
  name: string;
  email: string;
  role: UserRole;
}

export interface SchoolClass {
  id: string;
  name: string;
  section?: string | null;
}

export interface CreateClassInput {
  name: string;
  section?: string | null;
}

export interface TeacherRef {
  id: string;
  name: string;
  email: string;
}

export interface Subject {
  id: string;
  name: string;
  code: string;
  classId: string;
  className: string;
  teachers: TeacherRef[];
}

export interface CreateSubjectInput {
  name: string;
  code: string;
  classId: string;
}

export interface Assignment {
  id: string;
  title: string;
  description: string;
  deadline: string; // ISO 8601
  maxMarks: number;
  status: AssignmentStatus;
  subjectId: string;
  teacherId: string;
}

export interface Submission {
  id: string;
  assignmentId: string;
  studentId: string;
  content: string;
  status: SubmissionStatus;
  marks?: number;
  feedback?: string;
  submittedAt: string;
  updatedAt?: string;
}

// Consistent API envelope (see rules/patterns.md — API Response Format).
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}
