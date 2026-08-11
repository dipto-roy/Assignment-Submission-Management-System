import { apiDownloadBlob, apiFetch, apiUpload } from "@/lib/api/client";
import type { Attachment } from "@/types";

export const uploadAssignmentAttachment = (assignmentId: string, file: File) =>
  apiUpload<Attachment>(`/assignments/${assignmentId}/attachments`, file);

export const uploadSubmissionAttachment = (submissionId: string, file: File) =>
  apiUpload<Attachment>(`/submissions/${submissionId}/attachments`, file);

export const deleteAttachment = (id: string) =>
  apiFetch<void>(`/attachments/${id}`, { method: "DELETE" });

/**
 * Pulls a protected file and hands it to the browser as a save prompt.
 *
 * The object URL is revoked afterwards; without that, every download would pin its blob in
 * memory for the lifetime of the page.
 */
export async function downloadAttachment(attachment: Attachment): Promise<void> {
  const blob = await apiDownloadBlob(`/attachments/${attachment.id}/download`);
  const objectUrl = URL.createObjectURL(blob);

  try {
    const link = document.createElement("a");
    link.href = objectUrl;
    link.download = attachment.fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

/** Human-readable size for the attachment list. */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
