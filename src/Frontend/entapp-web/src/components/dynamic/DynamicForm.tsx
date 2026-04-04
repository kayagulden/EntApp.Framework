"use client";

import { useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { X, Save, Loader2 } from "lucide-react";
import type { FieldMetadata } from "@/types/dynamic";
import { metadataToZodSchema, metadataToDefaults } from "@/lib/schema-to-zod";
import { DynamicField } from "./DynamicField";
import { cn } from "@/lib/utils";

interface DynamicFormProps {
  fields: FieldMetadata[];
  title: string;
  mode: "create" | "edit";
  initialData?: Record<string, unknown>;
  isOpen: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (data: Record<string, unknown>) => void;
}

/**
 * Metadata-driven form dialog.
 * React Hook Form + Zod validation, metadata'dan otomatik üretilir.
 */
export function DynamicForm({
  fields,
  title,
  mode,
  initialData,
  isOpen,
  isSubmitting,
  onClose,
  onSubmit,
}: DynamicFormProps) {
  const editableFields = fields.filter((f) => !f.readOnly && !f.computed);
  const schema = metadataToZodSchema(editableFields);
  const defaults = metadataToDefaults(editableFields);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: initialData ?? defaults,
  });

  // Reset form when dialog opens or initialData changes
  useEffect(() => {
    if (isOpen) {
      reset(initialData ?? defaults);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, initialData]);

  if (!isOpen) return null;

  const handleFormSubmit = (data: Record<string, unknown>) => {
    onSubmit(data);
  };

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm animate-fade-in"
        onClick={onClose}
      />

      {/* Panel (Sheet from right) */}
      <div
        className={cn(
          "fixed right-0 top-0 z-50 h-full w-full max-w-lg",
          "bg-[var(--color-card-bg)] border-l border-[var(--color-border)]",
          "shadow-2xl shadow-black/20",
          "flex flex-col",
          "animate-slide-in-right"
        )}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <div>
            <h2 className="text-lg font-semibold text-[var(--color-text)]">
              {mode === "create" ? `Yeni ${title}` : `${title} Düzenle`}
            </h2>
            <p className="text-sm text-[var(--color-text-muted)]">
              {mode === "create"
                ? "Aşağıdaki bilgileri doldurun"
                : "Bilgileri güncelleyin"}
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors"
          >
            <X className="w-5 h-5 text-[var(--color-text-muted)]" />
          </button>
        </div>

        {/* Form Body */}
        <form
          onSubmit={handleSubmit(handleFormSubmit)}
          className="flex-1 flex flex-col overflow-hidden"
        >
          <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
            {editableFields.map((field) => (
              <Controller
                key={field.name}
                name={field.name}
                control={control}
                render={({ field: formField }) => (
                  <DynamicField
                    field={field}
                    value={formField.value}
                    onChange={formField.onChange}
                    error={errors[field.name]?.message as string | undefined}
                    disabled={isSubmitting}
                  />
                )}
              />
            ))}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-[var(--color-border)] bg-[var(--color-bg)]/50">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className={cn(
                "px-4 py-2 rounded-lg text-sm font-medium",
                "border border-[var(--color-border)]",
                "text-[var(--color-text-muted)] hover:text-[var(--color-text)]",
                "hover:bg-[var(--color-border)]",
                "transition-all duration-200"
              )}
            >
              İptal
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className={cn(
                "flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-medium",
                "bg-indigo-600 text-white",
                "hover:bg-indigo-700 active:bg-indigo-800",
                "shadow-md shadow-indigo-500/20",
                "transition-all duration-200",
                "disabled:opacity-60 disabled:cursor-not-allowed"
              )}
            >
              {isSubmitting ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <Save className="w-4 h-4" />
              )}
              {mode === "create" ? "Oluştur" : "Kaydet"}
            </button>
          </div>
        </form>
      </div>
    </>
  );
}
