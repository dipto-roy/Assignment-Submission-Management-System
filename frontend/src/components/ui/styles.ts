/**
 * Shared Tailwind class strings built on the semantic tokens in `globals.css`.
 *
 * Rules for anything added here:
 *  - colours come from tokens (`bg-surface`, `text-foreground-muted`), never raw hex and
 *    never a `dark:` colour variant — the tokens already flip themselves;
 *  - every interactive control clears the 44px minimum touch target;
 *  - every interactive control has hover, focus-visible, active, and disabled states.
 */

/** Shared by every control so focus and motion behave identically across the app. */
const INTERACTIVE_BASE =
  "inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-[background-color,border-color,color,box-shadow,transform] duration-150 ease-out cursor-pointer select-none " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring " +
  "active:translate-y-px disabled:pointer-events-none disabled:opacity-55 disabled:cursor-not-allowed";

/** 44px tall: the accessible minimum, and the height every input matches. */
const CONTROL_HEIGHT = "min-h-11 px-4 py-2 text-sm";

/** For dense rows (table cells, list actions) where a full-size button would crowd. */
const CONTROL_HEIGHT_SM = "min-h-9 px-3 py-1.5 text-sm";

export const primaryButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT} bg-primary text-on-primary shadow-sm hover:bg-primary-hover hover:shadow-md`;

export const accentButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT} bg-accent text-on-accent shadow-sm hover:bg-accent-hover hover:shadow-md`;

export const secondaryButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT} border border-border-strong bg-surface text-foreground shadow-xs hover:bg-muted hover:border-primary`;

/** The old text-link button, now a real target with a hover surface instead of an underline. */
export const subtleButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT_SM} text-primary hover:bg-primary-soft hover:text-primary-soft-foreground`;

export const dangerButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT_SM} text-danger hover:bg-danger-soft`;

export const dangerSolidButtonClass = `${INTERACTIVE_BASE} ${CONTROL_HEIGHT} bg-danger text-on-danger shadow-sm hover:bg-danger-hover hover:shadow-md`;

/** Square icon-only control. Always pair with an `aria-label`. */
export const iconButtonClass = `${INTERACTIVE_BASE} h-11 w-11 text-foreground-muted hover:bg-muted hover:text-foreground`;

export const inputClass =
  "min-h-11 w-full rounded-lg border border-border-strong bg-surface px-3 py-2 text-sm text-foreground shadow-xs transition-[border-color,box-shadow] duration-150 " +
  "placeholder:text-foreground-subtle hover:border-primary/60 " +
  "focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring focus-visible:border-primary " +
  "disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-70";

/** Same skin as `inputClass`, sized for a filter bar rather than a full-width form field. */
export const compactInputClass = `${inputClass} w-auto min-w-44`;

export const textareaClass = `${inputClass} min-h-24 resize-y leading-relaxed`;

export const cardClass =
  "rounded-xl border border-border-subtle bg-surface p-5 shadow-sm transition-shadow duration-200 hover:shadow-md";

/** A card that is not itself interactive, so it should not react to hover. */
export const staticCardClass =
  "rounded-xl border border-border-subtle bg-surface p-5 shadow-sm";

export const panelClass =
  "rounded-2xl border border-border-subtle bg-surface p-5 shadow-sm sm:p-6";

export const mutedTextClass = "text-sm text-foreground-muted";

export const subtleTextClass = "text-xs text-foreground-subtle";

export const fieldLabelClass = "flex flex-col gap-1.5 text-sm font-medium text-foreground";

export const sectionTitleClass =
  "flex items-center gap-2 text-lg font-semibold tracking-tight text-foreground";

export const tableClass = "w-full min-w-160 border-collapse text-left text-sm";

export const tableHeadClass =
  "border-b border-border-subtle text-xs font-semibold uppercase tracking-wide text-foreground-subtle";

export const tableRowClass = "align-top transition-colors duration-150 hover:bg-muted/60";

export const dividedListClass = "divide-y divide-border-subtle";

/** Tone → badge classes. Kept as a map so badges can never drift apart per component. */
export const BADGE_TONES = {
  neutral: "bg-muted text-foreground-muted",
  primary: "bg-primary-soft text-primary-soft-foreground",
  success: "bg-success-soft text-success",
  warning: "bg-accent-soft text-accent-soft-foreground",
  danger: "bg-danger-soft text-danger",
  info: "bg-info-soft text-info",
} as const;

export type BadgeTone = keyof typeof BADGE_TONES;

export const badgeBaseClass =
  "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium leading-none";
