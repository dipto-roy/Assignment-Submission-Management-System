import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AttachmentPanel } from "@/components/attachments/AttachmentPanel";
import {
  deleteAttachment,
  downloadAttachment,
  uploadAssignmentAttachment,
  uploadSubmissionAttachment,
} from "@/lib/api/attachments";
import type { Attachment } from "@/types";

vi.mock("@/lib/api/attachments", async (importOriginal) => {
  // formatFileSize is a pure helper; keeping the real one means the assertions below check
  // the size actually rendered rather than a stub.
  const actual = await importOriginal<typeof import("@/lib/api/attachments")>();

  return {
    ...actual,
    uploadAssignmentAttachment: vi.fn(),
    uploadSubmissionAttachment: vi.fn(),
    deleteAttachment: vi.fn(),
    downloadAttachment: vi.fn(),
  };
});

const existing: Attachment = {
  id: "at-1",
  fileName: "essay.pdf",
  contentType: "application/pdf",
  sizeBytes: 2048,
  uploadedById: "u-1",
  uploadedAt: "2026-08-11T10:00:00Z",
};

const uploaded: Attachment = { ...existing, id: "at-2", fileName: "notes.pdf", sizeBytes: 1024 };

const pdf = (name = "report.pdf") =>
  new File(["hello"], name, { type: "application/pdf" });

describe("AttachmentPanel", () => {
  beforeEach(() => {
    // Call counts leak between cases otherwise, so a "was not called" assertion would pick up
    // an earlier test's upload.
    vi.clearAllMocks();
    vi.mocked(uploadSubmissionAttachment).mockResolvedValue(uploaded);
    vi.mocked(uploadAssignmentAttachment).mockResolvedValue(uploaded);
    vi.mocked(deleteAttachment).mockResolvedValue(undefined);
    vi.mocked(downloadAttachment).mockResolvedValue(undefined);
  });

  it("lists attached files with a readable size", () => {
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[existing]} />);

    expect(screen.getByRole("button", { name: "essay.pdf" })).toBeInTheDocument();
    expect(screen.getByText("2.0 KB")).toBeInTheDocument();
  });

  it("shows an empty state when nothing is attached", () => {
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} />);

    expect(screen.getByText("No files attached.")).toBeInTheDocument();
  });

  it("hides the picker and remove controls for a read-only viewer", () => {
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[existing]} />);

    expect(screen.queryByLabelText("Attach a file")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Remove/ })).not.toBeInTheDocument();
  });

  it("uploads through the submission endpoint and appends the result", async () => {
    const user = userEvent.setup();
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} canModify />);

    await user.upload(screen.getByLabelText("Attach a file"), pdf());

    await waitFor(() => expect(uploadSubmissionAttachment).toHaveBeenCalledWith("s-1", expect.any(File)));
    expect(await screen.findByRole("button", { name: "notes.pdf" })).toBeInTheDocument();
  });

  it("uploads through the assignment endpoint when the owner is an assignment", async () => {
    const user = userEvent.setup();
    render(<AttachmentPanel owner="assignment" ownerId="a-1" attachments={[]} canModify />);

    await user.upload(screen.getByLabelText("Attach a file"), pdf());

    await waitFor(() => expect(uploadAssignmentAttachment).toHaveBeenCalledWith("a-1", expect.any(File)));
    expect(uploadSubmissionAttachment).not.toHaveBeenCalled();
  });

  it("rejects a disallowed file type without contacting the API", async () => {
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} canModify />);

    const executable = new File(["x"], "payload.exe", { type: "application/octet-stream" });

    // fireEvent rather than userEvent.upload: userEvent enforces the input's `accept` filter
    // and would drop this file before the component ever saw it. `accept` is only a hint in a
    // real browser — the user can switch to "All files", and drag-and-drop ignores it — so the
    // component's own check is what has to hold, and that is what is asserted here.
    fireEvent.change(screen.getByLabelText("Attach a file"), { target: { files: [executable] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("not an accepted file type");
    expect(uploadSubmissionAttachment).not.toHaveBeenCalled();
  });

  it("rejects a file over the size limit without contacting the API", async () => {
    const user = userEvent.setup();
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} canModify />);

    const huge = new File([new Uint8Array(11 * 1024 * 1024)], "big.pdf", { type: "application/pdf" });
    await user.upload(screen.getByLabelText("Attach a file"), huge);

    expect(await screen.findByRole("alert")).toHaveTextContent("larger than");
    expect(uploadSubmissionAttachment).not.toHaveBeenCalled();
  });

  it("surfaces a server-side rejection verbatim", async () => {
    const user = userEvent.setup();
    vi.mocked(uploadSubmissionAttachment).mockRejectedValue(
      new Error("The deadline for this assignment has passed; the submission is locked."),
    );
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} canModify />);

    await user.upload(screen.getByLabelText("Attach a file"), pdf());

    expect(await screen.findByRole("alert")).toHaveTextContent("the submission is locked");
  });

  it("downloads through the API rather than a bare link", async () => {
    const user = userEvent.setup();
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[existing]} />);

    await user.click(screen.getByRole("button", { name: "essay.pdf" }));

    // A plain href would omit the Authorization header the endpoint requires.
    await waitFor(() => expect(downloadAttachment).toHaveBeenCalledWith(existing));
  });

  it("removes a file and drops it from the list", async () => {
    const user = userEvent.setup();
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[existing]} canModify />);

    await user.click(screen.getByRole("button", { name: "Remove essay.pdf" }));

    await waitFor(() => expect(deleteAttachment).toHaveBeenCalledWith("at-1"));
    expect(await screen.findByText("No files attached.")).toBeInTheDocument();
  });

  it("keeps the file listed when removal fails", async () => {
    const user = userEvent.setup();
    vi.mocked(deleteAttachment).mockRejectedValue(new Error("Network error."));
    render(<AttachmentPanel owner="submission" ownerId="s-1" attachments={[existing]} canModify />);

    await user.click(screen.getByRole("button", { name: "Remove essay.pdf" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error.");
    expect(screen.getByRole("button", { name: "essay.pdf" })).toBeInTheDocument();
  });

  it("notifies the parent when the attachment list changes", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <AttachmentPanel owner="submission" ownerId="s-1" attachments={[]} canModify onChange={onChange} />,
    );

    await user.upload(screen.getByLabelText("Attach a file"), pdf());

    await waitFor(() => expect(onChange).toHaveBeenCalledWith([uploaded]));
  });
});
