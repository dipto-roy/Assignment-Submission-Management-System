import type { SVGProps } from "react";

/**
 * One inline icon family for the whole app.
 *
 * Outline geometry on a 24×24 grid with a single 1.75 stroke width, drawn in
 * `currentColor` so an icon always matches the text it sits beside — including in dark
 * mode. Inlined rather than pulled from a package: the app needs ~30 glyphs, and shipping
 * them as data avoids both a runtime dependency and a network request.
 */

export type IconName =
  | "alert-circle"
  | "alert-triangle"
  | "arrow-left"
  | "bell"
  | "book-open"
  | "calendar-clock"
  | "check"
  | "check-circle"
  | "chevron-down"
  | "clock"
  | "download"
  | "edit"
  | "eye"
  | "eye-off"
  | "file-text"
  | "filter"
  | "graduation-cap"
  | "inbox"
  | "layers"
  | "log-in"
  | "log-out"
  | "mail"
  | "paperclip"
  | "plus"
  | "refresh"
  | "search"
  | "send"
  | "shield"
  | "sparkles"
  | "trash"
  | "upload"
  | "user"
  | "user-minus"
  | "user-plus"
  | "users"
  | "x";

/** Path data only — the wrapper below owns sizing, stroke, and accessibility. */
const ICON_PATHS: Record<IconName, string> = {
  "alert-circle": "M12 8v5M12 16.5h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z",
  "alert-triangle":
    "M10.3 3.9 1.9 18a2 2 0 0 0 1.7 3h16.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0ZM12 9v4M12 17h.01",
  "arrow-left": "M19 12H5M12 19l-7-7 7-7",
  bell: "M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 0 1-3.4 0",
  "book-open": "M2 4h6a4 4 0 0 1 4 4v12a3 3 0 0 0-3-3H2ZM22 4h-6a4 4 0 0 0-4 4v12a3 3 0 0 1 3-3h7Z",
  "calendar-clock":
    "M8 2v4M16 2v4M3 10h18M21 14V6a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h6M17.5 17.5 16 16.6V14M21 16.5a5 5 0 1 1-10 0 5 5 0 0 1 10 0Z",
  check: "m20 6-11 11-5-5",
  "check-circle": "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-12.5.5 2.5 2.5 5-5",
  "chevron-down": "m6 9 6 6 6-6",
  clock: "M12 7v5l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z",
  download: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3",
  edit: "M11 4H5a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2h13a2 2 0 0 0 2-2v-6M18.4 2.6a2 2 0 0 1 2.8 2.8L12 14.6 8 15.6l1-4Z",
  eye: "M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7Zm13 0a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z",
  "eye-off":
    "M10.7 5.1A9.9 9.9 0 0 1 12 5c6.4 0 10 7 10 7a17.7 17.7 0 0 1-3 4M6.6 6.6A17.6 17.6 0 0 0 2 12s3.6 7 10 7a9.7 9.7 0 0 0 4.9-1.3M3 3l18 18M9.9 9.9a3 3 0 0 0 4.2 4.2",
  "file-text": "M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8ZM14 3v5h5M9 13h6M9 17h4",
  filter: "M3 5h18l-7 8v6l-4 2v-8Z",
  "graduation-cap": "m12 3 10 5-10 5L2 8ZM6 10.5V16c0 1.7 2.7 3 6 3s6-1.3 6-3v-5.5M21 9v5",
  inbox: "M3 13h4l2 3h6l2-3h4M5 5h14l3 8v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4Z",
  layers: "m12 2 9 5-9 5-9-5ZM3 12l9 5 9-5M3 17l9 5 9-5",
  "log-in": "M15 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3M10 17l5-5-5-5M15 12H3",
  "log-out": "M9 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h3M16 17l5-5-5-5M21 12H9",
  mail: "M4 5h16a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1Zm-.6.6 8.6 7 8.6-7",
  paperclip:
    "M21 11.5 12.5 20a5.5 5.5 0 0 1-7.8-7.8l8.5-8.5a3.7 3.7 0 1 1 5.2 5.2l-8.5 8.5a1.8 1.8 0 1 1-2.6-2.6l7.9-7.8",
  plus: "M12 5v14M5 12h14",
  refresh: "M21 12a9 9 0 1 1-2.6-6.3M21 3v6h-6",
  search: "M17 17l4 4M19 11a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  send: "M22 2 11 13M22 2l-7 20-4-9-9-4Z",
  shield: "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z",
  sparkles: "m12 3 1.9 5.1L19 10l-5.1 1.9L12 17l-1.9-5.1L5 10l5.1-1.9ZM19 15l.9 2.1L22 18l-2.1.9L19 21l-.9-2.1L16 18l2.1-.9Z",
  trash: "M3 6h18M8 6V4h8v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6M10 11v6M14 11v6",
  upload: "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12",
  user: "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z",
  "user-minus": "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM16 11h6",
  "user-plus":
    "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM19 8v6M22 11h-6",
  users:
    "M17 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8",
  x: "M18 6 6 18M6 6l12 12",
};

/** Icon sizes are tokens, not arbitrary numbers, so glyph rhythm stays consistent. */
export const ICON_SIZE = {
  sm: 16,
  md: 18,
  lg: 22,
  xl: 28,
} as const;

export type IconSize = keyof typeof ICON_SIZE;

export interface IconProps extends Omit<SVGProps<SVGSVGElement>, "name"> {
  name: IconName;
  size?: IconSize;
  /**
   * Only pass this when the icon is the sole carrier of meaning. Icons that sit next to
   * a visible text label must stay decorative, or a screen reader reads the label twice.
   */
  title?: string;
}

export function Icon({ name, size = "md", title, className = "", ...rest }: IconProps) {
  const pixels = ICON_SIZE[size];

  return (
    <svg
      viewBox="0 0 24 24"
      width={pixels}
      height={pixels}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.75}
      strokeLinecap="round"
      strokeLinejoin="round"
      role={title ? "img" : undefined}
      aria-label={title}
      aria-hidden={title ? undefined : true}
      focusable="false"
      className={`shrink-0 ${className}`}
      {...rest}
    >
      {title && <title>{title}</title>}
      <path d={ICON_PATHS[name]} />
    </svg>
  );
}
