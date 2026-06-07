import * as React from "react";
import { cn } from "@/lib/cn";

export type TextareaProps = React.TextareaHTMLAttributes<HTMLTextAreaElement>;

/**
 * Textarea — multi-line sibling of <Input>. Same hairline border, brand-tinted
 * focus ring and dark-mode well tint; grows with `rows` and is vertically
 * resizable. Labels live above the field as plain <Label>s.
 */
export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, rows = 4, ...props }, ref) => {
    return (
      <textarea
        data-slot="textarea"
        rows={rows}
        className={cn(
          "w-full min-w-0 rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2",
          "text-sm shadow-xs outline-none resize-y",
          "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "selection:bg-[var(--color-primary)] selection:text-[var(--color-primary-foreground)]",
          "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          "md:text-sm",
          "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
          "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
          "aria-invalid:border-[var(--color-destructive)] aria-invalid:ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.2)]",
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);
Textarea.displayName = "Textarea";
