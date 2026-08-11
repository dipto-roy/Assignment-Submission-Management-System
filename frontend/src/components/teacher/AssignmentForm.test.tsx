import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AssignmentForm } from "@/components/teacher/AssignmentForm";
import { toDateTimeLocalValue } from "@/lib/datetime";
import type { Assignment, Subject } from "@/types";

const DAY_MS = 24 * 60 * 60 * 1000;

const subjects: Subject[] = [
  {
    id: "s-1",
    name: "Mathematics",
    code: "MATH101",
    classId: "c-1",
    className: "Class 10-A",
    teachers: [{ id: "t-1", name: "Tess Teacher", email: "teacher@lms.test" }],
  },
];

const existing: Assignment = {
  id: "a-1",
  title: "Chapter 4 problem set",
  description: "Answer every question.",
  deadline: new Date(Date.now() + 7 * DAY_MS).toISOString(),
  maxMarks: 100,
  status: "Draft",
  subjectId: "s-1",
  subjectName: "Mathematics",
  classId: "c-1",
  className: "Class 10-A",
  teacherId: "t-1",
  teacherName: "Tess Teacher",
  createdAt: "2026-08-01T00:00:00Z",
  updatedAt: null,
  attachments: [],
};

const futureDeadlineValue = toDateTimeLocalValue(new Date(Date.now() + 3 * DAY_MS).toISOString());
const pastDeadlineValue = toDateTimeLocalValue(new Date(Date.now() - DAY_MS).toISOString());

/** Fills every field of the create form; individual tests override one field to test its rule. */
const fillValidForm = async (overrides: { deadline?: string; maxMarks?: string } = {}) => {
  await userEvent.type(screen.getByLabelText("Title"), "Chapter 5 problem set");
  await userEvent.type(screen.getByLabelText("Description"), "Answer questions 1-10.");
  await userEvent.selectOptions(screen.getByLabelText("Subject"), "s-1");
  const maxMarks = overrides.maxMarks ?? "100";
  if (maxMarks !== "") {
    await userEvent.type(screen.getByLabelText("Max marks"), maxMarks);
  }

  const deadline = screen.getByLabelText("Deadline");
  await userEvent.clear(deadline);
  await userEvent.type(deadline, overrides.deadline ?? futureDeadlineValue);
};

describe("AssignmentForm validation", () => {
  it("requires a title", async () => {
    const onSubmit = vi.fn();
    render(<AssignmentForm subjects={subjects} onSubmit={onSubmit} />);

    await userEvent.click(screen.getByRole("button", { name: "Create assignment" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Title is required.");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("rejects a deadline in the past, mirroring the server validator", async () => {
    const onSubmit = vi.fn();
    render(<AssignmentForm subjects={subjects} onSubmit={onSubmit} />);

    await fillValidForm({ deadline: pastDeadlineValue });
    await userEvent.click(screen.getByRole("button", { name: "Create assignment" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Deadline must be in the future.");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("rejects a missing max marks value", async () => {
    const onSubmit = vi.fn();
    render(<AssignmentForm subjects={subjects} onSubmit={onSubmit} />);

    // jsdom enforces the input's own min={1} constraint, so an out-of-range value
    // never reaches the handler; the empty case exercises the same rule.
    await fillValidForm({ maxMarks: "" });
    await userEvent.click(screen.getByRole("button", { name: "Create assignment" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Max marks must be a whole number greater than 0.",
    );
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("submits an ISO deadline and numeric marks when the form is valid", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<AssignmentForm subjects={subjects} onSubmit={onSubmit} />);

    await fillValidForm();
    await userEvent.click(screen.getByRole("button", { name: "Create assignment" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith({
      title: "Chapter 5 problem set",
      description: "Answer questions 1-10.",
      deadline: new Date(futureDeadlineValue).toISOString(),
      maxMarks: 100,
      subjectId: "s-1",
    });
  });

  it("surfaces a server error and keeps the entered values", async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error("You are not assigned to this subject."));
    render(<AssignmentForm subjects={subjects} onSubmit={onSubmit} />);

    await fillValidForm();
    await userEvent.click(screen.getByRole("button", { name: "Create assignment" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "You are not assigned to this subject.",
    );
    expect(screen.getByLabelText("Title")).toHaveValue("Chapter 5 problem set");
  });
});

describe("AssignmentForm in edit mode", () => {
  it("prefills the assignment and locks the subject, which the API cannot change", () => {
    render(<AssignmentForm subjects={subjects} assignment={existing} onSubmit={vi.fn()} />);

    expect(screen.getByLabelText("Title")).toHaveValue("Chapter 4 problem set");
    expect(screen.getByLabelText("Max marks")).toHaveValue(100);
    expect(screen.getByLabelText("Subject")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Save changes" })).toBeInTheDocument();
  });

  it("keeps the values after saving so the editor can stay open", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<AssignmentForm subjects={subjects} assignment={existing} onSubmit={onSubmit} />);

    await userEvent.click(screen.getByRole("button", { name: "Save changes" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(screen.getByLabelText("Title")).toHaveValue("Chapter 4 problem set");
  });
});
