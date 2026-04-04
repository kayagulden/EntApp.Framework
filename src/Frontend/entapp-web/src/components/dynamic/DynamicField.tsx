"use client";

import type { FieldMetadata } from "@/types/dynamic";
import { cn } from "@/lib/utils";

interface DynamicFieldProps {
  field: FieldMetadata;
  value: unknown;
  onChange: (value: unknown) => void;
  error?: string;
  disabled?: boolean;
}

/**
 * Field type → component router.
 * Metadata'daki field tipine göre uygun input component'i render eder.
 */
export function DynamicField({
  field,
  value,
  onChange,
  error,
  disabled = false,
}: DynamicFieldProps) {
  const isDisabled = disabled || field.readOnly || !!field.computed;

  const baseInputClass = cn(
    "w-full rounded-lg border px-3 py-2 text-sm transition-colors duration-200",
    "bg-[var(--color-input-bg)] border-[var(--color-border)]",
    "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
    "focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500",
    isDisabled && "opacity-60 cursor-not-allowed",
    error && "border-red-500 focus:ring-red-500/40"
  );

  const renderField = () => {
    switch (field.type) {
      case "boolean":
        return (
          <label className="inline-flex items-center gap-3 cursor-pointer">
            <div className="relative">
              <input
                type="checkbox"
                checked={!!value}
                onChange={(e) => onChange(e.target.checked)}
                disabled={isDisabled}
                className="sr-only peer"
              />
              <div
                className={cn(
                  "w-11 h-6 rounded-full transition-colors duration-200",
                  "bg-slate-600 peer-checked:bg-indigo-500",
                  "peer-focus:ring-2 peer-focus:ring-indigo-500/40",
                  "after:content-[''] after:absolute after:top-[2px] after:start-[2px]",
                  "after:bg-white after:rounded-full after:h-5 after:w-5",
                  "after:transition-transform after:duration-200",
                  "peer-checked:after:translate-x-5"
                )}
              />
            </div>
            <span className="text-sm text-[var(--color-text-muted)]">
              {value ? "Evet" : "Hayır"}
            </span>
          </label>
        );

      case "enum":
        return (
          <select
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            disabled={isDisabled}
            className={baseInputClass}
          >
            <option value="">Seçiniz...</option>
            {field.options?.map((opt) => (
              <option key={opt} value={opt}>
                {opt}
              </option>
            ))}
          </select>
        );

      case "text":
      case "richtext":
        return (
          <textarea
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            disabled={isDisabled}
            rows={4}
            maxLength={field.maxLength || undefined}
            placeholder={field.label}
            className={cn(baseInputClass, "resize-y min-h-[80px]")}
          />
        );

      case "date":
        return (
          <input
            type="date"
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            disabled={isDisabled}
            className={baseInputClass}
          />
        );

      case "datetime":
        return (
          <input
            type="datetime-local"
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            disabled={isDisabled}
            className={baseInputClass}
          />
        );

      case "number":
        return (
          <input
            type="number"
            value={(value as number) ?? ""}
            onChange={(e) => onChange(e.target.valueAsNumber || 0)}
            disabled={isDisabled}
            min={field.min ?? undefined}
            max={field.max ?? undefined}
            step="1"
            placeholder={field.label}
            className={baseInputClass}
          />
        );

      case "decimal":
      case "money":
        return (
          <div className="relative">
            {field.type === "money" && (
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-[var(--color-text-muted)]">
                ₺
              </span>
            )}
            <input
              type="number"
              value={(value as number) ?? ""}
              onChange={(e) => onChange(e.target.valueAsNumber || 0)}
              disabled={isDisabled}
              min={field.min ?? undefined}
              max={field.max ?? undefined}
              step="0.01"
              placeholder={field.label}
              className={cn(
                baseInputClass,
                field.type === "money" && "pl-7"
              )}
            />
          </div>
        );

      // string, lookup, file — default: text input
      default:
        return (
          <input
            type="text"
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            disabled={isDisabled}
            maxLength={field.maxLength || undefined}
            placeholder={field.label}
            className={baseInputClass}
          />
        );
    }
  };

  return (
    <div className="space-y-1.5">
      <label className="block text-sm font-medium text-[var(--color-text)]">
        {field.label}
        {field.required && <span className="text-red-400 ml-1">*</span>}
        {field.computed && (
          <span className="text-xs text-[var(--color-text-muted)] ml-2">
            (hesaplanan)
          </span>
        )}
      </label>
      {renderField()}
      {error && (
        <p className="text-xs text-red-400 mt-1 animate-fade-in">{error}</p>
      )}
    </div>
  );
}
