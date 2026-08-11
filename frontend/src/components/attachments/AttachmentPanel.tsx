"use client";

import { useRef, useState } from "react";
import {
  deleteAttachment,
  downloadAttachment,
  formatFileSize,
  uploadAssignmentAttachment,
  uploadSubmissionAttachment,
} from "@/lib/api/attachments";
import { dangerButtonClass, mutedTextClass, subtleButtonClass } from "@/components/ui/styles";
import type { Attachment } from "@/types";

/**
 * Mirrors AttachmentRules on the backend. Duplicated deliberately: this copy gives immediate
 * feedback and avoids a pointless round trip, while the server's copy is the one that decides.
 */
const ALLOWED_EXTENSIONS = [
  ".pdf", ".doc", ".docx", ".odt", ".rtf", ".txt", ".md",
  ".xls", ".xlsx", ".ods", ".csv",
  ".ppt", ".pptx", ".odp",
  ".png", ".jpg", ".jpeg", ".gif", ".webp",
  ".zip",
];

/** Matches the default Storage:MaxFileSizeBytes (10 MB). */
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

export interface AttachmentPanelProps {
  /** Which owner the files hang off — decides the upload endpoint and the permission rules. */
  owner: "assignment" | "submission";
  ownerId: string;
  attachments: Attachment[];
  /** Hides the picker and the remove buttons for viewers who may only read. */
  canModify?: boolean;
  onChange?: (attachments: Attachment[]) => void;
  label?: string;
}

/**
 * Lists attached files and, for users who own the record, lets them add or remove one.
 *
 * Downloads go through the API rather than a direct link, because the endpoint requires an
 * Authorization header that a plain navigation would not send.
 */
export function AttachmentPanel({
  owner,
  ownerId,
  attachments,
  canModify = false,
  onChange,
  label = "Attachments",
}: AttachmentPanelProps) {
  const [items, setItems] = useState<Attachment[]>(attachments);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const publish = (next: Attachment[]) => {
    setItems(next);
    onChange?.(next);
  };

  const validate = (file: File): string | null => {
    if (file.size === 0) return `"${file.name}" is empty.`;
    if (file.size > MAX_FILE_SIZE_BYTES) {
      return `"${file.name}" is larger than the ${formatFileSize(MAX_FILE_SIZE_BYTES)} limit.`;
    }

    const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      return `"${file.name}" is not an accepted file type.`;
    }

    return null;
  };

  const handleFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    // Cleared straight away so re-picking the same file still fires a change event.
    event.target.value = "";

    const validationError = validate(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setIsBusy(true);

    try {
      const uploaded =
        owner === "assignment"
          ? await uploadAssignmentAttachment(ownerId, file)
          : await uploadSubmissionAttachment(ownerId, file);

      publish([...items, uploaded]);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Upload failed.");
    } finally {
      setIsBusy(false);
    }
  };

  const handleDownload = async (attachment: Attachment) => {
    setError(null);

    try {
      await downloadAttachment(attachment);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Download failed.");
    }
  };

  const handleRemove = async (attachment: Attachment) => {
    setError(null);
    setIsBusy(true);

    try {
      await deleteAttachment(attachment.id);
      publish(items.filter((item) => item.id !== attachment.id));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not remove the file.");
    } finally {
      setIsBusy(false);
    }
  };

  return (
    <section className="flex flex-col gap-2">
      <h4 className="text-sm font-medium">{label}</h4>

      {items.length === 0 ? (
        <p className={mutedTextClass}>No files attached.</p>
      ) : (
        <ul className="flex flex-col gap-1">
          {items.map((attachment) => (
            <li key={attachment.id} className="flex flex-wrap items-center gap-2 text-sm">
              <button
                type="button"
                onClick={() => handleDownload(attachment)}
                className={subtleButtonClass}
              >
                {attachment.fileName}
              </button>
              <span className={mutedTextClass}>{formatFileSize(attachment.sizeBytes)}</span>
              {canModify && (
                <button
                  type="button"
                  onClick={() => handleRemove(attachment)}
                  disabled={isBusy}
                  className={dangerButtonClass}
                  aria-label={`Remove ${attachment.fileName}`}
                >
                  Remove
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canModify && (
        <div className="flex items-center gap-2">
          <input
            ref={inputRef}
            type="file"
            onChange={handleFileSelected}
            disabled={isBusy}
            accept={ALLOWED_EXTENSIONS.join(",")}
            aria-label="Attach a file"
            className="text-sm"
          />
          {isBusy && <span className={mutedTextClass}>Working…</span>}
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
    </section>
  );
}
