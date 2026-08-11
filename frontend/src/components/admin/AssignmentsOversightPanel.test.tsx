import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssignmentsOversightPanel } from "@/components/admin/AssignmentsOversightPanel";
import { getAssignments } from "@/lib/api/assignments";
import { getAssignmentSubmissions } from "@/lib/api/submissions";
import type { Assignment, SubmissionDetail } from "@/types";

vi.mock("@/lib/api/assignments", () => ({ getAssignments: vi.fn() }));
vi.mock("@/lib/api/submissions", () => ({ getAssignmentSubmissions: vi.fn() }));
vi.mock("@/lib/hooks/useClasses", () => ({
  useClasses: () => ({
    classes: [{ id: "c-1", name: "Class 10", section: "A" }],
    isLoading: false,
  }),
}));

const draft: Assignment = {
  id: "a-1",
  title: "Chapter 4 problem set",
  description: "Questions 1-10",
  deadline: "2026-08-20T00:00:00Z",
  maxMarks: 50,
  status: "Draft",
  subjectId: "s-1",
  subjectName: "Mathematics",
  classId: "c-1",
  className: "Class 10-A",
  teacherId: "t-1",
  teacherName: "Tina Teacher",
  createdAt: "2026-08-01T00:00:00Z",
  updatedAt: null,
  attachments: [],
};

const submission: SubmissionDetail = {
  id: "sub-1",
  assignmentId: "a-1",
  assignmentTitle: draft.title,
  assignmentDeadline: draft.deadline,
  assignmentMaxMarks: 50,
  studentId: "u-1",
  studentName: "Sam Student",
  studentEmail: "student@lms.test",
  content: "My answer",
  status: "Graded",
  marks: 45,
  feedback: "Good work",
  submittedAt: "2026-08-02T00:00:00Z",
  updatedAt: null,
  gradedAt: "2026-08-03T00:00:00Z",
  attachments: [],
};

describe("AssignmentsOversightPanel", () => {
  beforeEach(() => {
    vi.mocked(getAssignments).mockReset().mockResolvedValue([draft]);
    vi.mocked(getAssignmentSubmissions).mockReset().mockResolvedValue([submission]);
  });

  it("lists drafts as well as published assignments", async () => {
    render(<AssignmentsOversightPanel />);

    expect(await screen.findByText(draft.title)).toBeInTheDocument();
    // Scoped to the row: "Draft" is also the text of the status filter's option.
    const row = within(screen.getByRole("listitem"));
    expect(row.getByText("Draft")).toBeInTheDocument();
    expect(row.getByText(/Class 10-A/)).toBeInTheDocument();
  });

  it("loads submissions only once an assignment is expanded", async () => {
    render(<AssignmentsOversightPanel />);
    await screen.findByText(draft.title);

    expect(getAssignmentSubmissions).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "View submissions" }));

    expect(await screen.findByText("Sam Student")).toBeInTheDocument();
    expect(screen.getByText("Graded · 45/50")).toBeInTheDocument();
    expect(getAssignmentSubmissions).toHaveBeenCalledWith("a-1");
  });

  it("narrows the query by class and status instead of filtering client-side", async () => {
    render(<AssignmentsOversightPanel />);
    await screen.findByText(draft.title);

    await userEvent.selectOptions(screen.getByLabelText("Filter by status"), "Published");

    await waitFor(() =>
      expect(getAssignments).toHaveBeenLastCalledWith({
        classId: undefined,
        status: "Published",
        search: undefined,
      }),
    );

    await userEvent.selectOptions(screen.getByLabelText("Filter by class"), "c-1");

    await waitFor(() =>
      expect(getAssignments).toHaveBeenLastCalledWith({
        classId: "c-1",
        status: "Published",
        search: undefined,
      }),
    );
  });

  it("surfaces a failed load", async () => {
    vi.mocked(getAssignments).mockRejectedValue(new Error("Forbidden"));

    render(<AssignmentsOversightPanel />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Forbidden");
  });

  it("shows an empty state when no assignment matches the filters", async () => {
    vi.mocked(getAssignments).mockResolvedValue([]);

    render(<AssignmentsOversightPanel />);

    expect(
      await screen.findByText("No assignments match these filters."),
    ).toBeInTheDocument();
  });
});
