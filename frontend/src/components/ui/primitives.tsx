import type { ReactNode } from "react";
import { Icon, type IconName } from "@/components/ui/Icon";
import {
  BADGE_TONES,
  badgeBaseClass,
  mutedTextClass,
  sectionTitleClass,
  type BadgeTone,
} from "@/components/ui/styles";

export interface BadgeProps {
  tone?: BadgeTone;
  icon?: IconName;
  children: ReactNode;
}

/**
 * Status pill. The icon is what makes the status readable when colour is unavailable —
 * to a colour-blind user, or in a printout — so tone and icon always travel together.
 */
export function Badge({ tone = "neutral", icon, children }: BadgeProps) {
  return (
    <span className={`${badgeBaseClass} ${BADGE_TONES[tone]}`}>
      {icon && <Icon name={icon} size="sm" />}
      {children}
    </span>
  );
}

export interface PageHeaderProps {
  icon: IconName;
  title: string;
  description?: string;
  actions?: ReactNode;
}

/** Page-level heading band: icon plate, title, one line of context, optional actions. */
export function PageHeader({ icon, title, description, actions }: PageHeaderProps) {
  return (
    <header className="mb-8 flex flex-wrap items-start justify-between gap-4 border-b border-border-subtle pb-6">
      <div className="flex items-start gap-3">
        <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-soft text-primary-soft-foreground">
          <Icon name={icon} size="lg" />
        </span>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">{title}</h1>
          {description && <p className={`mt-1 ${mutedTextClass}`}>{description}</p>}
        </div>
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  );
}

export interface SectionHeadingProps {
  icon: IconName;
  title: string;
  description?: string;
  /** Rendered as a pill beside the title — a count, a filter summary, and so on. */
  meta?: ReactNode;
}

export function SectionHeading({ icon, title, description, meta }: SectionHeadingProps) {
  return (
    <div className="mb-4">
      <div className="flex flex-wrap items-center gap-2">
        <h2 className={sectionTitleClass}>
          <Icon name={icon} size="lg" className="text-primary" />
          {title}
        </h2>
        {meta}
      </div>
      {description && <p className={`mt-1 ${mutedTextClass}`}>{description}</p>}
    </div>
  );
}

export interface EmptyStateProps {
  icon: IconName;
  title: string;
  description?: string;
  action?: ReactNode;
}

/** Shown instead of a bare "nothing here" sentence, so an empty screen still looks finished. */
export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-xl border border-dashed border-border-strong bg-muted/40 px-6 py-10 text-center">
      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-surface text-foreground-subtle shadow-xs">
        <Icon name={icon} size="lg" />
      </span>
      <p className="text-sm font-medium text-foreground">{title}</p>
      {description && <p className={`max-w-sm ${mutedTextClass}`}>{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

const ALERT_TONE = {
  error: { wrapper: "border-danger/40 bg-danger-soft text-danger", icon: "alert-circle" },
  success: { wrapper: "border-success/40 bg-success-soft text-success", icon: "check-circle" },
  info: { wrapper: "border-info/40 bg-info-soft text-info", icon: "alert-circle" },
  warning: {
    wrapper: "border-accent/40 bg-accent-soft text-accent-soft-foreground",
    icon: "alert-triangle",
  },
} as const satisfies Record<string, { wrapper: string; icon: IconName }>;

export interface AlertProps {
  tone?: keyof typeof ALERT_TONE;
  children: ReactNode;
  className?: string;
}

/**
 * Inline message. `role` follows the tone: errors interrupt (`alert`), everything else is
 * announced politely (`status`) so a success note does not cut off the screen reader.
 */
export function Alert({ tone = "error", children, className = "" }: AlertProps) {
  const { wrapper, icon } = ALERT_TONE[tone];

  return (
    <p
      role={tone === "error" ? "alert" : "status"}
      className={`flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${wrapper} ${className}`}
    >
      <Icon name={icon} size="sm" className="mt-0.5" />
      <span className="min-w-0">{children}</span>
    </p>
  );
}

export interface LoadingLineProps {
  label?: string;
}

/** Consistent inline loading indicator — spinner plus a specific, non-generic label. */
export function LoadingLine({ label = "Loading…" }: LoadingLineProps) {
  return (
    <p role="status" className={`flex items-center gap-2 ${mutedTextClass}`}>
      <Icon name="refresh" size="sm" className="app-spin" />
      {label}
    </p>
  );
}

/** Skeleton block for first paint, so a loading page reserves its final height (CLS). */
export function Skeleton({ className = "" }: { className?: string }) {
  return <div aria-hidden="true" className={`animate-pulse rounded-lg bg-muted ${className}`} />;
}
