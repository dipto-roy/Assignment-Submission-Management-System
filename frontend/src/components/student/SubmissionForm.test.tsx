import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SubmissionForm } from "@/components/student/SubmissionForm";
import { createSubmission, updateSubmission } from "@/lib/api/submissions";
import type { Assignment, Submission } from "@/types";

vi.mock("@/lib/api/submissions", () => ({
  createSubmission: vi.fn(),
  updateSubmission: vi.fn(),
}));

const HOUR_MS = 60 * 60 * 1000;

const assignmentDue = (deadline: Date): Assignment => ({
  id: "a-1",
  title: "Chapter 4 problem set",
  description: "Answer every question.",
  deadline: deadline.toISOString(),
  maxMarks: 100,
  status: "Published",
  subjectId: "s-1",
  subjectName: "Mathematics",
  classId: "c-1",
  className: "Class 10-A",
  teacherId: "t-1",
  teacherName: "Tess Teacher",
  createdAt: "2026-08-01T00:00:00Z",
  updatedAt: null,
});

const existingSubmission: Submission = {
  id: "sub-1",
  assignmentId: "a-1",
  assignmentTitle: "Chapter 4 problem set",
  assignmentDeadline: "2026-08-20T00:00:00Z",
  studentId: "u-1",
  content: "My first attempt",
  status: "Submitted",
  marks: null,
  feedback: null,
  submittedAt: "2026-08-02T00:00:00Z",
  updatedAt: null,
  gradedAt: null,
};

const openAssignment = assignmentDue(new Date(Date.now() + 24 * HOUR_MS));
const closedAssignment = assignmentDue(new Date(Date.now() - HOUR_MS));

describe("SubmissionForm deadline lock", () => {
  beforeEach(() => {
    vi.mocked(createSubmission).mockReset();
    vi.mocked(updateSubmission).mockReset();
  });

  it("hides the editor and explains why once the deadline has passed", () => {
    render(<SubmissionForm assignment={closedAssignment} onSaved={vi.fn()} />);

    expect(screen.getByText(/deadline has passed/i)).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the locked submission read-only after the deadline", () => {
    render(
      <SubmissionForm
        assignment={closedAssignment}
        submission={existingSubmission}
        onSaved={vi.fn()}
      />,
    );

    expect(screen.getByText(/your submission is locked/i)).toBeInTheDocument();
    expect(screen.getByText("My first attempt")).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
  });

  it("allows editing while the deadline is in the future", () => {
    render(<SubmissionForm assignment={openAssignment} onSaved={vi.fn()} />);

    expect(screen.getByRole("textbox")).toBeEnabled();
    expect(screen.getByRole("button", { name: "Submit" })).toBeInTheDocument();
  });
});

describe("SubmissionForm saving", () => {
  beforeEach(() => {
    vi.mocked(createSubmission).mockReset();
    vi.mocked(updateSubmission).mockReset();
  });

  it("rejects an empty answer without calling the API", async () => {
    render(<SubmissionForm assignment={openAssignment} onSaved={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Submit" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Your answer cannot be empty.");
    expect(createSubmission).not.toHaveBeenCalled();
  });

  it("creates a submission when the student has not submitted yet", async () => {
    const saved = { ...existingSubmission, content: "My answer" };
    vi.mocked(createSubmission).mockResolvedValue(saved);
    const onSaved = vi.fn();

    render(<SubmissionForm assignment={openAssignment} onSaved={onSaved} />);
    await userEvent.type(screen.getByRole("textbox"), "My answer");
    await userEvent.click(screen.getByRole("button", { name: "Submit" }));

    await waitFor(() => expect(createSubmission).toHaveBeenCalledWith("a-1", "My answer"));
    expect(onSaved).toHaveBeenCalledWith(saved);
  });

  it("updates the existing submission instead of creating a second one", async () => {
    const saved = { ...existingSubmission, content: "Revised answer" };
    vi.mocked(updateSubmission).mockResolvedValue(saved);

    render(
      <SubmissionForm
        assignment={openAssignment}
        submission={existingSubmission}
        onSaved={vi.fn()}
      />,
    );

    await userEvent.clear(screen.getByRole("textbox"));
    await userEvent.type(screen.getByRole("textbox"), "Revised answer");
    await userEvent.click(screen.getByRole("button", { name: "Update submission" }));

    await waitFor(() => expect(updateSubmission).toHaveBeenCalledWith("sub-1", "Revised answer"));
    expect(createSubmission).not.toHaveBeenCalled();
  });

  it("surfaces the server error when the API rejects the write", async () => {
    vi.mocked(createSubmission).mockRejectedValue(
      new Error("The deadline for this assignment has passed."),
    );

    render(<SubmissionForm assignment={openAssignment} onSaved={vi.fn()} />);
    await userEvent.type(screen.getByRole("textbox"), "Late answer");
    await userEvent.click(screen.getByRole("button", { name: "Submit" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The deadline for this assignment has passed.",
    );
  });
});
