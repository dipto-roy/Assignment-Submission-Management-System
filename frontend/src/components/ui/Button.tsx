import type { ButtonHTMLAttributes } from "react";
import { Icon, type IconName } from "@/components/ui/Icon";
import {
  accentButtonClass,
  dangerButtonClass,
  dangerSolidButtonClass,
  iconButtonClass,
  primaryButtonClass,
  secondaryButtonClass,
  subtleButtonClass,
} from "@/components/ui/styles";

const VARIANT_CLASS = {
  primary: primaryButtonClass,
  accent: accentButtonClass,
  secondary: secondaryButtonClass,
  subtle: subtleButtonClass,
  danger: dangerButtonClass,
  dangerSolid: dangerSolidButtonClass,
} as const;

export type ButtonVariant = keyof typeof VARIANT_CLASS;

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  /** Leading glyph. Decorative — the button's text is what a screen reader announces. */
  icon?: IconName;
  /**
   * Swaps the icon for a spinner and disables the button. The label stays put so the
   * button never changes width mid-action.
   */
  isBusy?: boolean;
}

/**
 * The app's only button. Variants map to intent (primary action, secondary, destructive)
 * rather than to colour, so a screen's meaning survives a palette change.
 */
export function Button({
  variant = "primary",
  icon,
  isBusy = false,
  disabled,
  className = "",
  children,
  type = "button",
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || isBusy}
      aria-busy={isBusy || undefined}
      className={`${VARIANT_CLASS[variant]} ${className}`}
      {...rest}
    >
      {isBusy ? (
        <Icon name="refresh" size="sm" className="app-spin" />
      ) : (
        icon && <Icon name={icon} size="sm" />
      )}
      {children}
    </button>
  );
}

export interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon: IconName;
  /** Required: an icon-only control has no visible text to name it. */
  label: string;
}

/** Icon-only control at the full 44×44 touch target. */
export function IconButton({ icon, label, className = "", type = "button", ...rest }: IconButtonProps) {
  return (
    <button type={type} aria-label={label} title={label} className={`${iconButtonClass} ${className}`} {...rest}>
      <Icon name={icon} size="lg" />
    </button>
  );
}
