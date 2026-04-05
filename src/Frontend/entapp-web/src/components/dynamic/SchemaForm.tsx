"use client";

import { useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Save, FileText } from "lucide-react";
import type { FieldMetadata } from "@/types/dynamic";
import { metadataToZodSchema, metadataToDefaults } from "@/lib/schema-to-zod";
import { DynamicField } from "./DynamicField";
import { cn } from "@/lib/utils";

interface SchemaFormProps {
  /** JSON schema — FieldMetadata[] formatında */
  schema: FieldMetadata[] | string | null;
  /** Önceden doldurulmuş form verileri (edit mode) */
  initialData?: Record<string, unknown>;
  /** Form submit callback — form verileri JSON string olarak döner */
  onSubmit: (formDataJson: string) => void;
  /** Loading state */
  isSubmitting?: boolean;
  /** Disabled state */
  disabled?: boolean;
  /** Custom class */
  className?: string;
}

/**
 * Standalone JSON Schema Form.
 * RequestCategory.FormSchemaJson veya workflow step form'ları için kullanılır.
 * Mevcut DynamicField + schema-to-zod altyapısını reuse eder.
 */
export function SchemaForm({
  schema,
  initialData,
  onSubmit,
  isSubmitting = false,
  disabled = false,
  className,
}: SchemaFormProps) {
  const [fields, setFields] = useState<FieldMetadata[]>([]);

  // Parse schema (string or array)
  useEffect(() => {
    if (!schema) {
      setFields([]);
      return;
    }

    if (typeof schema === "string") {
      try {
        const parsed = JSON.parse(schema);
        setFields(Array.isArray(parsed) ? parsed : []);
      } catch {
        setFields([]);
      }
    } else {
      setFields(schema);
    }
  }, [schema]);

  const zodSchema = metadataToZodSchema(fields);
  const defaults = metadataToDefaults(fields);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(zodSchema),
    defaultValues: initialData ?? defaults,
  });

  useEffect(() => {
    reset(initialData ?? defaults);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [schema, initialData]);

  if (fields.length === 0) {
    return (
      <div className={cn(
        "flex items-center gap-3 p-4 rounded-lg",
        "bg-[var(--color-bg)] border border-dashed border-[var(--color-border)]",
        "text-sm text-[var(--color-text-muted)]",
        className
      )}>
        <FileText className="w-4 h-4 opacity-60" />
        Bu kategori için ek form alanı tanımlanmamış.
      </div>
    );
  }

  const handleFormSubmit = (data: Record<string, unknown>) => {
    onSubmit(JSON.stringify(data));
  };

  return (
    <div className={cn("space-y-4", className)}>
      <div className="flex items-center gap-2 mb-3">
        <FileText className="w-4 h-4 text-indigo-400" />
        <span className="text-sm font-medium text-[var(--color-text)]">
          Ek Bilgiler
        </span>
        <span className="text-xs text-[var(--color-text-muted)]">
          ({fields.length} alan)
        </span>
      </div>

      <div className="space-y-4 p-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)]">
        {fields.map((field) => (
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
                disabled={disabled || isSubmitting}
              />
            )}
          />
        ))}
      </div>
    </div>
  );
}

/**
 * Hook: Kategori ID ile form şemasını fetch eder.
 */
export function useFormSchema(categoryId: string | null) {
  const [schema, setSchema] = useState<FieldMetadata[] | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!categoryId) {
      setSchema(null);
      return;
    }

    setLoading(true);
    fetch(`/api/req/categories/${categoryId}/form-schema`)
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => setSchema(Array.isArray(data) ? data : []))
      .catch(() => setSchema(null))
      .finally(() => setLoading(false));
  }, [categoryId]);

  return { schema, loading };
}
