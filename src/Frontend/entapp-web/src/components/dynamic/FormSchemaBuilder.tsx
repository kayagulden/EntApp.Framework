"use client";

import { useState, useCallback } from "react";
import {
  Plus,
  Trash2,
  GripVertical,
  Settings2,
  ChevronDown,
  ChevronUp,
  Copy,
  FileJson,
} from "lucide-react";
import type { FieldMetadata, FieldType } from "@/types/dynamic";
import { cn } from "@/lib/utils";

interface FormSchemaBuilderProps {
  /** Mevcut schema JSON string */
  value: string;
  /** Schema değiştiğinde callback */
  onChange: (schemaJson: string) => void;
  /** Disabled */
  disabled?: boolean;
}

const FIELD_TYPES: { value: FieldType; label: string; icon: string }[] = [
  { value: "string", label: "Metin", icon: "Aa" },
  { value: "text", label: "Uzun Metin", icon: "¶" },
  { value: "number", label: "Sayı", icon: "#" },
  { value: "decimal", label: "Ondalık", icon: ".0" },
  { value: "money", label: "Para", icon: "₺" },
  { value: "date", label: "Tarih", icon: "📅" },
  { value: "datetime", label: "Tarih-Saat", icon: "🕐" },
  { value: "boolean", label: "Evet/Hayır", icon: "☑" },
  { value: "enum", label: "Seçenek", icon: "▾" },
  { value: "file", label: "Dosya", icon: "📎" },
];

/**
 * Visual Form Schema Builder — Admin Panel.
 * RequestCategory düzenlerken FormSchemaJson'ı görsel olarak oluşturur.
 */
export function FormSchemaBuilder({
  value,
  onChange,
  disabled = false,
}: FormSchemaBuilderProps) {
  const [fields, setFields] = useState<FieldMetadata[]>(() => {
    try {
      const parsed = JSON.parse(value || "[]");
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  });
  const [expandedIdx, setExpandedIdx] = useState<number | null>(null);
  const [showJson, setShowJson] = useState(false);

  const updateFields = useCallback(
    (newFields: FieldMetadata[]) => {
      setFields(newFields);
      onChange(JSON.stringify(newFields, null, 2));
    },
    [onChange]
  );

  const addField = () => {
    const newField: FieldMetadata = {
      name: `field_${fields.length + 1}`,
      label: `Alan ${fields.length + 1}`,
      type: "string",
      required: false,
      readOnly: false,
      searchable: false,
      maxLength: 200,
      minLength: 0,
    };
    updateFields([...fields, newField]);
    setExpandedIdx(fields.length);
  };

  const removeField = (idx: number) => {
    updateFields(fields.filter((_, i) => i !== idx));
    if (expandedIdx === idx) setExpandedIdx(null);
  };

  const duplicateField = (idx: number) => {
    const copy = { ...fields[idx], name: `${fields[idx].name}_copy` };
    const newFields = [...fields];
    newFields.splice(idx + 1, 0, copy);
    updateFields(newFields);
  };

  const moveField = (idx: number, dir: -1 | 1) => {
    const target = idx + dir;
    if (target < 0 || target >= fields.length) return;
    const newFields = [...fields];
    [newFields[idx], newFields[target]] = [newFields[target], newFields[idx]];
    updateFields(newFields);
    setExpandedIdx(target);
  };

  const updateField = (idx: number, patch: Partial<FieldMetadata>) => {
    const newFields = fields.map((f, i) =>
      i === idx ? { ...f, ...patch } : f
    );
    updateFields(newFields);
  };

  return (
    <div className="space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Settings2 className="w-4 h-4 text-indigo-400" />
          <span className="text-sm font-medium text-[var(--color-text)]">
            Form Şeması
          </span>
          <span className="text-xs px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-400">
            {fields.length} alan
          </span>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setShowJson(!showJson)}
            className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors flex items-center gap-1"
          >
            <FileJson className="w-3.5 h-3.5" />
            {showJson ? "Gizle" : "JSON"}
          </button>
        </div>
      </div>

      {/* JSON Preview */}
      {showJson && (
        <pre className="text-xs p-3 rounded-lg bg-[var(--color-bg)] border border-[var(--color-border)] overflow-auto max-h-48 text-[var(--color-text-muted)]">
          {JSON.stringify(fields, null, 2)}
        </pre>
      )}

      {/* Field List */}
      <div className="space-y-2">
        {fields.map((field, idx) => (
          <div
            key={idx}
            className={cn(
              "rounded-lg border transition-all duration-200",
              expandedIdx === idx
                ? "border-indigo-500/40 bg-indigo-500/5"
                : "border-[var(--color-border)] bg-[var(--color-card-bg)]"
            )}
          >
            {/* Collapsed Header */}
            <div
              className="flex items-center gap-2 px-3 py-2.5 cursor-pointer"
              onClick={() =>
                setExpandedIdx(expandedIdx === idx ? null : idx)
              }
            >
              <GripVertical className="w-4 h-4 text-[var(--color-text-muted)] opacity-40" />
              <span className="text-xs px-1.5 py-0.5 rounded bg-[var(--color-border)] text-[var(--color-text-muted)] font-mono">
                {FIELD_TYPES.find((t) => t.value === field.type)?.icon || "?"}
              </span>
              <span className="text-sm font-medium text-[var(--color-text)] flex-1">
                {field.label}
              </span>
              <span className="text-xs text-[var(--color-text-muted)] font-mono">
                {field.name}
              </span>
              {field.required && (
                <span className="text-xs text-red-400">*</span>
              )}
              <ChevronDown
                className={cn(
                  "w-4 h-4 text-[var(--color-text-muted)] transition-transform",
                  expandedIdx === idx && "rotate-180"
                )}
              />
            </div>

            {/* Expanded Settings */}
            {expandedIdx === idx && (
              <div className="px-3 pb-3 space-y-3 border-t border-[var(--color-border)] pt-3">
                <div className="grid grid-cols-2 gap-3">
                  {/* Name */}
                  <div>
                    <label className="block text-xs text-[var(--color-text-muted)] mb-1">
                      Alan Adı (key)
                    </label>
                    <input
                      type="text"
                      value={field.name}
                      onChange={(e) =>
                        updateField(idx, { name: e.target.value })
                      }
                      disabled={disabled}
                      className="w-full text-sm px-2.5 py-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)] font-mono"
                    />
                  </div>

                  {/* Label */}
                  <div>
                    <label className="block text-xs text-[var(--color-text-muted)] mb-1">
                      Etiket
                    </label>
                    <input
                      type="text"
                      value={field.label}
                      onChange={(e) =>
                        updateField(idx, { label: e.target.value })
                      }
                      disabled={disabled}
                      className="w-full text-sm px-2.5 py-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    />
                  </div>

                  {/* Type */}
                  <div>
                    <label className="block text-xs text-[var(--color-text-muted)] mb-1">
                      Tip
                    </label>
                    <select
                      value={field.type}
                      onChange={(e) =>
                        updateField(idx, {
                          type: e.target.value as FieldType,
                        })
                      }
                      disabled={disabled}
                      className="w-full text-sm px-2.5 py-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    >
                      {FIELD_TYPES.map((t) => (
                        <option key={t.value} value={t.value}>
                          {t.icon} {t.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* MaxLength */}
                  <div>
                    <label className="block text-xs text-[var(--color-text-muted)] mb-1">
                      Maks. Uzunluk
                    </label>
                    <input
                      type="number"
                      value={field.maxLength || ""}
                      onChange={(e) =>
                        updateField(idx, {
                          maxLength: parseInt(e.target.value) || 0,
                        })
                      }
                      disabled={disabled}
                      className="w-full text-sm px-2.5 py-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    />
                  </div>
                </div>

                {/* Options (for enum type) */}
                {field.type === "enum" && (
                  <div>
                    <label className="block text-xs text-[var(--color-text-muted)] mb-1">
                      Seçenekler (virgülle ayırın)
                    </label>
                    <input
                      type="text"
                      value={field.options?.join(", ") || ""}
                      onChange={(e) =>
                        updateField(idx, {
                          options: e.target.value
                            .split(",")
                            .map((s) => s.trim())
                            .filter(Boolean),
                        })
                      }
                      disabled={disabled}
                      placeholder="Seçenek1, Seçenek2, Seçenek3"
                      className="w-full text-sm px-2.5 py-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    />
                  </div>
                )}

                {/* Checkboxes */}
                <div className="flex items-center gap-4">
                  <label className="inline-flex items-center gap-1.5 text-sm cursor-pointer">
                    <input
                      type="checkbox"
                      checked={field.required}
                      onChange={(e) =>
                        updateField(idx, { required: e.target.checked })
                      }
                      disabled={disabled}
                      className="rounded border-[var(--color-border)]"
                    />
                    <span className="text-[var(--color-text-muted)]">
                      Zorunlu
                    </span>
                  </label>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-1 pt-1 border-t border-[var(--color-border)]">
                  <button
                    type="button"
                    onClick={() => moveField(idx, -1)}
                    disabled={idx === 0 || disabled}
                    className="p-1.5 rounded hover:bg-[var(--color-border)] text-[var(--color-text-muted)] disabled:opacity-30 transition-colors"
                    title="Yukarı taşı"
                  >
                    <ChevronUp className="w-4 h-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => moveField(idx, 1)}
                    disabled={idx === fields.length - 1 || disabled}
                    className="p-1.5 rounded hover:bg-[var(--color-border)] text-[var(--color-text-muted)] disabled:opacity-30 transition-colors"
                    title="Aşağı taşı"
                  >
                    <ChevronDown className="w-4 h-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => duplicateField(idx)}
                    disabled={disabled}
                    className="p-1.5 rounded hover:bg-[var(--color-border)] text-[var(--color-text-muted)] transition-colors"
                    title="Kopyala"
                  >
                    <Copy className="w-4 h-4" />
                  </button>
                  <div className="flex-1" />
                  <button
                    type="button"
                    onClick={() => removeField(idx)}
                    disabled={disabled}
                    className="p-1.5 rounded hover:bg-red-500/10 text-red-400 transition-colors"
                    title="Sil"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Add Field */}
      <button
        type="button"
        onClick={addField}
        disabled={disabled}
        className={cn(
          "w-full flex items-center justify-center gap-2 py-2.5 rounded-lg",
          "border border-dashed border-[var(--color-border)]",
          "text-sm text-[var(--color-text-muted)]",
          "hover:border-indigo-500/40 hover:text-indigo-400 hover:bg-indigo-500/5",
          "transition-all duration-200",
          "disabled:opacity-50 disabled:cursor-not-allowed"
        )}
      >
        <Plus className="w-4 h-4" />
        Alan Ekle
      </button>
    </div>
  );
}
