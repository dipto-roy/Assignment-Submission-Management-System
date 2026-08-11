import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SubmissionsReview } from "@/components/teacher/SubmissionsReview";
import {
  getAssignmentSubmissions,
  gradeSubmission,
  setSubmissionStatus,
} from "@/lib/api/submissions";
import type { SubmissionDetail } from "@/types";

vi.mock("@/lib/api/submissions", () => ({
  getAssignmentSubmissions: vi.fn(),
  gradeSubmission: vi.fn(),
  setSubmissionStatus: vi.fn(),
}));

const MAX_MARKS = 50;

const submission: SubmissionDetail = {
  id: "sub-1",
  assignmentId: "a-1",
  assignmentTitle: "Chapter 4 problem set",
  assignmentDeadline: "2026-08-20T00:00:00Z",
  assignmentMaxMarks: MAX_MARKS,
  studentId: "u-1",
  studentName: "Sam Student",
  studentEmail: "student@lms.test",
  content: "My answer",
  status: "Submitted",
  marks: null,
  feedback: null,
  submittedAt: "2026-08-02T00:00:00Z",
  updatedAt: null,
  gradedAt: null,
  attachments: [],
};

const renderReview = async () => {
  render(<SubmissionsReview assignmentId="a-1" maxMarks={MAX_MARKS} />);
  // The name appears in both the review table and the collapsed answers list.
  await screen.findAllByText("Sam Student");
};

describe("SubmissionsReview", () => {
  beforeEach(() => {
    vi.mocked(getAssignmentSubmissions).mockReset().mockResolvedValue([submission]);
    vi.mocked(gradeSubmission).mockReset();
    vi.mocked(setSubmissionStatus).mockReset();
  });

  it("blocks marks above the assignment maximum without calling the API", async () => {
    await renderReview();

    await userEvent.type(screen.getByLabelText(/marks for sam student/i), "60");
    await userEvent.click(screen.getByRole("button", { name: "Save grade" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      `Marks cannot exceed the assignment maximum of ${MAX_MARKS}.`,
    );
    expect(gradeSubmission).not.toHaveBeenCalled();
  });

  it("rejects an empty marks field", async () => {
    await renderReview();

    await userEvent.click(screen.getByRole("button", { name: "Save grade" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Marks must be a whole number.");
    expect(gradeSubmission).not.toHaveBeenCalled();
  });

  it("submits marks within range, sending null for empty feedback", async () => {
    vi.mocked(gradeSubmission).mockResolvedValue({ ...submission, marks: 45, status: "Graded" });
    await renderReview();

    await userEvent.type(screen.getByLabelText(/marks for sam student/i), "45");
    await userEvent.click(screen.getByRole("button", { name: "Save grade" }));

    await waitFor(() =>
      expect(gradeSubmission).toHaveBeenCalledWith("sub-1", { marks: 45, feedback: null }),
    );
  });

  it("sends feedback alongside the marks when provided", async () => {
    vi.mocked(gradeSubmission).mockResolvedValue({
      ...submission,
      marks: 45,
      feedback: "Good work",
      status: "Graded",
    });
    await renderReview();

    await userEvent.type(screen.getByLabelText(/marks for sam student/i), "45");
    await userEvent.type(screen.getByLabelText(/feedback for sam student/i), "Good work");
    await userEvent.click(screen.getByRole("button", { name: "Save grade" }));

    await waitFor(() =>
      expect(gradeSubmission).toHaveBeenCalledWith("sub-1", { marks: 45, feedback: "Good work" }),
    );
  });

  it("changes submission status through the API", async () => {
    vi.mocked(setSubmissionStatus).mockResolvedValue({ ...submission, status: "Late" });
    await renderReview();

    await userEvent.selectOptions(screen.getByLabelText(/status for sam student/i), "Late");

    await waitFor(() => expect(setSubmissionStatus).toHaveBeenCalledWith("sub-1", "Late"));
  });

  it("surfaces a server rejection when changing status", async () => {
    vi.mocked(setSubmissionStatus).mockRejectedValue(
      new Error("Grade the submission before setting its status to Returned."),
    );
    await renderReview();

    await userEvent.selectOptions(screen.getByLabelText(/status for sam student/i), "Returned");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Grade the submission before setting its status to Returned.",
    );
  });

  it("shows an empty state when nobody has submitted", async () => {
    vi.mocked(getAssignmentSubmissions).mockResolvedValue([]);

    render(<SubmissionsReview assignmentId="a-1" maxMarks={MAX_MARKS} />);

    expect(await screen.findByText("No submissions yet.")).toBeInTheDocument();
  });
});
