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

export interface EnrolledStudent {
  id: string;
  name: string;
  email: string;
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
  subjectName: string;
  classId: string;
  className: string;
  teacherId: string;
  teacherName: string;
  createdAt: string;
  updatedAt?: string | null;
  /** Brief, spec or rubric files the teacher attached. */
  attachments: Attachment[];
}

export interface CreateAssignmentInput {
  title: string;
  description: string;
  deadline: string; // ISO 8601 (UTC)
  maxMarks: number;
  subjectId: string;
}

export type UpdateAssignmentInput = Omit<CreateAssignmentInput, "subjectId">;

export interface Submission {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  assignmentDeadline: string;
  studentId: string;
  content: string;
  status: SubmissionStatus;
  marks?: number | null;
  feedback?: string | null;
  submittedAt: string;
  updatedAt?: string | null;
  gradedAt?: string | null;
  /** Files the student uploaded alongside the text answer. */
  attachments: Attachment[];
}

/** Teacher/Admin review view — adds student identity and the assignment's mark ceiling. */
export interface SubmissionDetail extends Submission {
  assignmentMaxMarks: number;
  studentName: string;
  studentEmail: string;
}

export interface GradeSubmissionInput {
  marks: number;
  feedback?: string | null;
}

/** A file attached to an assignment (teacher's brief) or a submission (student's work). */
export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedById: string;
  uploadedAt: string;
}

export type NotificationType =
  | "AssignmentPublished"
  | "SubmissionReceived"
  | "SubmissionGraded"
  | "DeadlineApproaching";

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  assignmentId?: string | null;
  submissionId?: string | null;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
}

export interface UnreadCount {
  unread: number;
}

// Consistent API envelope (see rules/patterns.md — API Response Format).
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}
