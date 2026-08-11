"use client";

import { useRef, useState } from "react";
import {
  deleteAttachment,
  downloadAttachment,
  formatFileSize,
  uploadAssignmentAttachment,
  uploadSubmissionAttachment,
} from "@/lib/api/attachments";
import { Button } from "@/components/ui/Button";
import { Icon, type IconName } from "@/components/ui/Icon";
import { Alert } from "@/components/ui/primitives";
import { mutedTextClass, subtleTextClass } from "@/components/ui/styles";
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

/** Extension → glyph, so a list of files is scannable by shape rather than by reading. */
const EXTENSION_ICON: Record<string, IconName> = {
  ".png": "eye",
  ".jpg": "eye",
  ".jpeg": "eye",
  ".gif": "eye",
  ".webp": "eye",
  ".zip": "layers",
};

const iconForFile = (fileName: string): IconName => {
  const extension = fileName.slice(fileName.lastIndexOf(".")).toLowerCase();
  return EXTENSION_ICON[extension] ?? "file-text";
};

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
    <section className="flex flex-col gap-2.5">
      <h4 className="flex items-center gap-1.5 text-sm font-semibold text-foreground">
        <Icon name="paperclip" size="sm" className="text-primary" />
        {label}
      </h4>

      {items.length === 0 ? (
        <p className={mutedTextClass}>No files attached.</p>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {items.map((attachment) => (
            <li
              key={attachment.id}
              className="flex flex-wrap items-center gap-2 rounded-lg border border-border-subtle bg-muted/50 py-1 pl-2 pr-1 text-sm"
            >
              {/* The whole file name is the download control — a bigger target than an icon. */}
              <button
                type="button"
                onClick={() => handleDownload(attachment)}
                className="inline-flex min-h-9 min-w-0 flex-1 cursor-pointer items-center gap-2 rounded-md px-1 text-left font-medium text-primary transition-colors duration-150 hover:text-primary-hover hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                <Icon name={iconForFile(attachment.fileName)} size="sm" />
                <span className="truncate">{attachment.fileName}</span>
                <Icon name="download" size="sm" className="opacity-60" />
              </button>

              <span className={`shrink-0 font-mono ${subtleTextClass}`}>
                {formatFileSize(attachment.sizeBytes)}
              </span>

              {canModify && (
                <Button
                  variant="danger"
                  icon="trash"
                  onClick={() => handleRemove(attachment)}
                  disabled={isBusy}
                  aria-label={`Remove ${attachment.fileName}`}
                >
                  Remove
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canModify && (
        <div className="flex flex-wrap items-center gap-3">
          {/* The native picker is styled through its file-selector button rather than being
              hidden behind a proxy control, which keeps the label association intact. */}
          <input
            ref={inputRef}
            type="file"
            onChange={handleFileSelected}
            disabled={isBusy}
            accept={ALLOWED_EXTENSIONS.join(",")}
            aria-label="Attach a file"
            className="max-w-full cursor-pointer text-sm text-foreground-muted
              file:mr-3 file:cursor-pointer file:rounded-lg file:border file:border-border-strong file:bg-surface
              file:px-3 file:py-2 file:text-sm file:font-medium file:text-foreground
              hover:file:border-primary hover:file:bg-muted disabled:cursor-not-allowed disabled:opacity-60"
          />

          {isBusy && (
            <span role="status" className={`inline-flex items-center gap-1.5 ${mutedTextClass}`}>
              <Icon name="refresh" size="sm" className="app-spin" />
              Working…
            </span>
          )}
        </div>
      )}

      {canModify && (
        <p className={subtleTextClass}>
          Up to {formatFileSize(MAX_FILE_SIZE_BYTES)}. Documents, spreadsheets, slides, images, or a
          zip.
        </p>
      )}

      {error && <Alert>{error}</Alert>}
    </section>
  );
}
