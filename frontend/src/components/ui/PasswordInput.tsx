"use client";

import { useId, useState, type InputHTMLAttributes } from "react";
import { Icon, type IconName } from "@/components/ui/Icon";
import { inputClass } from "@/components/ui/styles";

/**
 * A password field with a reveal toggle.
 *
 * The toggle flips the input's `type` rather than rendering a second field, so the value,
 * caret position, and form state survive the switch. It is a `button` with an
 * `aria-label` and `aria-pressed` (not a checkbox) because the visible state change is the
 * field beside it, and it stays out of nothing a password manager needs.
 */

export interface PasswordInputProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  /** Optional leading glyph, matching the icon-inside-field pattern used by other inputs. */
  leadingIcon?: IconName;
}

export function PasswordInput({
  leadingIcon,
  className = "",
  ...rest
}: PasswordInputProps) {
  const [isVisible, setIsVisible] = useState(false);
  const inputId = useId();

  return (
    <span className="relative block">
      {leadingIcon && (
        <Icon
          name={leadingIcon}
          size="sm"
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-foreground-subtle"
        />
      )}

      <input
        id={inputId}
        type={isVisible ? "text" : "password"}
        className={`${inputClass} ${leadingIcon ? "pl-9" : ""} pr-12 ${className}`}
        {...rest}
      />

      <button
        type="button"
        onClick={() => setIsVisible((visible) => !visible)}
        aria-label={isVisible ? "Hide password" : "Show password"}
        aria-pressed={isVisible}
        aria-controls={inputId}
        className="absolute right-0 top-1/2 flex h-11 w-11 -translate-y-1/2 cursor-pointer items-center justify-center rounded-r-lg text-foreground-subtle transition-colors duration-150 hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring"
      >
        <Icon name={isVisible ? "eye-off" : "eye"} size="sm" />
      </button>
    </span>
  );
}
